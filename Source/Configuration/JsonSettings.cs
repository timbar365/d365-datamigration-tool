using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace D365.Framework.Tools.DataMigration.Configuration
{
    public static class JsonSettings
    {
        public static JsonSerializerSettings JsonSerializationSettings { get; } = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,            
            ContractResolver = new OrderedContractResolver(),                      
            Converters = new List<JsonConverter>
            {
                // new AttributeCollectionConverter(),
                // new IgnorableCollectionConverter(),
                // new SimpleTypeConverter()
            }
        };

        private class OrderedContractResolver : DefaultContractResolver
        {
            protected override System.Collections.Generic.IList<JsonProperty> CreateProperties(System.Type type, MemberSerialization memberSerialization)
            {
                return base.CreateProperties(type, memberSerialization).OrderBy(p => p.PropertyName).ToList();
            }
        }
    }
}