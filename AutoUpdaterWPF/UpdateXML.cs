using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AutoUpdaterWPF
{
    [XmlRoot("items")]
    public class UpdateXml
    {
        [XmlElement("item")]
        public List<Item> Items { get; set; }

        public class Item
        {
            [XmlElement("version")]
            public string VersionString { get; set; }

            public Version Version => !string.IsNullOrEmpty(VersionString) ? new Version(VersionString) : null;

            [XmlElement("title")]
            public string Title { get; set; }

            [XmlElement("changelog")]
            public string ChangeLog { get; set; }

            [XmlElement("url")]
            public string Url { get; set; }

            [XmlElement("filename")]
            public string FileName { get; set; }
        }
    }
}