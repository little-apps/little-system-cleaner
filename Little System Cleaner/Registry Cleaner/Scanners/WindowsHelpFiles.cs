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

using System.IO;
using System.Linq;
using Microsoft.Win32;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Little_System_Cleaner.Misc;
using System.Threading;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public class WindowsHelpFiles : ScannerBase
    {
        public override string ScannerName => Strings.WindowsHelpFiles;

        public override void Scan()
        {
            try
            {
                CheckHelpFiles(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\HTML Help"));
                CheckHelpFiles(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\Help"));
            }
            catch (System.Security.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Scans for invalid windows help files
        /// </summary>
        private static void CheckHelpFiles(RegistryKey regKey)
        {
            if (regKey == null)
                return;

            Wizard.Report.WriteLine("Checking for missing help files in " + regKey.Name);

            foreach (var helpFile in 
                regKey.GetValueNames()
                    .Select(helpFile => new {Name = helpFile, Value = regKey.GetValue(helpFile) as string})
                    .Where(o => !HelpFileExists(o.Name, o.Value))
                    .Select(o => o.Name)
                    .TakeWhile(helpFile => !CancellationToken.IsCancellationRequested)
                )
            {
                // (Won't include default value name as strHelpFile must not be null/empty)
                Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(), (string.IsNullOrWhiteSpace(helpFile) ? "(default)" : helpFile));
            }
        }

        /// <summary>
        /// Sees if the help file exists
        /// </summary>
        /// <param name="helpFile">Should contain the filename</param>
        /// <param name="helpPath">Should be the path to file</param>
        /// <returns>True if it exists</returns>
        private static bool HelpFileExists(string helpFile, string helpPath)
        {
            if (string.IsNullOrEmpty(helpFile) || string.IsNullOrEmpty(helpPath))
                return true;

            if (Utils.FileExists(helpPath) || Wizard.IsOnIgnoreList(helpPath))
                return true;

            if (Utils.FileExists(helpFile) || Wizard.IsOnIgnoreList(helpFile))
                return true;

            string combinedPath = Path.Combine(helpPath, helpFile);

            if (Utils.FileExists(combinedPath) || Wizard.IsOnIgnoreList(combinedPath))
                return true;

            return false;
        }
    }
}
