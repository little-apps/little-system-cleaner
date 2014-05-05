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
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    public class BadRegKeyArray : CollectionBase
    {
        public BadRegistryKey this[int index]
        {
            get { return (BadRegistryKey)this.InnerList[index]; }
            set { this.InnerList[index] = value; }
        }

        public int Add(BadRegistryKey BadRegKey)
        {
            if (BadRegKey == null)
                throw new ArgumentNullException("BadRegKey");

            return (this.InnerList.Add(BadRegKey));
        }

        public int IndexOf(BadRegistryKey BadRegKey)
        {
            return (this.InnerList.IndexOf(BadRegKey));
        }

        public void Insert(int index, BadRegistryKey BadRegKey)
        {
            if (BadRegKey == null)
                throw new ArgumentNullException("BadRegKey");

            this.InnerList.Insert(index, BadRegKey);
        }

        public void Remove(BadRegistryKey BadRegKey)
        {
            if (BadRegKey == null)
                throw new ArgumentNullException("BadRegKey");

            this.InnerList.Remove(BadRegKey);
        }

        /// <summary>
        /// Checks if an entry already exists with the same registry key
        /// </summary>
        /// <param name="regPath">Registry key</param>
        /// <param name="valueName">Value Name</param>
        /// <returns>True if it exists</returns>
        public bool Contains(string regPath, string valueName)
        {
            foreach (BadRegistryKey brk in this.InnerList)
            {
                if (string.IsNullOrEmpty(valueName))
                {
                    if (brk.RegKeyPath == regPath)
                        return true;
                }
                else
                {
                    if (brk.RegKeyPath == regPath && brk.ValueName == valueName)
                        return true;
                }
            }

            return false;
        }

        public int Problems(string sectionName)
        {
            int count = 0;

            foreach (BadRegistryKey badRegKey in (ArrayList)this.InnerList.Clone())
            {
                if (badRegKey.SectionName == sectionName)
                    count++;
            }

            return count;
        }
    }
}
