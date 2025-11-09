using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using D365.Framework.Tools.DataMigration.Configuration;
using D365.Framework.Tools.DataMigration.Types;
using Newtonsoft.Json;

namespace D365.Framework.Tools.DataMigration.Import
{
    public class OwnerMapper
    {
        private OwnerMapping OwnerMapping { get; }

        public OwnerMapper(string pathMappingFile)
        {
            if (pathMappingFile != null)
                this.OwnerMapping = JsonConvert.DeserializeObject<OwnerMapping>(File.ReadAllText(pathMappingFile), JsonSettings.JsonSerializationSettings);
        }

        public void MapOwner(Dictionary<string,object> attributes)
        {
            if (this.OwnerMapping != null && attributes.ContainsKey("_ownerid_value"))
            {
                //Get current owner id
                var currentOwnerId = attributes["_ownerid_value"]?.ToString();
                if (currentOwnerId != null)
                {
                    //Search for mapping
                    var newOwner = this.OwnerMapping.Map.Where(m => m.SourceId?.ToLower() == currentOwnerId.ToLower()).FirstOrDefault();

                    //if found
                    if (newOwner != null)
                    {
                        //replace
                        attributes["_ownerid_value"] = newOwner.TargetId;
                    }
                }
            }
        }
    }
}