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
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Windows.Media.Imaging;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public abstract class ScannerBase
    {
        /// <summary>
        /// Returns the scanner name
        /// </summary>
        abstract public string ScannerName
        {
            get;
        }

        /// <summary>
        /// Gets/Sets the icon for the section
        /// </summary>
        public BitmapImage bMapImg { get; set; } 

        /// <summary>
        /// Gets/Sets if the scanner is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// The root node (used for scan dialog)
        /// </summary>
        public BadRegistryKey RootNode = new BadRegistryKey(null, "");

        /// <summary>
        /// Returns the scanner name
        /// </summary>
        public override string ToString()
        {
            return (string)ScannerName.Clone();
        }

        //public virtual void Scan()
        //{
        //    return;
        //}
 
    }
}
