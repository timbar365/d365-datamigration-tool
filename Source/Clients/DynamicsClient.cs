using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using D365.Framework.Tools.DataMigration.Configuration;
using D365.Framework.Tools.DataMigration.Types;
using log4net;
using Newtonsoft.Json;

namespace D365.Framework.Tools.DataMigration.Clients
{
    public class DynamicsClient
    {
        public static ILog Log = LogManager.GetLogger(typeof(DynamicsClient));
        private WebClient Client { get; }

        private TokenCreator TokenCreator { get; }
        private Uri DynamicsUrl { get; }
        private int Timeout { get; }
        private string ApiUrl { get; }

        public DynamicsClient(Uri dynamicsUrl, TokenCreator tokenCreator, int timeout)
        {
            this.TokenCreator = tokenCreator;
            this.DynamicsUrl = dynamicsUrl;
            this.Timeout = timeout;

            this.Client = new WebClientTimeout(timeout);
            this.Client.Headers["OData-MaxVersion"] = "4.0";
            this.Client.Headers["OData-Version"] = "4.0";
            this.Client.Headers["Accept"] = "application/json";
            this.Client.Headers["Prefer"] = "odata.include-annotations=\"*\"";
            this.Client.Headers["Authorization"] = $"Bearer {this.TokenCreator.GetToken()}";
            this.ApiUrl = new Uri(dynamicsUrl, "api/data/v9.0").AbsoluteUri;
        }

        internal DynamicsClient Clone()
        {
            return new DynamicsClient(this.DynamicsUrl, this.TokenCreator, this.Timeout);
        }

        private EntityDataResult ConvertToEntityDataResult(string result)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<EntityDataResult>(result, JsonSettings.JsonSerializationSettings);
        }

        protected string ExecuteGetRequest(Uri url)
        {
            this.Client.Headers["Authorization"] = $"Bearer {this.TokenCreator.GetToken()}";
            return this.Client.DownloadString(url);
        }

        protected string ExecutePatchRequest(Uri url, string data, bool byPassPlugins)
        {
            try
            {
                this.Client.Headers["Content-Type"] = "application/json"; //Wird nach jedem Call entfernt
                this.Client.Headers["Authorization"] = $"Bearer {this.TokenCreator.GetToken()}";
                this.Client.Headers["MSCRM.BypassCustomPluginExecution"] = byPassPlugins.ToString();

                var result = this.Client.UploadString(url, "PATCH", data);
                Log.Debug("Response Headers:");
                Log.Debug(string.Join(",", this.Client?.ResponseHeaders?.AllKeys?.Where(k => k.StartsWith("x-ms-ratelimit")).Select(k => $"{k}:{this.Client.ResponseHeaders[k]}")));
                return result;
            }
            catch (System.Net.WebException wex)
            {
                this.LogWebException(wex);
                throw;
            }
        }

        //TODO Patch und Post zusammenlegen
        protected string ExecutePostRequest(Uri url, string data, bool byPassPlugins)
        {
            try
            {
                this.Client.Headers["Content-Type"] = "application/json"; //Wird nach jedem Call entfernt
                this.Client.Headers["Authorization"] = $"Bearer {this.TokenCreator.GetToken()}";
                this.Client.Headers["MSCRM.BypassCustomPluginExecution"] = byPassPlugins.ToString();

                var result = this.Client.UploadString(url, "POST", data);
                Log.Debug("Response Headers:");
                Log.Debug(string.Join(",", this.Client?.ResponseHeaders?.AllKeys?.Where(k => k.StartsWith("x-ms-ratelimit")).Select(k => $"{k}:{this.Client.ResponseHeaders[k]}")));
                return result;
            }
            catch (System.Net.WebException wex)
            {
                this.LogWebException(wex);
                throw;
            }
        }


        public EntityDataResult RetrieveByAttribute(string entityCollectionName, string attributeName, string attributeValue)
        {
            try
            {
                var url = new Uri($"{this.ApiUrl}/{entityCollectionName}?$filter={attributeName} eq '{Uri.EscapeDataString(attributeValue)}'");
                var result = this.ExecuteGetRequest(url);
                return ConvertToEntityDataResult(result);
            }
            catch (System.Net.WebException wex)
            {
                this.LogWebException(wex);
                throw;
            }
        }

        public EntityDataResult RetrieveByMultipleAttributes(string entityCollectionName, Dictionary<string, string> attributeValues)
        {
            try
            {
                var filterStrings = attributeValues.Select(a => $"{a.Key} eq '{Uri.EscapeDataString(a.Value)}'").ToArray();
                var filter = string.Join(" and ", filterStrings);
                var url = new Uri($"{this.ApiUrl}/{entityCollectionName}?$filter={filter}");
                var result = this.ExecuteGetRequest(url);
                return ConvertToEntityDataResult(result);
            }
            catch (System.Net.WebException wex)
            {
                this.LogWebException(wex);
                throw;
            }
        }

        public EntityDataResult ExecuteFetchXml(string entityCollectionName, string fetchXml)
        {
            try
            {
                var url = new Uri($"{this.ApiUrl}/{entityCollectionName}?fetchXml={Uri.EscapeDataString(fetchXml)}");
                Log.Debug(url);
                var result = this.ExecuteGetRequest(url);
                return ConvertToEntityDataResult(result);
            }
            catch (WebException wex)
            {
                this.LogWebException(wex);
                throw;
            }
        }

        public string Upsert(string entityCollectionName, Guid id, Dictionary<string, object> data, bool byPassPlugins)
        {
            var url = new Uri($"{this.ApiUrl}/{entityCollectionName}({id})");
            data.Keys.Where(k => k.Contains("@Microsoft.Dynamics.CRM.") || k.Contains("@OData.Community.")).ToList().ForEach(k =>
            {
                data.Remove(k);
            });

            var body = JsonConvert.SerializeObject(data);
            var result = this.ExecutePatchRequest(url, body, byPassPlugins);
            return result;
        }

        public void Associate(string entityCollectionName1, string id1, string entityCollectionName2, string id2, string relationshipName, bool byPassPlugins)
        {
            var url = new Uri($"{this.ApiUrl}/{entityCollectionName1}({id1})/{relationshipName}/$ref");
            var body = $"{{ \"@odata.id\": \"{this.ApiUrl}/{entityCollectionName2}({id2})\"}}";
            this.ExecutePostRequest(url, body, byPassPlugins);
        }


        private Dictionary<string, EntityMetadata> EntityMetadataCache { get; } = new Dictionary<string, EntityMetadata>();

        public EntityMetadata GetEntityMetadata(string logicalName, bool useCache = true)
        {
            if (useCache && this.EntityMetadataCache.ContainsKey(logicalName))
                return this.EntityMetadataCache[logicalName];

            var url = new Uri($"{this.ApiUrl}/EntityDefinitions(LogicalName='{logicalName}')");
            Log.Debug(url);
            var result = this.ExecuteGetRequest(url);
            var meta = JsonConvert.DeserializeObject<EntityMetadata>(result);
            if (useCache)
                this.EntityMetadataCache.Add(logicalName, meta);
            return meta;
        }

        private Dictionary<string, AttributeMetadata> AttributeMetadataCache { get; } = new Dictionary<string, AttributeMetadata>();
        public AttributeMetadata GetAttributeMetadata(string entitylogicalName, string attributelogicalName, bool useCache = true)
        {
            var cachekey = $"{entitylogicalName}::{attributelogicalName}";

            if (useCache && this.AttributeMetadataCache.ContainsKey(cachekey))
                return this.AttributeMetadataCache[cachekey];

            var url = new Uri($"{this.ApiUrl}/EntityDefinitions(LogicalName='{entitylogicalName}')/Attributes?$filter=LogicalName eq '{attributelogicalName}'");
            Log.Debug(url);
            var result = this.ExecuteGetRequest(url);
            var meta = JsonConvert.DeserializeObject<AttributeMetadataList>(result).List.SingleOrDefault();
            if (meta == null)
                throw new Exception($"Cannot find attribute metadata: {cachekey}");
            if (useCache)
                this.AttributeMetadataCache.Add(cachekey, meta);
            return meta;
        }

        private Dictionary<string, RelationshipMetadata> RelationshipMetadataCache { get; } = new Dictionary<string, RelationshipMetadata>();
        public RelationshipMetadata GetRelationshipMetadataOneToMany(string entitylogicalName, string relationshipName, bool useCache = true)
        {
            var cachekey = $"{entitylogicalName}::{relationshipName}::1toM";

            if (useCache && this.RelationshipMetadataCache.ContainsKey(cachekey))
                return this.RelationshipMetadataCache[cachekey];

            var url = new Uri($"{this.ApiUrl}/EntityDefinitions(LogicalName='{entitylogicalName}')/OneToManyRelationships?$filter=SchemaName eq '{relationshipName}'");
            Log.Debug(url);
            var result = this.ExecuteGetRequest(url);
            var meta = JsonConvert.DeserializeObject<RelationshipMetadataList>(result).List.SingleOrDefault();
            if (meta == null)
                throw new Exception($"Cannot find relationship metadata: {cachekey}");
            if (useCache)
                this.RelationshipMetadataCache.Add(cachekey, meta);
            return meta;
        }

        public RelationshipMetadata GetRelationshipMetadataManyToOne(string entitylogicalName, string attributeName, string referencedEntityLogicalName, bool useCache = true)
        {
            var cachekey = $"{entitylogicalName}::{attributeName}::Mto1";

            if (useCache && this.RelationshipMetadataCache.ContainsKey(cachekey))
                return this.RelationshipMetadataCache[cachekey];

            var url = new Uri($"{this.ApiUrl}/EntityDefinitions(LogicalName='{entitylogicalName}')/ManyToOneRelationships?$filter=ReferencingAttribute eq '{attributeName}' and ReferencedEntity eq '{referencedEntityLogicalName}'");
            Log.Debug(url);
            var result = this.ExecuteGetRequest(url);
            var meta = JsonConvert.DeserializeObject<RelationshipMetadataList>(result).List.SingleOrDefault();
            if (meta == null)
                throw new Exception($"Cannot find relationship metadata: {cachekey}");
            if (useCache)
                this.RelationshipMetadataCache.Add(cachekey, meta);
            return meta;
        }

        private Dictionary<string, RelationshipM2MMetadata> RelationshipM2MMetadataCache { get; } = new Dictionary<string, RelationshipM2MMetadata>();
        public RelationshipM2MMetadata GetRelationshipMetadataManyToMany(string intersectEntitylogicalName, bool useCache = true)
        {
            var cachekey = $"{intersectEntitylogicalName}::_::MtoM";

            if (useCache && this.RelationshipM2MMetadataCache.ContainsKey(cachekey))
                return this.RelationshipM2MMetadataCache[cachekey];

            var url = new Uri($"{this.ApiUrl}/EntityDefinitions(LogicalName='{intersectEntitylogicalName}')/ManyToManyRelationships");
            Log.Debug(url);
            var result = this.ExecuteGetRequest(url);
            var meta = JsonConvert.DeserializeObject<RelationshipM2MMetadataList>(result).List.SingleOrDefault();
            if (meta == null)
                throw new Exception($"Cannot find intersect relationship metadata: {cachekey}");
            if (useCache)
                this.RelationshipM2MMetadataCache.Add(cachekey, meta);
            return meta;
        }


        private void LogWebException(WebException ex)
        {
            String responseFromServer = ex.Message.ToString() + " ";
            if (ex.Response != null)
            {
                using (WebResponse response = ex.Response)
                {
                    Stream dataRs = response.GetResponseStream();
                    using (StreamReader reader = new StreamReader(dataRs))
                    {
                        responseFromServer += reader.ReadToEnd();
                    }
                }
            }
            Log.Error("Server Response: " + responseFromServer);
        }
    }
}