using Newtonsoft.Json;

namespace D365.Framework.Tools.DataMigration.Types
{
    public class EntityMetadata
    {
        [JsonProperty("LogicalName")]
        public string LogicalName { get; set; }
        [JsonProperty("EntitySetName")]
        public string EntitySetName { get; set; }
        [JsonProperty("ObjectTypeCode")]
        public int ObjectTypeCode { get; set; }
        [JsonProperty("PrimaryNameAttribute")]
        public string PrimaryNameAttribute { get; set; }
        [JsonProperty("PrimaryIdAttribute")]
        public string PrimaryIdAttribute { get; set; }
        [JsonProperty("OwnershipType")]
        public string OwnershipType { get; set; }
        [JsonProperty("IsIntersect")]
        public bool IsIntersect { get; set; }

    }
}