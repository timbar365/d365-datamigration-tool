using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using D365.Framework.Tools.DataMigration.Base;
using D365.Framework.Tools.DataMigration.Clients;
using D365.Framework.Tools.DataMigration.Configuration;
using D365.Framework.Tools.DataMigration.Helper;
using D365.Framework.Tools.DataMigration.Types;
using log4net;
using MoreLinq;
using Newtonsoft.Json;

namespace D365.Framework.Tools.DataMigration.Import
{
    public class DataImporter : DataProcessor
    {
        private static ILog Log = LogManager.GetLogger(typeof(DataImporter));
        private OwnerMapper OwnerMapper { get; }
        private FieldMapper FieldMapper { get; }
        private AlternateKeyMapper AlternateKeyMapper { get; }
        private int ParallelLimit { get; }

        public DataImporter(DynamicsClient client, int parallelLimit, OwnerMapper ownerMapper, FieldMapper fieldMapper) : base(client)
        {
            this.ParallelLimit = parallelLimit;
            this.OwnerMapper = ownerMapper;
            this.AlternateKeyMapper = new AlternateKeyMapper(client);
            this.FieldMapper = fieldMapper;
        }

        public bool Import(string importDataFilePath)
        {
            Log.Debug("Import(" + importDataFilePath + ")");
            this.ErrorOccurred = false;

            var importJob = JsonConvert.DeserializeObject<ImportJobDefinition>(File.ReadAllText(importDataFilePath), JsonSettings.JsonSerializationSettings);

            Log.Info("--------------");
            Log.Info("Initial mappings");
            Log.Info("--------------");
            //Import mapping
            this.FieldMapper.MapFields(importJob);
            //Map all primaryids and lookups with alternate keys
            this.AlternateKeyMapper.ProcessAlternateKeys(importJob);

            if (importJob.EnsureRecordAvailabilty)
            {
                Log.Info("--------------");
                Log.Info("Ensure records");
                Log.Info("--------------");
                ProcessImportJob(importJob, (ImportEntityDefinition d, WebApiEntity e) =>
                {
                    var fullEntity = PrepareEntity(d, e, importJob);

                    if (d.EnsureWithAllFields)
                    {
                        return fullEntity;
                    }
                    WebApiEntity eMin = new WebApiEntity(fullEntity.CollectionName, fullEntity.Id);
                    if (d.PrimaryAttributeName != null && fullEntity.Contains(d.PrimaryAttributeName))
                    {
                        eMin.Attributes.Add(d.PrimaryAttributeName, fullEntity[d.PrimaryAttributeName]);
                    }
                    if (fullEntity.Contains("overriddencreatedon"))
                    {
                        eMin.Attributes.Add("overriddencreatedon", fullEntity["overriddencreatedon"]);
                    }
                    if (!string.IsNullOrEmpty(d.EnsureWithFields))
                    {
                        foreach (var f in d.EnsureWithFields.Split(','))
                        {
                            //Check if maybe lookup field --> odata.bind
                            var keyName = fullEntity.Contains(f) ? f : fullEntity.Contains($"{f}@odata.bind") ? $"{f}@odata.bind" : throw new ArgumentOutOfRangeException($"Field not found in entity: {f}");
                            eMin.Attributes.Add(keyName, fullEntity[keyName]);
                        };
                    }
                    return eMin;
                }, "Importing ensured", false);
            }
            Log.Info("--------------");
            Log.Info("Deactivate");
            Log.Info("--------------");
            this.DeactivateAll(importJob);

            Log.Info("--------------");
            Log.Info("Do full import of records");
            Log.Info("--------------");
            ProcessImportJob(importJob, (ImportEntityDefinition d, WebApiEntity e) => PrepareEntity(d, e, importJob), "Importing full", true);
            return !this.ErrorOccurred;
        }

        private void ProcessFieldMapping(string importMappingFilePath, ImportJobDefinition importJob)
        {
            if (!string.IsNullOrWhiteSpace(importMappingFilePath))
            {
                var allFieldMapping = JsonConvert.DeserializeObject<EntityMapping>(File.ReadAllText(importMappingFilePath), JsonSettings.JsonSerializationSettings);

                SemaphoreSlim semaphoreEntityDef = new SemaphoreSlim(4);
                Task.WaitAll(importJob.ImportEntityDefinitions.Select(ed => Task.Factory.StartNew(() =>
                {
                    using (new SemaphoreUsage(semaphoreEntityDef))
                    {
                        //Get mapping for current entity
                        var entityFieldMapping = allFieldMapping?.EntityMappings?.Where(em => em.Target?.ToLower() == ed.EntityLogicalName.ToLower()).FirstOrDefault();
                        if (entityFieldMapping != null)
                        {
                            Log.Info($"Mapping entity: {entityFieldMapping.Source}-->{entityFieldMapping.Target} ");

                            //Map entity logical Name if defined
                            if (entityFieldMapping.Target != null)
                            {
                                ed.EntityLogicalName = entityFieldMapping.Target;
                                ed.EntityCollectionName = this.Client.GetEntityMetadata(entityFieldMapping.Target, true).EntitySetName;
                            }

                            //Start mapping all fields
                            entityFieldMapping.Fields?.ForEach(fm =>
                            {
                                SemaphoreSlim semaphoreFieldMap = new SemaphoreSlim(4);
                                //Map data fields
                                Task.WaitAll(ed.Entities.Select(e => Task.Factory.StartNew(() =>
                                {
                                    using (new SemaphoreUsage(semaphoreFieldMap))
                                    {
                                        if (e.Attributes.ContainsKey(fm.Source))
                                        {
                                            Log.Debug($"Mapping field {fm.Source}-->{fm.Target}");
                                            var value = e[fm.Source];
                                            e.Attributes.Remove(fm.Source);
                                            e.Attributes.Add(fm.Target, value);
                                        }

                                        //Handle all lookups
                                        e.Attributes.Keys.Where(k => k.Contains("@Microsoft.Dynamics.CRM.lookuplogicalname")).ToList().ForEach(k =>
                                        {
                                            var sourceLogicalName = e[k] as string;
                                            var lookupEnitityMapping = allFieldMapping.EntityMappings.Where(em => em.Source.ToLower() == sourceLogicalName?.ToLower()).FirstOrDefault();

                                            if (lookupEnitityMapping != null)
                                            {
                                                var targetLogicalName = lookupEnitityMapping.Target;
                                                e[k] = (string)targetLogicalName;
                                                //Handle Nav Property
                                            }
                                        });
                                    }
                                })).ToArray());

                                //Map other configurations with fields
                                ed.EnsureWithFields = ed.EnsureWithFields == null ? null : string.Join(',', ed.EnsureWithFields?.Split(',').Select(s => s.Trim().ToLower() == fm.Source.ToLower() ? fm.Target : s) ?? new string[] { });
                                ed.AlternateKeyAttribute?.Replace(fm.Source, fm.Target, true, System.Globalization.CultureInfo.CurrentCulture);
                                ed.FieldsToIgnore?.Replace(fm.Source, fm.Target, true, System.Globalization.CultureInfo.CurrentCulture);
                            });
                        }
                    }
                })).ToArray()
                );
            }
        }

        private WebApiEntity PrepareEntity(ImportEntityDefinition entityDef, WebApiEntity entity, ImportJobDefinition jobDef)
        {
            if (!string.IsNullOrWhiteSpace(entityDef.FieldsToIgnore))
            {
                //Remove field that should be ignored
                entityDef.FieldsToIgnore.Split(",").ForEach(f => entity.Attributes.Remove(f));
            }

            this.PrepareAttributes(entityDef.EntityLogicalName, entity.Attributes, jobDef);

            return entity;
        }

        private void PrepareAttributes(string entityLogicalName, Dictionary<string, object> attributes, ImportJobDefinition jobDef)
        {
            var entityMetadata = this.Client.GetEntityMetadata(entityLogicalName, true);

            attributes.Keys.Where(
                k => k.StartsWith("_created")
                || k.StartsWith("_owning")
                || k.StartsWith("_modified")
                || (entityMetadata.OwnershipType?.ToLower() == "none" && k.StartsWith("_ownerid_value"))
                ).ToList().ForEach(k =>
            {
                attributes.Remove(k);
            });

            //Override createdon
            if (attributes.ContainsKey("createdon") && !attributes.ContainsKey("overriddencreatedon"))
            {
                attributes.Add("overriddencreatedon", attributes["createdon"]);
            }

            this.OwnerMapper.MapOwner(attributes);

            //Navigation-Properties erstellen
            (from a in attributes
             where a.Value != null && a.Key.StartsWith("_") && a.Key.EndsWith("_value")
             select a).ToList().ForEach(lk =>
             {
                 string navProp = $"{lk.Key}@Microsoft.Dynamics.CRM.associatednavigationproperty";
                 string newKeyName = null;
                 if (attributes.ContainsKey(navProp))
                 {
                     newKeyName = attributes[navProp].ToString();
                 }
                 else
                 {
                     //Load AttributeMetadata
                     var referencedEntityLogicalName = attributes[$"{lk.Key}@Microsoft.Dynamics.CRM.lookuplogicalname"].ToString();
                     var attrLogicalName = Regex.Replace(lk.Key, "^_(.*)_value$", "$1");
                     newKeyName = this.Client.GetRelationshipMetadataManyToOne(entityLogicalName, attrLogicalName, referencedEntityLogicalName).ReferencingEntityNavigationPropertyName;
                 }

                 attributes[$"{newKeyName}@odata.bind"] = $"{this.Client.GetEntityMetadata(attributes[$"{lk.Key}@Microsoft.Dynamics.CRM.lookuplogicalname"].ToString()).EntitySetName}({attributes[lk.Key]})";
                 attributes.Keys.Where(k => k.StartsWith(lk.Key)).ToList().ForEach(k => attributes.Remove(k));
                 //attributes.Remove(lk.Key);
             });

            attributes.Keys.ToList().ForEach(k =>
            {
                if (attributes[k] is Newtonsoft.Json.Linq.JArray)
                {
                    var r = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(attributes[k].ToString());
                    var m = this.Client.GetRelationshipMetadataOneToMany(entityLogicalName, k);
                    r.ForEach(d => { this.PrepareAttributes(m.ReferencingEntity, d, jobDef); });
                    attributes[k] = r;
                }
            });

        }

        protected void ProcessImportJob(ImportJobDefinition importJob, Func<ImportEntityDefinition, WebApiEntity, WebApiEntity> entitySelect, string taskname, bool isFullRun)
        {
            importJob.ImportEntityDefinitions.ForEach((ed) =>
           {
               if (ed.SkipImport)
                   Log.Info($"Skipping import of {ed.EntityLogicalName}");
               else
               {
                   var entityMetadata = this.Client.GetEntityMetadata(ed.EntityLogicalName, true);
                   if (entityMetadata.IsIntersect)
                   {
                       if (isFullRun)
                       {
                           Log.Info($"{taskname} {ed.EntityCollectionName} - {ed.Entities?.Count ?? 0} associations");
                           AssociateEntities(ed, ed.Entities, parrallelLimit: this.ParallelLimit, byPassPlugins: ed.DisablePlugins);
                       }
                   }
                   else
                   {
                       Log.Info($"{taskname} {ed.EntityCollectionName} - {ed.Entities?.Count ?? 0} records");
                       UpsertEntities(ed.Entities.Select((WebApiEntity e) => entitySelect(ed, e)).ToList(), parrallelLimit: this.ParallelLimit, byPassPlugins: ed.DisablePlugins);
                   }
               }
           });
        }

        protected void AssociateEntities(ImportEntityDefinition ed, List<WebApiEntity> entities, int chunkSize = 50, int parrallelLimit = 5, bool byPassPlugins = false)
        {
            Action<DynamicsClient, WebApiEntity, bool> webApiCall = (client, entity, byPassPlugins) =>
            {
                var relationshipMetadata = client.GetRelationshipMetadataManyToMany(ed.EntityLogicalName);
                var attr1EntityMetadata = client.GetEntityMetadata(relationshipMetadata.Entity1LogicalName);
                var attr2EntityMetadata = client.GetEntityMetadata(relationshipMetadata.Entity2LogicalName);

                // Get alternate Key Mappings for m:n associated entities
                
                var entity1key = AlternateKeyMapper.GetKeyFromMapping(attr1EntityMetadata.LogicalName, entity[relationshipMetadata.Entity1IntersectAttribute]?.ToString());
                var entity2key = AlternateKeyMapper.GetKeyFromMapping(attr2EntityMetadata.LogicalName, entity[relationshipMetadata.Entity2IntersectAttribute]?.ToString());

                client.Associate(attr1EntityMetadata.EntitySetName, entity1key, attr2EntityMetadata.EntitySetName, entity2key, relationshipMetadata.SchemaName, byPassPlugins);
            };
            ProcessEntities(entities, webApiCall, "Associating", chunkSize, parrallelLimit, byPassPlugins);
        }

        protected void UpsertEntities(List<WebApiEntity> entities, int chunkSize = 50, int parrallelLimit = 5, bool byPassPlugins = false)
        {
            Action<DynamicsClient, WebApiEntity, bool> webApiCall = (client, entity, byPassPlugins) => client.Upsert(entity.CollectionName, entity.Id, entity.Attributes, byPassPlugins);
            ProcessEntities(entities, webApiCall, "Upserting", chunkSize, parrallelLimit, byPassPlugins);
        }

        private void ProcessEntities(List<WebApiEntity> entities, Action<DynamicsClient, WebApiEntity, bool> webApiCall, string actionName, int chunkSize = 50, int parrallelLimit = 5, bool byPassPlugins = false)
        {
            if (byPassPlugins) Log.Info($"Plugins are bypassed");
            List<Task> allTasks = new List<Task>();
            SemaphoreSlim semaphore = new SemaphoreSlim(parrallelLimit);
            try
            {
                // ExecuteMultipleRequest request = default(ExecuteMultipleRequest);
                entities.Batch(chunkSize).ForEach((IEnumerable<WebApiEntity> entitiesChunk, int chunkIndex) =>
                {
                    while (semaphore.CurrentCount == 0) Thread.Sleep(1000); //Only add new tasks if limit has not reached yet
                    allTasks.Add(Task.Run(() =>
                    {
                        Log.Debug($"Waiting Chunk {chunkIndex}");
                        using (new SemaphoreUsage(semaphore))
                        {
                            Log.Debug($"Starting Chunk {chunkIndex} - {actionName} {entitiesChunk.Count()} records");
                            var chunkClient = this.Client.Clone();
                            entitiesChunk.ForEach(e =>
                            {
                                try
                                {
                                    webApiCall(chunkClient, e, byPassPlugins);
                                }
                                catch (System.Exception ex)
                                {
                                    this.ErrorOccurred = true;
                                    Log.Error($"Chunk:{chunkIndex} - Error {actionName.ToLower()} record {e?.CollectionName}@{e?.Id} : {ex.Message} {ex.InnerException?.Message}");
                                }
                            });
                            Log.Debug($"Ending Chunk {chunkIndex}");
                        }
                    }));
                    Thread.Sleep(1000); //Give other Threads time to start
                });
                Task.WaitAll(allTasks.ToArray());
            }
            finally
            {
                if (semaphore != null)
                {
                    ((IDisposable)semaphore).Dispose();
                }
            }
        }

        #region Deactivate

        protected void DeactivateAll(ImportJobDefinition importJob)
        {
            importJob.ImportEntityDefinitions.Where(def => def.DeactivateAllRecords && !def.SkipImport).ToList().ForEach(def =>
            {
                Log.Info($"Deactivating {def.EntityCollectionName}");

                this.UpsertEntities(GetAllEntityData(def).Select(e =>
                {
                    var deactivateEntity = new WebApiEntity(e.CollectionName, e.Id);
                    deactivateEntity["statecode"] = 1;
                    deactivateEntity["statuscode"] = 2;
                    return deactivateEntity;
                }).ToList(), parrallelLimit: this.ParallelLimit, byPassPlugins: def.DisablePlugins);

            });
        }

        private List<WebApiEntity> GetAllEntityData(ImportEntityDefinition meta)
        {
            var fetchXml = $"<fetch><entity name='{meta.EntityLogicalName}'><attribute name='{meta.PrimaryAttributeId}' /></entity></fetch>";

            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            int num = 1;
            EntityDataResult entityCollection = null;
            do
            {
                Log.Debug($"Read Page {num} - Cookie: {entityCollection?.PagingCookie}");
                entityCollection = GetNextPage(meta.EntityCollectionName, fetchXml, entityCollection?.PagingCookie, num++);

                list.AddRange(entityCollection.EntityRecords);
            }
            while (entityCollection.HasMoreRecords);
            return list.Select(i => this.ConvertToEntity(meta.EntityCollectionName, meta.PrimaryAttributeId, i)).ToList();
        }

        #endregion

    }
}