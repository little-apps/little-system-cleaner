using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class IncludeFolder : INotifyPropertyChanged, IEquatable<IncludeFolder>
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private readonly DirectoryInfo _dirInfo;
        private string _name;
        private bool? _bIsChecked = true;


        public bool? IsChecked
        {
            get { return _bIsChecked; }
            set 
            { 
                this._bIsChecked = value;
                this.OnPropertyChanged("IsChecked");
            }
        }

        public string Name
        {
            get { return this._name; }
            set 
            { 
                this._name = value;
                this.OnPropertyChanged("Name");
            }
        }

        public DirectoryInfo DirInfo
        {
            get { return this._dirInfo; }
        }

        public IncludeFolder(DirectoryInfo dirInfo)
        {
            this.Name = dirInfo.ToString();
            this._dirInfo = dirInfo;
        }

        public IncludeFolder(string folderPath)
        {
            this.Name = folderPath;
            this._dirInfo = new DirectoryInfo(folderPath);
        }

        public bool Equals(IncludeFolder other)
        {
            return (other != null && this.Name == other.Name);
        }
        public override bool Equals(object obj)
        {
            if (obj is IncludeFolder)
                return Equals(obj as IncludeFolder);
            else
                return false;
        }
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}
