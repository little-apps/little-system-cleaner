﻿/*
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
using System.ComponentModel;

namespace Registry_Cleaner.Helpers
{
    [Serializable]
    public class ExcludeItem : ICloneable, IEquatable<ExcludeItem>, INotifyPropertyChanged
    {
        private string _pathFile = "";
        private string _pathFolder = "";
        private string _pathRegistry = "";

        public string RegistryPath
        {
            get { return _pathRegistry; }
            set
            {
                _pathRegistry = value;

                OnPropertyChanged(nameof(Item));
            }
        }

        public string FolderPath
        {
            get { return _pathFolder; }
            set
            {
                _pathFolder = value;

                OnPropertyChanged(nameof(Item));
            }
        }

        public string FilePath
        {
            get { return _pathFile; }
            set
            {
                _pathFile = value;

                OnPropertyChanged(nameof(Item));
            }
        }

        public bool IsPath => !string.IsNullOrEmpty(_pathFile) || !string.IsNullOrEmpty(_pathFolder);

        public string Item => ToString();

        #region ICloneable Members

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion ICloneable Members

        /// <summary>
        ///     Returns the assigned path (registry/file/folder)
        /// </summary>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(_pathRegistry))
                return string.Copy(_pathRegistry);

            if (!string.IsNullOrEmpty(_pathFile))
                return string.Copy(_pathFile);

            if (!string.IsNullOrEmpty(_pathFolder))
                return string.Copy(_pathFolder);

            return GetType().Name;
        }

        #region IEquatable Members

        public bool Equals(ExcludeItem other)
        {
            return other != null && ToString() == other.ToString();
        }

        public bool Equals(string other)
        {
            return !string.IsNullOrEmpty(other) && FolderPath == other;
        }

        public override bool Equals(object obj)
        {
            var a = obj as ExcludeItem;
            if (a != null)
                return Equals(a);

            var s = obj as string;
            return s != null && Equals(s);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        #endregion IEquatable Members

        #region INotifyPropertyChanged Members

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion INotifyPropertyChanged Members
    }
}