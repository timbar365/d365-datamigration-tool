using System.Collections.Generic;
using Newtonsoft.Json;

namespace D365.Framework.Tools.DataMigration.Types
{
    public class EntityMapping
    {

        public class FieldMap
        {
            public string Source { get; set; }
            public string Target { get; set; }
        }
        
        public class EntityMap
        {
            public string Source { get; set; }            
            public string Target { get; set; }
            public List<FieldMap> Fields { get; set; }
        }

        [JsonProperty("EntityMappings")]
        public List<EntityMap> EntityMappings { get; set; }
    }
}