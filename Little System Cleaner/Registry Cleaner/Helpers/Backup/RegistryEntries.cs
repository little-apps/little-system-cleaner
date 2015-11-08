using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers.Backup
{
    public class RegistryEntries
    {
        public int Count => RegEntries.Count;

        [XmlAttribute("Created")]
        public long Created
        {
            get { return CreatedDateTime.ToBinary(); }
            set { CreatedDateTime = DateTime.FromBinary(value); }
        }

        [XmlArray("RegistryEntries")]
        [XmlArrayItem("RegistryEntry")]
        public List<RegistryEntry> RegEntries { get; set; } = new List<RegistryEntry>();

        public RegistryEntry this[int i]
        {
            get
            {
                // This indexer is very simple, and just returns or sets 
                // the corresponding element from the internal array. 
                return RegEntries[i];
            }
            set { RegEntries[i] = value; }
        }


        public DateTime CreatedDateTime { get; private set; } = DateTime.Now;

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