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
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Win32;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class Multimedia : ScannerBase
    {
        public Multimedia(string[] fileList)
        {
            Name = "Multimedia";

            foreach (string filePath in fileList)
            {
                string name, desc;

                if (PluginIsValid(filePath, out name, out desc))
                    this.Children.Add(new Multimedia(this, name, desc, filePath));
            }
        }

        public Multimedia(ScannerBase parent, string header, string description, string pluginPath)
        {
            Parent = parent;
            Name = header;
            Description = description;
            PluginPath = pluginPath;
        }

        public override void Scan()
        {
            ScanPlugins();
        }
    }
}