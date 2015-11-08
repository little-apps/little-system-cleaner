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

using System.Diagnostics;
using System.Linq;
using System.Security;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using Little_System_Cleaner.Uninstall_Manager.Helpers;
using Microsoft.Win32;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public class ApplicationInfo : ScannerBase
    {
        public override string ScannerName => Strings.ApplicationInfo;

        /// <summary>
        ///     Verifies installed programs in add/remove list
        /// </summary>
        public override void Scan()
        {
            try
            {
                Wizard.Report.WriteLine("Verifying programs in Add/Remove list");

                using (
                    var regKey =
                        Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"))
                {
                    if (regKey == null)
                        return;

                    foreach (
                        var strProgName in
                            regKey.GetSubKeyNames().TakeWhile(strProgName => !CancellationToken.IsCancellationRequested)
                        )
                    {
                        using (var regKey2 = regKey.OpenSubKey(strProgName))
                        {
                            if (regKey2 == null)
                                continue;

                            var progInfo = new ProgramInfo(regKey2);

                            if (regKey2.ValueCount <= 0)
                            {
                                Wizard.StoreInvalidKey(Strings.InvalidRegKey, regKey2.ToString());
                                continue;
                            }

                            if (progInfo.WindowsInstaller)
                                continue;

                            if (string.IsNullOrEmpty(progInfo.DisplayName) && !progInfo.Uninstallable)
                            {
                                Wizard.StoreInvalidKey(Strings.InvalidRegKey, regKey2.ToString());
                                continue;
                            }

                            // Check display icon
                            if (!string.IsNullOrEmpty(progInfo.DisplayIcon))
                                if (!ScanFunctions.IconExists(progInfo.DisplayIcon))
                                    Wizard.StoreInvalidKey(Strings.InvalidFile, regKey2.ToString(), "DisplayIcon");

                            // Check install location 
                            if (!string.IsNullOrEmpty(progInfo.InstallLocation))
                                if (!ScanFunctions.DirExists(progInfo.InstallLocation) &&
                                    !Utils.FileExists(progInfo.InstallLocation))
                                    if (!Wizard.IsOnIgnoreList(progInfo.InstallLocation))
                                        Wizard.StoreInvalidKey(Strings.InvalidFile, regKey2.ToString(),
                                            "InstallLocation");

                            // Check install source 
                            if (!string.IsNullOrEmpty(progInfo.InstallSource))
                                if (!ScanFunctions.DirExists(progInfo.InstallSource) &&
                                    !Utils.FileExists(progInfo.InstallSource))
                                    if (!Wizard.IsOnIgnoreList(progInfo.InstallSource))
                                        Wizard.StoreInvalidKey(Strings.InvalidFile, regKey2.ToString(), "InstallSource");

                            // Check ARP Cache
                            if (!progInfo.SlowCache)
                                continue;

                            if (string.IsNullOrEmpty(progInfo.FileName))
                                continue;

                            if (!Utils.FileExists(progInfo.FileName) && !Wizard.IsOnIgnoreList(progInfo.FileName))
                                Wizard.StoreInvalidKey(Strings.InvalidRegKey, progInfo.SlowInfoCacheRegKey);
                        }
                    }
                }

                Wizard.Report.WriteLine("Verifying registry entries in Add/Remove Cache");

                CheckArpCache(
                    Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Management\ARPCache\"));
                CheckArpCache(
                    Registry.CurrentUser.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Management\ARPCache\"));
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        ///     Do a cross-reference check on ARP Cache keys
        /// </summary>
        /// <param name="regKey"></param>
        private static void CheckArpCache(RegistryKey regKey)
        {
            if (regKey == null)
                return;

            foreach (var subKey in regKey.GetSubKeyNames()
                .Where(
                    subKey =>
                        Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" +
                                                         subKey) == null)
                .TakeWhile(subKey => !CancellationToken.IsCancellationRequested))
            {
                Wizard.StoreInvalidKey(Strings.ObsoleteRegKey, $"{regKey.Name}/{subKey}");
            }
        }
    }
}