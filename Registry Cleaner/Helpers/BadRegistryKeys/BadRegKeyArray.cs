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
using System.Linq;

namespace Registry_Cleaner.Helpers.BadRegistryKeys
{
    public class BadRegKeyArray : CollectionBase
    {
        private static readonly object LockObj = new object();

        public BadRegistryKey this[int index]
        {
            get { return (BadRegistryKey)InnerList[index]; }
            set { InnerList[index] = value; }
        }

        public int Add(BadRegistryKey badRegKey)
        {
            if (badRegKey == null)
                throw new ArgumentNullException(nameof(badRegKey));

            int index;

            lock (LockObj)
            {
                index = InnerList.Add(badRegKey);
            }

            return index;
        }

        public int IndexOf(BadRegistryKey badRegKey)
        {
            int index;

            lock (LockObj)
            {
                index = InnerList.IndexOf(badRegKey);
            }

            return index;
        }

        public void Insert(int index, BadRegistryKey badRegKey)
        {
            if (badRegKey == null)
                throw new ArgumentNullException(nameof(badRegKey));

            lock (LockObj)
            {
                InnerList.Insert(index, badRegKey);
            }
        }

        public void Remove(BadRegistryKey badRegKey)
        {
            if (badRegKey == null)
                throw new ArgumentNullException(nameof(badRegKey));

            lock (LockObj)
            {
                InnerList.Remove(badRegKey);
            }
        }

        /// <summary>
        ///     Checks if an entry already exists with the same registry key
        /// </summary>
        /// <param name="regPath">Registry key</param>
        /// <param name="valueName">Value Name</param>
        /// <returns>True if it exists</returns>
        public bool Contains(string regPath, string valueName)
        {
            lock (LockObj)
            {
                foreach (BadRegistryKey brk in InnerList)
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
            }

            return false;
        }

        public int Problems(string sectionName)
        {
            var count = 0;

            lock (LockObj)
            {
                count +=
                    ((ArrayList)InnerList.Clone()).Cast<BadRegistryKey>()
                        .Count(badRegKey => badRegKey.SectionName == sectionName);
            }

            return count;
        }
    }
}