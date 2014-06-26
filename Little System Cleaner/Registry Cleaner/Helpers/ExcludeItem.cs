/*
    Little System Cleaner
    Copyright (C) 2008 Little Apps (http://www.little-apps.com/)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections.ObjectModel;
using Little_System_Cleaner.Misc;
using System.ComponentModel;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    [Serializable]
    public class ExcludeItem : ICloneable, IEquatable<ExcludeItem>, INotifyPropertyChanged
    {
        private string _pathRegistry = "";
        private string _pathFolder = "";
        private string _pathFile = "";

        public string RegistryPath
        {
            get { return _pathRegistry; }
            set
            {
                this._pathRegistry = value;

                this.OnPropertyChanged("Item");
            }
        }

        public string FolderPath
        {
            get { return _pathFolder; }
            set
            {
                this._pathFolder = value;

                this.OnPropertyChanged("Item");
            }
        }

        public string FilePath
        {
            get { return _pathFile; }
            set
            {
                this._pathFile = value;

                this.OnPropertyChanged("Item");
            }
        }

        public bool IsPath
        {
            get { return (!string.IsNullOrEmpty(this._pathFile) || !string.IsNullOrEmpty(this._pathFolder)); }
        }

        public string Item
        {
            get { return ToString(); }
        }

        /// <summary>
        /// Returns the assigned path (registry/file/folder)
        /// </summary>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(_pathRegistry))
                return string.Copy(_pathRegistry);

            if (!string.IsNullOrEmpty(_pathFile))
                return string.Copy(_pathFile);

            if (!string.IsNullOrEmpty(_pathFolder))
                return string.Copy(_pathFolder);

            return this.GetType().Name;
        }

        /// <summary>
        /// The constructor for this class
        /// </summary>
        public ExcludeItem()
        {
        }

        #region ICloneable Members
        public Object Clone()
        {
            return this.MemberwiseClone();
        }
        #endregion

        #region IEquatable Members
        public bool Equals(ExcludeItem other)
        {
            return (other != null && this.ToString() == other.ToString());
        }

        public bool Equals(string other)
        {
            return (!string.IsNullOrEmpty(other) && this.FolderPath == other);
        }

        public override bool Equals(object obj)
        {
            if (obj is ExcludeItem)
                return Equals(obj as ExcludeItem);
            else if (obj is string)
                return Equals(obj as string);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
        #endregion

        #region INotifyPropertyChanged Members
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }
}
