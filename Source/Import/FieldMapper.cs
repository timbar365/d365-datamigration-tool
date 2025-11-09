using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using D365.Framework.Tools.DataMigration.Clients;
using D365.Framework.Tools.DataMigration.Configuration;
using D365.Framework.Tools.DataMigration.Helper;
using D365.Framework.Tools.DataMigration.Types;
using log4net;
using Newtonsoft.Json;

namespace D365.Framework.Tools.DataMigration.Import
{
    public class FieldMapper
    {
        private static ILog Log = LogManager.GetLogger(typeof(FieldMapper));
        private EntityMapping EntityMapping { get; }
        private DynamicsClient Client { get; }

        public FieldMapper(string pathMappingFile, DynamicsClient client)
        {
            if (pathMappingFile != null)
                this.EntityMapping = JsonConvert.DeserializeObject<EntityMapping>(File.ReadAllText(pathMappingFile), JsonSettings.JsonSerializationSettings);
            this.Client = client;
        }

        public void MapFields(ImportJobDefinition importJob)
        {
            if (this.EntityMapping == null) return;

            SemaphoreSlim semaphoreEntityDef = new SemaphoreSlim(4);
            Task.WaitAll(importJob.ImportEntityDefinitions.Select(ed => Task.Factory.StartNew(() =>
            {
                using (new SemaphoreUsage(semaphoreEntityDef))
                {
                    //Get mapping for current entity
                    var entityFieldMapping = this.EntityMapping?.EntityMappings?.Where(em => em.Source?.ToLower() == ed.EntityLogicalName.ToLower()).FirstOrDefault();
                    if (entityFieldMapping != null)
                    {
                        Log.Info($"Mapping entity: {entityFieldMapping.Source}-->{entityFieldMapping.Target} ");

                        //Map entity logical Name if defined
                        if (entityFieldMapping.Target != null)
                        {
                            ed.EntityLogicalName = entityFieldMapping.Target;
                            ed.EntityCollectionName = this.Client.GetEntityMetadata(entityFieldMapping.Target, true).EntitySetName;

                            {
                                SemaphoreSlim semaphoreFieldMap = new SemaphoreSlim(4);
                                //Map data fields
                                Task.WaitAll(ed.Entities.Select(e => Task.Factory.StartNew(() =>
                                {
                                    using (new SemaphoreUsage(semaphoreFieldMap))
                                    {
                                        e.CollectionName = ed.EntityCollectionName;
                                    }
                                })).ToArray());
                            }
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
                                        e.Attributes[fm.Target] = value;
                                    }
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

            //Handle all lookups
            this.MapLookups(importJob);

        }

        private void MapLookups(ImportJobDefinition importJob)
        {
            //Handle all lookups
            SemaphoreSlim semaphoreEntityLkp = new SemaphoreSlim(4);
            Task.WaitAll(importJob.ImportEntityDefinitions.SelectMany(i => i.Entities).ToList().Select(e => Task.Factory.StartNew(() =>
            {
                using (new SemaphoreUsage(semaphoreEntityLkp))
                {
                    //Handle all lookups
                    e.Attributes.Keys.Where(k => k.Contains("@Microsoft.Dynamics.CRM.lookuplogicalname")).ToList().ForEach(k =>
                    {
                        var sourceLogicalName = e[k] as string;
                        var lookupEnitityMapping = this.EntityMapping?.EntityMappings?.Where(em => em.Source.ToLower() == sourceLogicalName?.ToLower()).FirstOrDefault();

                        if (lookupEnitityMapping != null && lookupEnitityMapping.Source.ToLower() != lookupEnitityMapping.Target?.ToLower())
                        {
                            var targetLogicalName = lookupEnitityMapping.Target;
                            e[k] = (string)targetLogicalName;
                            //Remove Nav Property, not necessary
                            var attrkeyFirst = k.Split('@')[0];
                            e.Attributes.Remove($"{attrkeyFirst}@Microsoft.Dynamics.CRM.associatednavigationproperty");
                        }
                    });

                }
            })).ToArray());
        }
    }
}