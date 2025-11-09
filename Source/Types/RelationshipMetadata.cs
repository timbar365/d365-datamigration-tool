using System.Collections.Generic;
using Newtonsoft.Json;

namespace D365.Framework.Tools.DataMigration.Types
{

    public class RelationshipMetadataList {
         [JsonProperty("value")]
        public List<RelationshipMetadata> List { get; set; }
    }

    public class RelationshipMetadata
    {
        [JsonProperty("SchemaName")]
        public string SchemaName { get; set; }
        [JsonProperty("ReferencingEntity")]
        public string ReferencingEntity { get; set; }
         [JsonProperty("ReferencingAttribute")]
        public string ReferencingAttribute { get; set; }
         [JsonProperty("ReferencingEntityNavigationPropertyName")]
        public string ReferencingEntityNavigationPropertyName { get; set; }
         [JsonProperty("ReferencedEntity")]
        public string ReferencedEntity { get; set; }
        [JsonProperty("ReferencedAttribute")]
        public string ReferencedAttribute { get; set; }
    }

    public class RelationshipM2MMetadataList {
         [JsonProperty("value")]
        public List<RelationshipM2MMetadata> List { get; set; }
    }

    public class RelationshipM2MMetadata {
        [JsonProperty("Entity1LogicalName")]
        public string Entity1LogicalName { get; set; }

        [JsonProperty("Entity2LogicalName")]
        public string Entity2LogicalName { get; set; }

        [JsonProperty("IntersectEntityName")]
        public string IntersectEntityName { get; set; }

        [JsonProperty("Entity1IntersectAttribute")]
        public string Entity1IntersectAttribute { get; set; }

        [JsonProperty("Entity2IntersectAttribute")]
        public string Entity2IntersectAttribute { get; set; }

        [JsonProperty("Entity1NavigationPropertyName")]
        public string Entity1NavigationPropertyName { get; set; }

        [JsonProperty("Entity2NavigationPropertyName")]
        public string Entity2NavigationPropertyName { get; set; }

        [JsonProperty("IsCustomRelationship")]
        public bool IsCustomRelationship { get; set; }

        [JsonProperty("IsValidForAdvancedFind")]
        public bool IsValidForAdvancedFind { get; set; }

        [JsonProperty("SchemaName")]
        public string SchemaName { get; set; }

        [JsonProperty("SecurityTypes")]
        public string SecurityTypes { get; set; }

        [JsonProperty("IsManaged")]
        public bool IsManaged { get; set; }

        [JsonProperty("RelationshipType")]
        public string RelationshipType { get; set; }

        [JsonProperty("IntroducedVersion")]
        public string IntroducedVersion { get; set; }

        [JsonProperty("MetadataId")]
        public string MetadataId { get; set; }
    }
}