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
        //[Serializable, XmlRoot("item")]
        public class Item
        {
            private string _versionString;
            private string _title;
            private string _changelog;
            private string _url;

            [XmlElement("version")]
            public string VersionString
            {
                get { return this._versionString; }
                set { this._versionString = value; }
            }

            public Version Version
            {
                get 
                {
                    if (string.IsNullOrEmpty(this._versionString))
                        return null;

                    return new Version(this._versionString); 
                }
            }

            [XmlElement("title")]
            public string Title
            {
                get { return this._title; }
                set { this._title = value; }
            }

            [XmlElement("changelog")]
            public string ChangeLog
            {
                get { return this._changelog; }
                set { this._changelog = value; }
            }

            [XmlElement("url")]
            public string URL
            {
                get { return this._url; }
                set { this._url = value; }
            }
        }

        private List<Item> _items = new List<Item>();

        [XmlElement("item")]
        public List<Item> Items
        {
            get { return this._items; }
            set { this._items = value; }
        }

        public UpdateXML()
        {

        }
    }
}
