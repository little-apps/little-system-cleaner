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

using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Privacy_Cleaner.Scanners;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers
{
   
    public class SectionModel : ITreeModel
    {
        public ObservableCollection<ScannerBase> RootChildren { get; private set; }

        internal static SectionModel CreateSectionModel()
        {
            SectionModel sectionModel = new SectionModel();

            if (InternetExplorer.IsInstalled())
            {
                sectionModel.RootChildren.Add(new InternetExplorer());
            }

            if (Firefox.IsInstalled())
            {
                sectionModel.RootChildren.Add(new Firefox());
            }

            // Check for chrome.exe in install directory
            if (gChrome.IsInstalled())
            {
                sectionModel.RootChildren.Add(new gChrome());
            }

            // Misc scanners
            sectionModel.RootChildren.Add(new Little_System_Cleaner.Privacy_Cleaner.Scanners.Misc());

            // If plugins exist -> Recurse through the plugins directory
            string pluginDir = string.Format(@"{0}\Privacy Cleaner Plugins", System.Windows.Forms.Application.StartupPath);
            if (Directory.Exists(pluginDir))
            {
                sectionModel.RootChildren.Add(new Plugins(Directory.GetFiles(pluginDir, "*.xml")));
            }

            return sectionModel;
        }

        public SectionModel()
        {
            RootChildren = new ObservableCollection<ScannerBase>();
        }

        public System.Collections.IEnumerable GetChildren(object parent)
        {
            if (parent == null)
                return RootChildren;
            else
                return (parent as ScannerBase).Children;
        }

        public bool HasChildren(object parent)
        {
            return (parent as ScannerBase).Children.Count > 0;
        }
    }
}
