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
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Forms;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers
{
    public class SectionModel : ITreeModel
    {
        public SectionModel()
        {
            RootChildren = new ObservableCollection<ScannerBase>();
        }

        public ObservableCollection<ScannerBase> RootChildren { get; }

        public IEnumerable GetChildren(object parent)
        {
            return parent == null ? RootChildren : (parent as ScannerBase)?.Children;
        }

        public bool HasChildren(object parent)
        {
            var scannerBase = parent as ScannerBase;
            return scannerBase != null && scannerBase.Children.Count > 0;
        }

        internal static SectionModel CreateSectionModel()
        {
            var sectionModel = new SectionModel();

            if (InternetExplorer.IsInstalled())
            {
                sectionModel.RootChildren.Add(new InternetExplorer());
            }

            if (Firefox.IsInstalled())
            {
                sectionModel.RootChildren.Add(new Firefox());
            }

            // Check for chrome.exe in install directory
            if (GChrome.IsInstalled())
            {
                sectionModel.RootChildren.Add(new GChrome());
            }

            // Misc scanners
            sectionModel.RootChildren.Add(new Scanners.Misc());

            // If plugins exist -> Recurse through the plugins directory
            string pluginDir = $@"{Application.StartupPath}\Privacy Cleaner Plugins";
            if (Directory.Exists(pluginDir))
            {
                sectionModel.RootChildren.Add(new Plugins(Directory.GetFiles(pluginDir, "*.xml")));
            }

            return sectionModel;
        }
    }
}