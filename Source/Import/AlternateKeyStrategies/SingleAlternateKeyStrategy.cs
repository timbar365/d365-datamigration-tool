using D365.Framework.Tools.DataMigration.Clients;
using D365.Framework.Tools.DataMigration.Helper;
using D365.Framework.Tools.DataMigration.Types;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Framework.Tools.DataMigration.Import.AlternateKeyStrategies
{
    /// <summary>
    /// Mapping of single alternate key
    /// </summary>
    public class SingleAlternateKeyStrategy
    {
        private static ILog Log = LogManager.GetLogger(typeof(AlternateKeyMapper));
        private static Dictionary<string, Dictionary<string, Guid?>> AlternateKeyCache;
        private static Dictionary<string, Dictionary<Guid, Guid?>> AlternateKeyIdMappingCache;

        private DynamicsClient Client { get; }

        public SingleAlternateKeyStrategy(DynamicsClient client, Dictionary<string,
            Dictionary<string, Guid?>> alternateKeyCache, Dictionary<string,
            Dictionary<Guid, Guid?>> alternateKeyIdMappingCache)
        {
            Client = client;
            AlternateKeyCache = alternateKeyCache;
            AlternateKeyIdMappingCache = alternateKeyIdMappingCache;
        }

        public void ProcessAlternateKeys(ImportJobDefinition importJob, ImportEntityDefinition entityDef)
        {

            lock (AlternateKeyCache)
            {
                //Ensure alternate key cache for entity
                if (!AlternateKeyCache.ContainsKey(entityDef.EntityLogicalName))
                {
                    AlternateKeyCache.Add(entityDef.EntityLogicalName, new Dictionary<string, Guid?>());
                    AlternateKeyIdMappingCache.Add(entityDef.EntityLogicalName, new Dictionary<Guid, Guid?>());
                }
            }

            Log.Info($"Mapping alternate keys for {entityDef.EntityCollectionName}");

            entityDef.Entities.ForEach(entity =>
            {
                //Id für den Datensatz auf Basis des alternativen Keys ermitteln
                if (entity.Contains(entityDef.AlternateKeyAttribute))
                {
                    var oldId = entity.Id;
                    entity.Id = GetIdByAlternateKey(entityDef, entity[entityDef.AlternateKeyAttribute].ToString(), oldId) ?? entity.Id;
                    entity[entityDef.PrimaryAttributeId] = entity.Id.ToString();

                    //Update all lookups in all entities - if changed
                    if (oldId != entity.Id)
                    {
                        importJob.ImportEntityDefinitions.ForEach(def => def.Entities.ForEach(e =>
                        {
                            var list = e.Attributes.Where(a => a.Value != null && a.Key.StartsWith("_") && a.Key.EndsWith("_value")
                                                    && e.Attributes[a.Key + "@Microsoft.Dynamics.CRM.lookuplogicalname"]?.ToString().ToLower() == entityDef.EntityLogicalName
                                                    && new Guid(a.Value.ToString()) == oldId).ToList();
                            list.ForEach(a => e[a.Key] = entity.Id.ToString());
                        }));
                    }
                }
            });
        }


        private Guid? GetIdByAlternateKey(ImportEntityDefinition def, string alternateKeyValue, Guid oldId)
        {
            Log.Debug($"Get alternate key for {def.EntityCollectionName} with value {alternateKeyValue}");
            if (alternateKeyValue == null)
            {
                return null;
            }
            if (!AlternateKeyCache[def.EntityLogicalName].ContainsKey(alternateKeyValue))
            {
                RetryHelper.Do((attemptCnt) =>
                {
                    Log.Debug($"Read alternate key for {def.EntityCollectionName} with value {alternateKeyValue}, Attempt: {attemptCnt}");
                    var id = Client.RetrieveByAttribute(def.EntityCollectionName, def.AlternateKeyAttribute, alternateKeyValue).EntityRecords.SingleOrDefault()?[def.PrimaryAttributeId]?.ToString();
                    
                    AlternateKeyCache[def.EntityLogicalName].Add(alternateKeyValue, id != null ? new Guid(id) : null);
                    AlternateKeyIdMappingCache[def.EntityLogicalName].Add(oldId, id != null ? new Guid(id) : null);

                }, TimeSpan.FromSeconds(2));
            }
            return AlternateKeyCache[def.EntityLogicalName][alternateKeyValue];
        }
    }
}