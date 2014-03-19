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

namespace Little_System_Cleaner
{
    [Serializable()]
    public class ExcludeItem : ICloneable
    {
        private string _pathRegistry = "";
        public string RegistryPath
        {
            get { return _pathRegistry; }
            set
            {
                if (Utils.RegKeyExists(value))
                    _pathRegistry = value;
            }
        }

        private string _pathFolder = "";
        public string FolderPath
        {
            get { return _pathFolder; }
            set
            {
                if (Directory.Exists(value))
                    _pathFolder = value;
            }
        }

        private string _pathFile = "";
        public string FilePath
        {
            get { return _pathFile; }
            set
            {
                if (File.Exists(value))
                    _pathFile = value;
            }
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

        public Object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    [Serializable()]
    public class ExcludeArray : ObservableCollection<ExcludeItem>
    {
        public ExcludeArray()
        {
        }

        public new bool Contains(ExcludeItem excludeItem)
        {
            foreach (ExcludeItem item in this.Items)
            {
                if (item.ToString() == excludeItem.ToString())
                    return true;
            }

            return false;
        }

        
    }
}
