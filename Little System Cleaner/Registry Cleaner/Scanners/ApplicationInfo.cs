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
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Little_System_Cleaner.Uninstall_Manager.Helpers;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Registry_Cleaner.Helpers;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public class ApplicationInfo : ScannerBase
    {
        public override string ScannerName
        {
            get { return Strings.ApplicationInfo; }
        }

        /// <summary>
        /// Verifies installed programs in add/remove list
        /// </summary>
        public static void Scan()
        {
            try
            {
                ScanWizard.Report.WriteLine("Verifying programs in Add/Remove list");

                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"))
                {
                    if (regKey == null)
                        return;

                    foreach (string strProgName in regKey.GetSubKeyNames())
                    {
                        using (RegistryKey regKey2 = regKey.OpenSubKey(strProgName))
                        {
                            if (regKey2 != null)
                            {
                                ProgramInfo progInfo = new ProgramInfo(regKey2);

                                if (regKey2.ValueCount <= 0)
                                {
                                    ScanWizard.StoreInvalidKey(Strings.InvalidRegKey, regKey2.ToString());
                                    continue;
                                }

                                if (progInfo.WindowsInstaller)
                                    continue;

                                if (string.IsNullOrEmpty(progInfo._displayName) && (!progInfo.Uninstallable))
                                {
                                    ScanWizard.StoreInvalidKey(Strings.InvalidRegKey, regKey2.ToString());
                                    continue;
                                }

                                // Check display icon
                                if (!string.IsNullOrEmpty(progInfo._displayIcon))
                                    if (!ScanFunctions.IconExists(progInfo._displayIcon))
                                        ScanWizard.StoreInvalidKey(Strings.InvalidFile, regKey2.ToString(), "DisplayIcon");

                                // Check install location 
                                if (!string.IsNullOrEmpty(progInfo._installLocation))
                                    if ((!ScanFunctions.DirExists(progInfo._installLocation)) && (!Utils.FileExists(progInfo._installLocation)))
                                        if (!ScanWizard.IsOnIgnoreList(progInfo._installLocation))
                                            ScanWizard.StoreInvalidKey(Strings.InvalidFile, regKey2.ToString(), "InstallLocation");

                                // Check install source 
                                if (!string.IsNullOrEmpty(progInfo._installSource))
                                    if ((!ScanFunctions.DirExists(progInfo._installSource)) && (!Utils.FileExists(progInfo._installSource)))
                                        if (!ScanWizard.IsOnIgnoreList(progInfo._installSource))
                                            ScanWizard.StoreInvalidKey(Strings.InvalidFile, regKey2.ToString(), "InstallSource");

                                // Check ARP Cache
                                if (progInfo.SlowCache)
                                {
                                    if (!string.IsNullOrEmpty(progInfo.FileName))
                                        if (!Utils.FileExists(progInfo.FileName) && !ScanWizard.IsOnIgnoreList(progInfo.FileName))
                                            ScanWizard.StoreInvalidKey(Strings.InvalidRegKey, progInfo.SlowInfoCacheRegKey);
                                }
                            }
                        }
                    }
                }

                ScanWizard.Report.WriteLine("Verifying registry entries in Add/Remove Cache");

                checkARPCache(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Management\ARPCache\"));
                checkARPCache(Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Management\ARPCache\"));
            }
            catch (System.Security.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Do a cross-reference check on ARP Cache keys
        /// </summary>
        /// <param name="regKey"></param>
        private static void checkARPCache(RegistryKey regKey)
        {
            if (regKey == null)
                return;

            foreach (string subKey in regKey.GetSubKeyNames())
            {
                if (Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + subKey) == null)
                    ScanWizard.StoreInvalidKey(Strings.ObsoleteRegKey, string.Format("{0}/{1}", regKey.Name, subKey));
            }
        }
    }
}
