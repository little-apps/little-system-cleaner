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
using Registry_Cleaner.Scanners;
using System.Collections;
using Registry_Cleaner.Properties;

namespace Registry_Cleaner.Helpers.Sections
{
    public class SectionModel : ITreeModel
    {
        public SectionModel()
        {
            Root = new Section();
        }

        public Section Root { get; }

        public IEnumerable GetChildren(object parent)
        {
            if (parent == null)
                parent = Root;
            return (parent as Section)?.Children;
        }

        public bool HasChildren(object parent)
        {
            var section = parent as Section;
            return section != null && section.Children.Count > 0;
        }

        internal static SectionModel CreateSectionModel()
        {
            var myComp = new Section { Icon = Resources.mycomputer, SectionName = "My Computer" };
            var model = new SectionModel();

            myComp.Children.Add(new Section
            {
                Icon = Resources.activexcom,
                SectionName = Strings.ActivexComObjects,
                Description = "Locations to ActiveX and COM objects that no longer exist",
                Parent = myComp
            });
            myComp.Children.Add(new Section
            {
                Icon = Resources.appinfo,
                SectionName = Strings.ApplicationInfo,
                Description = "Currently installed applications",
                Parent = myComp
            });
            myComp.Children.Add(new Section
            {
                Icon = Resources.programlocations,
                SectionName = Strings.ApplicationPaths,
                Description = "Removes invalid application paths",
                Parent = myComp
            });
            myComp.Children.Add(new Section
            {
                Icon = Resources.softwaresettings,
                SectionName = Strings.ApplicationSettings,
                Description = "Scans for software registry keys with no data",
                Parent = myComp
            });
            myComp.Children.Add(new Section
            {
                Icon = Resources.startup,
                SectionName = Strings.StartupFiles,
                Description = "Programs that run when windows starts up",
                Parent = myComp
            });
            myComp.Children.Add(new Section
            {
                Icon = Resources.drivers,
                SectionName = Strings.SystemDrivers,
                Description = "Finds invalid references to drivers",
                Parent = myComp
            });
            myComp.Children.Add(new Section
            {
                Icon = Resources.shareddlls,
                SectionName = Strings.SharedDLLs,
                Description = "Scans for invalid DLL references",
                Parent = myComp
            });
            myComp.Children.Add(new Section
            {
                Icon = Resources.helpfiles,
                SectionName = Strings.WindowsHelpFiles,
                Description = "Scans for help files that no longer exist",
                Parent = myComp
            });
            myComp.Children.Add(new Section
            {
                Icon = Resources.soundevents,
                SectionName = Strings.WindowsSounds,
                Description = "Scans for missing windows sounds",
                Parent = myComp
            });
            myComp.Children.Add(new Section
            {
                Icon = Resources.historylist,
                SectionName = Strings.RecentDocs,
                Description = "Scans for missing recent documents links",
                Parent = myComp
            });
            myComp.Children.Add(new Section
            {
                Icon = Resources.fonts,
                SectionName = Strings.WindowsFonts,
                Description = "Finds invalid font references",
                Parent = myComp
            });

            model.Root.Children.Add(myComp);

            return model;
        }
    }
}