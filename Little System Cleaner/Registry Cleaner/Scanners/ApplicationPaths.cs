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
using System.Diagnostics;
using System.Linq;
using System.Security;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using Microsoft.Win32;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public class ApplicationPaths : ScannerBase
    {
        public override string ScannerName => Strings.ApplicationPaths;

        /// <summary>
        ///     Verifies programs in App Paths
        /// </summary>
        public override void Scan()
        {
            try
            {
                Wizard.Report.WriteLine("Checking for invalid installer folders");
                ScanInstallFolders();

                Wizard.Report.WriteLine("Checking for invalid application paths");
                ScanAppPaths();
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static void ScanInstallFolders()
        {
            var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders");

            if (regKey == null)
                return;

            foreach (var strFolder in regKey.GetValueNames()
                .Where(strFolder => !string.IsNullOrWhiteSpace(strFolder))
                .Where(strFolder => !ScanFunctions.DirExists(strFolder) && !Wizard.IsOnIgnoreList(strFolder))
                .TakeWhile(strFolder => !CancellationToken.IsCancellationRequested))
            {
                Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.Name, strFolder);
            }
        }

        private static void ScanAppPaths()
        {
            var regKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths");

            if (regKey == null)
                return;

            foreach (
                var strSubKey in
                    regKey.GetSubKeyNames().TakeWhile(strSubKey => !CancellationToken.IsCancellationRequested))
            {
                var regKey2 = regKey.OpenSubKey(strSubKey);

                if (regKey2 == null)
                    continue;

                if (Convert.ToInt32(regKey2.GetValue("BlockOnTSNonInstallMode")) == 1)
                    continue;

                var strAppPath = regKey2.GetValue("") as string;
                var strAppDir = regKey2.GetValue("Path") as string;

                if (string.IsNullOrEmpty(strAppPath))
                {
                    Wizard.StoreInvalidKey(Strings.InvalidRegKey, regKey2.ToString());
                    continue;
                }

                if (!string.IsNullOrEmpty(strAppDir))
                {
                    if (Wizard.IsOnIgnoreList(strAppDir))
                        continue;
                    if (Utils.SearchPath(strAppPath, strAppDir))
                        continue;
                    if (Utils.SearchPath(strSubKey, strAppDir))
                        continue;
                }
                else
                {
                    if (ScanFunctions.FileExists(strAppPath) || Wizard.IsOnIgnoreList(strAppPath))
                        continue;
                }

                Wizard.StoreInvalidKey(Strings.InvalidFile, regKey2.Name);
            }

            regKey.Close();
        }
    }
}