using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class IncludeFolder : INotifyPropertyChanged, IEquatable<IncludeFolder>
    {
        private bool? _isChecked = true;

        private string _name;

        public IncludeFolder()
        {
        }

        public IncludeFolder(DirectoryInfo dirInfo)
        {
            Name = dirInfo.ToString();
            DirInfo = dirInfo;
        }

        public IncludeFolder(string folderPath)
        {
            Name = folderPath;
            DirInfo = new DirectoryInfo(folderPath);
        }


        public bool? IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));

                if (DirInfo == null)
                    DirInfo = new DirectoryInfo(_name);
            }
        }

        [XmlIgnore]
        public DirectoryInfo DirInfo { get; private set; }

        public bool Equals(IncludeFolder other)
        {
            return other != null && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            var a = obj as IncludeFolder;

            return a != null && Equals(a);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion
    }
}