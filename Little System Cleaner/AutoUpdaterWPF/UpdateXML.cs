using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Little_System_Cleaner.AutoUpdaterWPF
{
    [XmlRootAttribute("items")]
    public class UpdateXML
    {
        public class Item
        {
            [XmlElement("version")]
            public string VersionString
            {
                get;
                set;
            }

            public Version Version
            {
                get
                {
                    return (!string.IsNullOrEmpty(this.VersionString) ? new Version(this.VersionString) : null);
                }
            }

            [XmlElement("title")]
            public string Title
            {
                get;
                set;
            }

            [XmlElement("changelog")]
            public string ChangeLog
            {
                get;
                set;
            }

            [XmlElement("url")]
            public string URL
            {
                get;
                set;
            }

            [XmlElement("filename")]
            public string FileName
            {
                get;
                set;
            }
        }

        private List<Item> _items = new List<Item>();

        [XmlElement("item")]
        public List<Item> Items
        {
            get;
            set;
        }

        public UpdateXML()
        {

        }
    }
}
