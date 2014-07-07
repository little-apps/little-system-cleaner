using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public ResultXML(string desc, Dictionary<string, List<string>> xmlPaths)
        {
            this.Description = desc;
            this.XMLPaths = xmlPaths;
        }

        public override void Clean(Report report)
        {
            foreach (KeyValuePair<string, List<string>> kvp in this.XMLPaths)
            {
                string filePath = kvp.Key;
                List<string> xPaths = kvp.Value;

                XmlDocument xmlDoc = new XmlDocument();

                try
                {
                    xmlDoc.Load(filePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: {0}\nUnable to load XML file ({1})", ex.Message, filePath);
                    continue;
                }


                foreach (string xPath in xPaths)
                {
                    XmlNodeList xmlNodes;

                    try
                    {
                        xmlNodes = xmlDoc.SelectNodes(xPath);
                    }
                    catch (System.Xml.XPath.XPathException ex)
                    {
                        Debug.WriteLine("The following error occurred: {0}\nUnable to find XPath ({1}) in XML file ({1})", ex.Message, xPath, filePath);
                        continue;
                    }

                    foreach (XmlNode xmlNode in xmlNodes)
                    {
                        XmlNode parentNode = xmlNode.ParentNode;

                        if (parentNode != null)
                            parentNode.RemoveChild(xmlNode);
                        else
                            xmlDoc.RemoveChild(xmlNode);

                        Properties.Settings.Default.lastScanErrorsFixed++;
                    }
                    
                    report.WriteLine("Removed XML File: {0} Matching XPath: {0}", filePath, xPath);
                }

                xmlDoc.Save(filePath);
            }
        }
    }
}
