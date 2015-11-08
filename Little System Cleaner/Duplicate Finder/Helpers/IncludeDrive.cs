using System.IO;
using System.Xml.Serialization;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    [XmlInclude(typeof (IncludeDrive))]
    public class IncludeDrive
    {
        public IncludeDrive()
        {
        }

        public IncludeDrive(DriveInfo drive)
        {
            Name = drive.ToString();
        }

        public bool? IsChecked { get; set; } = true;

        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            return obj is IncludeDrive ? Equals((IncludeDrive) obj) : base.Equals(obj);
        }

        public bool Equals(IncludeDrive other)
        {
            return other != null && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}