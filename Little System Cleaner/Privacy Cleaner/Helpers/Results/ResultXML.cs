using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public struct XmlInfo
    {
        public string FilePath;
        public string SearchElement;
        public string SearchElementText;
        public string SearchAttribute;
        public string SearchAttributeText;
    }

    public class ResultXml : ResultNode
    {
        /// <summary>
        /// Constructor for XML file
        /// </summary>
        /// <param name="desc">Description</param>
        /// <param name="xmlPaths">XML Paths</param>
        public ResultXml(string desc, Dictionary<string, List<string>> xmlPaths)
        {
            Description = desc;
            XmlPaths = xmlPaths;
        }

        public override void Clean(Report report)
        {
            foreach (KeyValuePair<string, List<string>> kvp in XmlPaths)
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
                    catch (XPathException ex)
                    {
                        Debug.WriteLine("The following error occurred: {0}\nUnable to find XPath ({1}) in XML file ({2})", ex.Message, xPath, filePath);
                        continue;
                    }

                    if (xmlNodes != null)
                    {
                        foreach (XmlNode xmlNode in xmlNodes)
                        {
                            var parentNode = xmlNode.ParentNode;

                            if (parentNode != null)
                                parentNode.RemoveChild(xmlNode);
                            else
                                xmlDoc.RemoveChild(xmlNode);

                            Settings.Default.lastScanErrorsFixed++;
                        }
                    }

                    report.WriteLine("Removed XML File: {0} Matching XPath: {1}", filePath, xPath);
                }

                xmlDoc.Save(filePath);

            }
        }
    }
}
