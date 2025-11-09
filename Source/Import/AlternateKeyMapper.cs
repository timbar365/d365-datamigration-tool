using System;
using System.Collections.Generic;
using System.Linq;
using D365.Framework.Tools.DataMigration.Clients;
using D365.Framework.Tools.DataMigration.Helper;
using D365.Framework.Tools.DataMigration.Import.AlternateKeyStrategies;
using D365.Framework.Tools.DataMigration.Types;
using log4net;

public class AlternateKeyMapper
{
    private static ILog Log = LogManager.GetLogger(typeof(AlternateKeyMapper));
    private readonly CombinedAlternateKeyStrategy combinedKeystrategy;
    private readonly SingleAlternateKeyStrategy singleKeystrategy;
    private static Dictionary<string, Dictionary<string, Guid?>> AlternateKeyCache = new Dictionary<string, Dictionary<string, Guid?>>();
    private static Dictionary<string, Dictionary<Guid, Guid?>> AlternateKeyIdMappingCache = new Dictionary<string, Dictionary<Guid, Guid?>>();


    private DynamicsClient Client { get; }

    public AlternateKeyMapper(DynamicsClient client)
    {
        this.Client = client;
        combinedKeystrategy = new CombinedAlternateKeyStrategy(client, AlternateKeyCache, AlternateKeyIdMappingCache);
        singleKeystrategy = new SingleAlternateKeyStrategy(client, AlternateKeyCache, AlternateKeyIdMappingCache);
    }

    public void ProcessAlternateKeys(ImportJobDefinition importJob)
    {
        importJob.ImportEntityDefinitions.Where(ed => !string.IsNullOrWhiteSpace(ed.AlternateKeyAttribute)).ToList().ForEach((entityDef) =>
        {
            if (entityDef.AlternateKeyAttribute.Contains(";"))
            {
                Log.Debug("Mapping combined alternate keys");
                combinedKeystrategy.ProcessAlternateKeys(importJob, entityDef);
            }
            else
            {
                Log.Debug("Mapping single alternate key");
                singleKeystrategy.ProcessAlternateKeys(importJob, entityDef);
            }
        });
    }

    /// <summary>
    /// Gets Key from Mapping Cache (Id to Id)
    /// </summary>
    /// <param name="entityName">Name of Entity</param>
    /// <param name="id">Primary Key (Guid)</param>
    /// <returns></returns>
    public string GetKeyFromMapping(string entityName, string id)
    {
        var uid = new Guid(id);

        if (!AlternateKeyIdMappingCache.ContainsKey(entityName))
        {
            return uid.ToString();
        }

        if (!AlternateKeyIdMappingCache[entityName].ContainsKey(uid))
        {
            return uid.ToString();
        }

        return AlternateKeyIdMappingCache[entityName][uid].ToString() ?? uid.ToString();
    }

}