using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    [Serializable]
    public class IncludeDrive : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

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
