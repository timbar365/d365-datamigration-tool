using System.IO;
using System.Xml.Serialization;
using log4net;
using D365.Framework.Tools.DataMigration.Clients;
using D365.Framework.Tools.DataMigration.Types;
using Newtonsoft.Json;
using D365.Framework.Tools.DataMigration.Configuration;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System;
using System.Text;
using D365.Framework.Tools.DataMigration.Base;

namespace D365.Framework.Tools.DataMigration.Export
{
    public class DataExporter : DataProcessor
    {
        private static ILog Log = LogManager.GetLogger(typeof(DataExporter));
        public DataExporter(DynamicsClient client) : base(client)
        {
        }

        public void Export(string exportConfigurationFilePath, string exportDataFilePath)
        {
            using (StreamReader textReader = File.OpenText(exportConfigurationFilePath))
            {
                entities entities = (entities)new XmlSerializer(typeof(entities)).Deserialize(textReader);
                ImportJobDefinition importJobDefinition = new ImportJobDefinition
                {
                    EnsureRecordAvailabilty = true
                };
                entities.entity.ToList().ForEach((entitiesEntity e) =>
                {
                    //Read meta data to retrieve the missing information
                    var meta = this.Client.GetEntityMetadata(e.logicalname);
                    e.collectionname = meta.EntitySetName;
                    e.primaryidfield = meta.PrimaryIdAttribute;
                    e.primarynamefield = meta.PrimaryNameAttribute;

                    Log.Info("Exporting " + e.logicalname);
                    ImportEntityDefinition importEntityDefinition = new ImportEntityDefinition
                    {
                        DisablePlugins = e.disableplugins,
                        EntityLogicalName = e.logicalname,
                        EntityCollectionName = e.collectionname,
                        PrimaryAttributeId = e.primaryidfield,
                        PrimaryAttributeName = e.primarynamefield,
                        EnsureWithAllFields = e.ensureWithAllFields,
                        EnsureWithFields = e.ensureWithFields,
                        AlternateKeyAttribute = e.alternateKeyField,
                        DeactivateAllRecords = e.deactivateAllRecords,
                        SkipImport = e.skipImport,
                        FieldsToIgnore = e.fieldsToIgnore
                    };
                    var entityData = GetEntityData(e);
                    importEntityDefinition.Entities.AddRange(entityData.Select(d => this.ConvertToEntity(meta.EntitySetName, meta.PrimaryIdAttribute, d)));
                    Log.Info($"Exported {entityData.Count} records of {e.logicalname}");
                    importJobDefinition.ImportEntityDefinitions.Add(importEntityDefinition);
                });
                Directory.CreateDirectory(Path.GetDirectoryName(exportDataFilePath));
                File.WriteAllText(exportDataFilePath, JsonConvert.SerializeObject(importJobDefinition, Newtonsoft.Json.Formatting.Indented, JsonSettings.JsonSerializationSettings));
                Log.Info("File write to " + exportDataFilePath);
            }
        }
        private List<Dictionary<string, object>> GetEntityData(entitiesEntity e)
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            int num = 1;
            EntityDataResult entityCollection = null;
            do
            {
                Log.Debug($"Read Page {num} - Cookie: {entityCollection?.PagingCookie}");
                entityCollection = GetNextPage(e.collectionname, e.fetchfilter.InnerXml, entityCollection?.PagingCookie, num++);
                foreach (var r in entityCollection.EntityRecords)
                {
                    foreach (var key in r.Keys)
                    {
                        if (key.StartsWith("_createdby")
                        || (!e.exportcreatedon && key.StartsWith("createdon"))
                        || key.StartsWith("_owning")
                        || key.StartsWith("importsequencenumber")
                        || key.StartsWith("_organizationid")
                        || key.StartsWith("_modifiedby")
                        || key.StartsWith("modifiedon")
                        || (!e.exportowner && key.StartsWith("_ownerid")))
                        {
                            r.Remove(key);
                        }
                    }
                }
                list.AddRange(entityCollection.EntityRecords);
            }
            while (entityCollection.HasMoreRecords);
            return list;
        }

    }
}