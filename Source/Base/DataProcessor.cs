using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using D365.Framework.Tools.DataMigration.Clients;
using D365.Framework.Tools.DataMigration.Types;
using log4net;

namespace D365.Framework.Tools.DataMigration.Base
{
    public abstract class DataProcessor
    {
        private static ILog Log = LogManager.GetLogger(typeof(DataProcessor));
        protected DynamicsClient Client { get; }

        protected bool ErrorOccurred { get; set; }

        public DataProcessor(DynamicsClient client)
        {
            this.Client = client;
        }
        
        protected WebApiEntity ConvertToEntity(string entitySetName, string primaryIdAttribute, Dictionary<string, object> values)
        {
            return new WebApiEntity(entitySetName, new Guid(values[primaryIdAttribute].ToString()))
            {
                Attributes = values
            };
        }

        protected EntityDataResult GetNextPage(string collectionName, string fetchxml, string pagingCookie, int pageNum)
        {
            return this.Client.ExecuteFetchXml(collectionName, CreateXml(fetchxml, pagingCookie, pageNum, 1000));
        }

        protected string CreateXml(string xml, string cookie, int page, int count)
        {
            StringReader input = new StringReader(xml);
            XmlTextReader reader = new XmlTextReader(input);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(reader);
            return CreateXml(xmlDocument, cookie, page, count);
        }

        private string CreateXml(XmlDocument doc, string cookie, int page, int count)
        {
            XmlAttributeCollection attributes = doc.DocumentElement.Attributes;
            if (cookie != null)
            {
                XmlAttribute xmlAttribute = doc.CreateAttribute("paging-cookie");                
                xmlAttribute.Value = this.ExtractPagingCookie(cookie);
                attributes.Append(xmlAttribute);
            }
            XmlAttribute xmlAttribute2 = doc.CreateAttribute("page");
            xmlAttribute2.Value = Convert.ToString(page);
            attributes.Append(xmlAttribute2);
            XmlAttribute xmlAttribute3 = doc.CreateAttribute("count");
            xmlAttribute3.Value = Convert.ToString(count);
            attributes.Append(xmlAttribute3);
            StringBuilder stringBuilder = new StringBuilder();
            StringWriter w = new StringWriter(stringBuilder);
            XmlTextWriter xmlTextWriter = new XmlTextWriter(w);
            doc.WriteTo(xmlTextWriter);
            xmlTextWriter.Close();
            return stringBuilder.ToString();
        }

        private string ExtractPagingCookie(string cookie)
        {
            var xml = new XmlDocument();
            xml.LoadXml(cookie);
            return Uri.UnescapeDataString(Uri.UnescapeDataString(xml.DocumentElement.Attributes["pagingcookie"].Value));
        }
    }
}