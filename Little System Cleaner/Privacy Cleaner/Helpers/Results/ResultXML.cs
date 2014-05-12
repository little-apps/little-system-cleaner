using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public struct XMLInfo
    {
        public string filePath;
        public string searchElement;
        public string searchElementText;
        public string searchAttribute;
        public string searchAttributeText;
    }

    public class ResultXML : ResultNode
    {
        /// <summary>
        /// Constructor for XML file
        /// </summary>
        /// <param name="desc">Description</param>
        /// <param name="xmlInfo">XML Info Array</param>
        public ResultXML(string desc, Dictionary<string, string> xmlPaths)
        {
            this.Description = desc;
            this.XMLPaths = xmlPaths;
        }

        public override void Clean(Report report)
        {
            foreach (KeyValuePair<string, string> kvp in this.XMLPaths)
            {
                string filePath = kvp.Key;
                string xPath = kvp.Value;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);
                foreach (XmlNode xmlNode in xmlDoc.SelectNodes(xPath))
                {
                    XmlNode parentNode = xmlNode.ParentNode;
                    if (parentNode != null)
                        parentNode.RemoveChild(xmlNode);
                    else
                        xmlDoc.RemoveChild(xmlNode);
                }
                xmlDoc.Save(filePath);
                report.WriteLine("Removed XML File: {0} Matching XPath: {0}", filePath, xPath);
            }
        }
    }
}
