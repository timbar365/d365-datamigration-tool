using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;

namespace D365.Framework.Tools.DataMigration.Types
{

    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class entities
    {
        private entitiesEntity[] entityField;

        [XmlElement("entity")]
        public entitiesEntity[] entity
        {
            get
            {
                return entityField;
            }
            set
            {
                entityField = value;
            }
        }
    }

    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class entitiesEntity
    {
        private string alternatekeyfield;

        private entitiesEntityField[] fieldsField;

        private object relationshipsField;

        private XmlElement fetchfilterField;

        private string logicalnameField;
        private string collectionnameField;

        private bool exportownerField;

        private bool exportcreatedonField;

        private string primaryidfieldField;

        private string primarynamefieldField;

        private bool disablepluginsField;

        private bool deactivateAllRecordsField;
        private string fieldsToIgnoreField;

        private bool ensureWithAllFieldsField;
        private bool skipImportField;

        private string ensureWithFieldsField;

        [XmlArrayItem("field", IsNullable = false)]
        public entitiesEntityField[] fields
        {
            get
            {
                return fieldsField;
            }
            set
            {
                fieldsField = value;
            }
        }

        public object relationships
        {
            get
            {
                return relationshipsField;
            }
            set
            {
                relationshipsField = value;
            }
        }

        [XmlAnyElement]
        public XmlElement fetchfilter
        {
            get
            {
                return fetchfilterField;
            }
            set
            {
                fetchfilterField = value;
            }
        }

        [XmlAttribute]
        public string logicalname
        {
            get
            {
                return logicalnameField;
            }
            set
            {
                logicalnameField = value;
            }
        }

        [XmlAttribute]
        public bool exportowner
        {
            get
            {
                return exportownerField;
            }
            set
            {
                exportownerField = value;
            }
        }

        [XmlAttribute]
        public bool exportcreatedon
        {
            get
            {
                return exportcreatedonField;
            }
            set
            {
                exportcreatedonField = value;
            }
        }

        [XmlIgnore]
        public string collectionname
        {
            get
            {
                return collectionnameField;
            }
            set
            {
                collectionnameField = value;
            }
        }

        [XmlIgnore]
        public string primaryidfield
        {
            get
            {
                return primaryidfieldField;
            }
            set
            {
                primaryidfieldField = value;
            }
        }

        [XmlIgnore]
        public string primarynamefield
        {
            get
            {
                return primarynamefieldField;
            }
            set
            {
                primarynamefieldField = value;
            }
        }

        [XmlAttribute]
        public bool disableplugins
        {
            get
            {
                return disablepluginsField;
            }
            set
            {
                disablepluginsField = value;
            }
        }

        [XmlAttribute]
        public bool deactivateAllRecords
        {
            get
            {
                return deactivateAllRecordsField;
            }
            set
            {
                deactivateAllRecordsField = value;
            }
        }

        [XmlAttribute]
        public string fieldsToIgnore
        {
            get
            {
                return fieldsToIgnoreField;
            }
            set
            {
                fieldsToIgnoreField = value;
            }
        }

        [XmlAttribute]
        public bool ensureWithAllFields
        {
            get
            {
                return ensureWithAllFieldsField;
            }
            set
            {
                ensureWithAllFieldsField = value;
            }
        }
        
        [XmlAttribute]
        public bool skipImport
        {
            get
            {
                return skipImportField;
            }
            set
            {
                skipImportField = value;
            }
        }

        [XmlAttribute]
        public string ensureWithFields
        {
            get
            {
                return ensureWithFieldsField;
            }
            set
            {
                ensureWithFieldsField = value;
            }
        }

        [XmlAttribute]
        public string alternateKeyField
        {
            get
            {
                return alternatekeyfield;
            }
            set
            {
                alternatekeyfield = value;
            }
        }
    }

    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class entitiesEntityField
    {
        private string displaynameField;

        private string nameField;

        private string typeField;

        private bool customfieldField;

        private bool customfieldFieldSpecified;

        private bool primaryKeyField;

        private bool primaryKeyFieldSpecified;

        private string lookupTypeField;

        [XmlAttribute]
        public string displayname
        {
            get
            {
                return displaynameField;
            }
            set
            {
                displaynameField = value;
            }
        }

        [XmlAttribute]
        public string name
        {
            get
            {
                return nameField;
            }
            set
            {
                nameField = value;
            }
        }

        [XmlAttribute]
        public string type
        {
            get
            {
                return typeField;
            }
            set
            {
                typeField = value;
            }
        }

        [XmlAttribute]
        public bool customfield
        {
            get
            {
                return customfieldField;
            }
            set
            {
                customfieldField = value;
            }
        }

        [XmlIgnore]
        public bool customfieldSpecified
        {
            get
            {
                return customfieldFieldSpecified;
            }
            set
            {
                customfieldFieldSpecified = value;
            }
        }

        [XmlAttribute]
        public bool primaryKey
        {
            get
            {
                return primaryKeyField;
            }
            set
            {
                primaryKeyField = value;
            }
        }

        [XmlIgnore]
        public bool primaryKeySpecified
        {
            get
            {
                return primaryKeyFieldSpecified;
            }
            set
            {
                primaryKeyFieldSpecified = value;
            }
        }

        [XmlAttribute]
        public string lookupType
        {
            get
            {
                return lookupTypeField;
            }
            set
            {
                lookupTypeField = value;
            }
        }
    }

}