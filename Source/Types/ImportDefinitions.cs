using System;
using System.Collections.Generic;

namespace D365.Framework.Tools.DataMigration.Types
{
    public class ImportEntityDefinition
    {
        public bool DisablePlugins
        {
            get;
            set;

        }
        public bool DeactivateAllRecords
        {
            get;
            set;
        }

        public bool EnsureWithAllFields
        {
            get;
            set;
        }

        public bool SkipImport
        {
            get;
            set;
        }

        public string EnsureWithFields
        {
            get;
            set;
        }

        public string FieldsToIgnore
        {
            get;
            set;
        }

        public string EntityLogicalName
        {
            get;
            set;
        }

        public string EntityCollectionName
        {
            get;
            set;
        }

        public string PrimaryAttributeId
        {
            get;
            set;
        }

        public string PrimaryAttributeName
        {
            get;
            set;
        }

        public string AlternateKeyAttribute
        {
            get;
            set;
        }


        public List<WebApiEntity> Entities
        {
            get;
            set;
        } = new List<WebApiEntity>();

    }

    public class WebApiEntity
    {

        public WebApiEntity(string collectionname, Guid id)
        {
            this.CollectionName = collectionname;
            this.Id = id;
        }
        public string CollectionName { get; set; }
        public Guid Id { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        public object this[string s]
        {
            get { return this.Attributes[s]; }
            set { this.Attributes[s] = value; }
        }

        public bool Contains(string key)
        {
            return this.Attributes.ContainsKey(key);
        }
    }

    public class ImportJobDefinition
    {
        public bool EnsureRecordAvailabilty
        {
            get;
            set;
        }

        public List<ImportEntityDefinition> ImportEntityDefinitions
        {
            get;
            set;
        } = new List<ImportEntityDefinition>();
    }
}