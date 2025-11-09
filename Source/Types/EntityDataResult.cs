using System.Collections.Generic;
using Newtonsoft.Json;

namespace D365.Framework.Tools.DataMigration.Types
{
    public class EntityDataResult
    {
        [JsonProperty("@Microsoft.Dynamics.CRM.fetchxmlpagingcookie")]
        public string PagingCookie { get; set; }
        
        [JsonProperty("@Microsoft.Dynamics.CRM.morerecords")]
        public bool HasMoreRecords { get; set; }

        [JsonProperty("value")]
        public List<Dictionary<string, object>> EntityRecords { get; set; }
    }
}