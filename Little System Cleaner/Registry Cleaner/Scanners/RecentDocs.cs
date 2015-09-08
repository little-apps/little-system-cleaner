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
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Little_System_Cleaner.Misc;
using System.Threading;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public class RecentDocs : ScannerBase
    {
        public override string ScannerName => Strings.RecentDocs;

        internal static void Scan()
        {
            try
            {
                ScanMuiCache();
                ScanExplorerDocs();
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
        }

        /// <summary>
        /// Checks MUI Cache for invalid file references (XP Only)
        /// </summary>
        private static void ScanMuiCache()
        {
            try
            {
                using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\ShellNoRoam\MUICache"))
                {
                    if (regKey == null)
                        return;

                    foreach (string valueName in from valueName in regKey.GetValueNames() where !string.IsNullOrWhiteSpace(valueName) where !valueName.StartsWith("@") && valueName != "LangID" where !Utils.FileExists(valueName) && !Wizard.IsOnIgnoreList(valueName) select valueName)
                    {
                        Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.Name, valueName);
                    }
                }
            }
            catch (System.Security.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Recurses through the recent documents registry keys for invalid files
        /// </summary>
        private static void ScanExplorerDocs()
        {
            try
            {
                using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RecentDocs"))
                {
                    if (regKey == null)
                        return;

                    Wizard.Report.WriteLine("Cleaning invalid references in " + regKey.Name);

                    EnumMruList(regKey);

                    foreach (RegistryKey subKey in regKey.GetSubKeyNames().Select(strSubKey => regKey.OpenSubKey(strSubKey)).Where(subKey => subKey != null))
                    {
                        EnumMruList(subKey);
                    }
                }
            }
            catch (System.Security.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private static void EnumMruList(RegistryKey regKey)
        {
            foreach (string strValueName in regKey.GetValueNames())
            {
                string filePath = "", fileArgs;

                // Skip if value name is null/empty
                if (string.IsNullOrWhiteSpace(strValueName))
                    continue;

                // Ignore MRUListEx and others
                if (!Regex.IsMatch(strValueName, "[0-9]"))
                    continue;

                var value = regKey.GetValue(strValueName);

                string fileName = ExtractUnicodeStringFromBinary(value);
                string shortcutPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Recent)}\\{fileName}.lnk";

                // See if file exists in Recent Docs folder
                if (!string.IsNullOrEmpty(fileName))
                {
                    Wizard.StoreInvalidKey(Strings.InvalidRegKey, regKey.ToString(), strValueName);
                    continue;
                }

                if (!Utils.FileExists(shortcutPath) || !Utils.ResolveShortcut(shortcutPath, out filePath, out fileArgs))
                {
                    if (!Wizard.IsOnIgnoreList(shortcutPath) && !Wizard.IsOnIgnoreList(filePath))
                    {
                        Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(), strValueName);
                    }
                }
            }
        }

        /// <summary>
        /// Converts registry value to filename
        /// </summary>
        /// <param name="keyObj">Value from registry key</param>
        private static string ExtractUnicodeStringFromBinary(object keyObj)
        {
            string value = keyObj.ToString();    //get object value 
            string type = keyObj.GetType().Name;  //get object type

            if (type == "Byte[]")
            {
                value = "";
                byte[] bytes = (byte[])keyObj;
                //this seems crude but cannot find a way to 'cast' a Unicode string to byte[]
                //even in case where we know the beginning format is Unicode
                //so do it the hard way

                char[] chars = Encoding.Unicode.GetChars(bytes);
                foreach (char bt in chars)
                {
                    if (bt != 0)
                    {
                        value = value + bt; //construct string one at a time
                    }
                    else
                    {
                        break;  //apparently found 0,0 (end of string)
                    }
                }
            }
            return value;
        }
    }
}
