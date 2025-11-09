// OwnerMapping myDeserializedClass = JsonConvert.DeserializeObject<OwnerMapping>(myJsonResponse);
using System.Collections.Generic;
using Newtonsoft.Json;

namespace D365.Framework.Tools.DataMigration.Types
{
    public class OwnerMapping
    {

        public class Mapping
        {
            public string SourceId { get; set; }
            public string TargetId { get; set; }
        }

        [JsonProperty("OwnerMapping")]
        public List<Mapping> Map { get; set; }
    }
}