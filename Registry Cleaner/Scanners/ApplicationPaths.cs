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

using Registry_Cleaner.Controls;
using Registry_Cleaner.Helpers;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security;
using Shared;

namespace Registry_Cleaner.Scanners
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

            foreach (var folder in regKey.GetValueNames()
                .Where(folder => !string.IsNullOrWhiteSpace(folder))
                .Where(folder => !ScanFunctions.DirExists(folder) && !Wizard.IsOnIgnoreList(folder))
                .TakeWhile(folder => !CancellationToken.IsCancellationRequested))
            {
                Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.Name, folder);
            }
        }

        private static void ScanAppPaths()
        {
            var regKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths");

            if (regKey == null)
                return;

            foreach (
                var subKey in
                    regKey.GetSubKeyNames().TakeWhile(subKey => !CancellationToken.IsCancellationRequested))
            {
                var regKey2 = regKey.OpenSubKey(subKey);

                if (regKey2 == null)
                    continue;

                if (Convert.ToInt32(regKey2.GetValue("BlockOnTSNonInstallMode")) == 1)
                    continue;

                var appPath = regKey2.GetValue("") as string;
                var appDir = regKey2.GetValue("Path") as string;

                if (string.IsNullOrEmpty(appPath))
                {
                    Wizard.StoreInvalidKey(Strings.InvalidRegKey, regKey2.ToString());
                    continue;
                }

                if (!string.IsNullOrEmpty(appDir))
                {
                    if (Wizard.IsOnIgnoreList(appDir))
                        continue;
                    if (Utils.SearchPath(appPath, appDir))
                        continue;
                    if (Utils.SearchPath(subKey, appDir))
                        continue;
                }
                else
                {
                    if (ScanFunctions.FileExists(appPath) || Wizard.IsOnIgnoreList(appPath))
                        continue;
                }

                Wizard.StoreInvalidKey(Strings.InvalidFile, regKey2.Name);
            }

            regKey.Close();
        }
    }
}