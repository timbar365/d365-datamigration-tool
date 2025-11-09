using D365.Framework.Tools.DataMigration.Clients;
using D365.Framework.Tools.DataMigration.Helper;
using D365.Framework.Tools.DataMigration.Types;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;

namespace D365.Framework.Tools.DataMigration.Import.AlternateKeyStrategies
{
    /// <summary>
    /// Mapping of combined alternate keys split by ";"
    /// </summary>
    public class CombinedAlternateKeyStrategy
    {
        private static ILog Log = LogManager.GetLogger(typeof(CombinedAlternateKeyStrategy));
        private static Dictionary<string, Dictionary<string, Guid?>> AlternateKeyCache;
        private static Dictionary<string, Dictionary<Guid, Guid?>> AlternateKeyIdMappingCache;

        private DynamicsClient Client { get; }

        public CombinedAlternateKeyStrategy(DynamicsClient client,
            Dictionary<string, Dictionary<string, Guid?>> alternateKeyCache,
            Dictionary<string, Dictionary<Guid, Guid?>> alternateKeyIdMappingCache)
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

            Log.Info($"Mapping combined alternate keys for {entityDef.EntityCollectionName}");

            entityDef.Entities.ForEach(entity =>
            {
                var combinedAlternatekeys = entityDef.AlternateKeyAttribute.Split(";");

                //Id für den Datensatz auf Basis des alternativen Keys ermitteln
                if (combinedAlternatekeys.All(key => entity.Contains(key)))
                {
                    var entityValues = combinedAlternatekeys.Select(key => new KeyValuePair<string, string>(key, entity[key].ToString())).ToDictionary(a => a.Key, a => a.Value);

                    var oldId = entity.Id;
                    entity.Id = GetIdByMultipleAlternateKeys(entityDef, entityValues, entityDef.AlternateKeyAttribute, oldId) ?? entity.Id;
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


        private Guid? GetIdByMultipleAlternateKeys(ImportEntityDefinition def, Dictionary<string, string> alternateKeyValues, string multipleAlternateKey, Guid oldId)
        {
            Log.Debug($"Get combined alternate key for {def.EntityCollectionName} with value {alternateKeyValues.Select(x => x.Value).Split(", ")}");
            if (alternateKeyValues.Any(a => a.Value == null))
            {
                return null;
            }
            if (!AlternateKeyCache[def.EntityLogicalName].ContainsKey(multipleAlternateKey))
            {
                RetryHelper.Do((attemptCnt) =>
                {
                    Log.Debug($"Read combined alternate key for {def.EntityCollectionName} with value {alternateKeyValues.Select(x => x.Value).Split(", ")}, Attempt: {attemptCnt}");
                    var id = this.Client.RetrieveByMultipleAttributes(def.EntityCollectionName, alternateKeyValues).EntityRecords.SingleOrDefault()?[def.PrimaryAttributeId]?.ToString();
                    AlternateKeyCache[def.EntityLogicalName].Add(multipleAlternateKey, id != null ? new Guid(id) : (Guid?)null);
                    AlternateKeyIdMappingCache[def.EntityLogicalName].Add(oldId, id != null ? new Guid(id) : null);
                }, TimeSpan.FromSeconds(2));
            }
            return AlternateKeyCache[def.EntityLogicalName][multipleAlternateKey];
        }
    }
}
