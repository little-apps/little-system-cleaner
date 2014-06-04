using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class IncludeDrive : INotifyPropertyChanged
    {        
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private string _name;
        private bool? _bIsChecked = true;


        public bool? IsChecked
        {
            get { return _bIsChecked; }
            set { this._bIsChecked = value; }
        }

        public string Name
        {
            get { return this._name; }
            set { this._name = value; }
        }

        public IncludeDrive(DriveInfo drive)
        {
            this._name = drive.ToString();
        }

        public bool Equals(IncludeDrive other)
        {
            return (other != null && this.Name == other.Name);
        }
        public override bool Equals(object obj)
        {
            if (obj is IncludeDrive)
                return Equals(obj as IncludeDrive);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}
