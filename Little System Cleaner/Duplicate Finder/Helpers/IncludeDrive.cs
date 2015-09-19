using System;
using System.ComponentModel;
using System.IO;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    [Serializable]
    public class IncludeDrive
    {
        public bool? IsChecked { get; set; } = true;

        public string Name { get; }

        public IncludeDrive(DriveInfo drive)
        {
            Name = drive.ToString();
        }

        public override bool Equals(object obj)
        {
            return (obj is IncludeDrive ? Equals(obj as IncludeDrive) : base.Equals(obj));
        }

        public bool Equals(IncludeDrive other)
        {
            return (other != null && Name == other.Name);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
