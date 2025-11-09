using System.Collections.Generic;
using Newtonsoft.Json;

namespace D365.Framework.Tools.DataMigration.Types
{

    public class AttributeMetadataList {
         [JsonProperty("value")]
        public List<AttributeMetadata> List { get; set; }
    }

    public class AttributeMetadata
    {
        [JsonProperty("SchemaName")]
        public string SchemaName { get; set; }
        [JsonProperty("EntityLogicalName")]
        public string EntityLogicalName { get; set; }
    }
}