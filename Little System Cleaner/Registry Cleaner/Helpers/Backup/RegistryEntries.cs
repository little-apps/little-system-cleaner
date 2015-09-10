using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers.Backup
{
    public class RegistryEntries
    {
        private DateTime _dateTimeCreated = DateTime.Now;
        private List<RegistryEntry> _regEntries = new List<RegistryEntry>();

        public int Count => _regEntries.Count;

        [XmlAttribute("Created")]
        public long Created
        {
            get { return _dateTimeCreated.ToBinary(); }
            set { _dateTimeCreated = DateTime.FromBinary(value); }
        }

        [XmlArray("RegistryEntries")]
        [XmlArrayItem("RegistryEntry")]
        public List<RegistryEntry> RegEntries
        {
            get { return _regEntries; }
            set { _regEntries = value; }
        }

        public RegistryEntry this[int i]
        {
            get
            {
                // This indexer is very simple, and just returns or sets 
                // the corresponding element from the internal array. 
                return RegEntries[i];
            }
            set
            {
                RegEntries[i] = value;
            }
        }


        public DateTime CreatedDateTime => _dateTimeCreated;

        public void Clear()
        {
            RegEntries.Clear();
        }

        public bool Contains(RegistryEntry val)
        {
            return RegEntries.Contains(val);
        }

        public int IndexOf(RegistryEntry val)
        {
            return RegEntries.IndexOf(val);
        }

        public void Add(RegistryEntry val)
        {
            RegEntries.Add(val);
        }
    }
}
