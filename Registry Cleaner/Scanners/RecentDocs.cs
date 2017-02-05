﻿/*
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
using System.Text;
using System.Text.RegularExpressions;
using Shared;

namespace Registry_Cleaner.Scanners
{
    public class RecentDocs : ScannerBase
    {
        public override string ScannerName => Strings.RecentDocs;

        public override void Scan()
        {
            ScanMuiCache();
            ScanExplorerDocs();
        }

        /// <summary>
        ///     Checks MUI Cache for invalid file references (XP Only)
        /// </summary>
        private static void ScanMuiCache()
        {
            try
            {
                using (var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\ShellNoRoam\MUICache"))
                {
                    if (regKey == null)
                        return;

                    foreach (var valueName in regKey.GetValueNames()
                        .Where(valueName => !string.IsNullOrWhiteSpace(valueName))
                        .Where(valueName => !valueName.StartsWith("@") && valueName != "LangID")
                        .Where(valueName => !ScanFunctions.FileExists(valueName) && !Wizard.IsOnIgnoreList(valueName))
                        .TakeWhile(valueName => !ScanFunctions.FileExists(valueName) && !Wizard.IsOnIgnoreList(valueName)))
                    {
                        Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.Name, valueName);
                    }
                }
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        ///     Recurses through the recent documents registry keys for invalid files
        /// </summary>
        private static void ScanExplorerDocs()
        {
            try
            {
                using (
                    var regKey =
                        Registry.CurrentUser.OpenSubKey(
                            "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RecentDocs"))
                {
                    if (regKey == null)
                        return;

                    Wizard.Report.WriteLine("Cleaning invalid references in " + regKey.Name);

                    EnumMruList(regKey);

                    foreach (var subKey in regKey.GetSubKeyNames()
                        .Select(subKey => regKey.OpenSubKey(subKey))
                        .Where(subKey => subKey != null)
                        .TakeWhile(subKey => !CancellationToken.IsCancellationRequested))
                    {
                        EnumMruList(subKey);
                    }
                }
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static void EnumMruList(RegistryKey regKey)
        {
            foreach (var valueName in regKey.GetValueNames())
            {
                var filePath = "";
                string fileArgs;

                // Skip if value name is null/empty
                if (string.IsNullOrWhiteSpace(valueName))
                    continue;

                // Ignore MRUListEx and others
                if (!Regex.IsMatch(valueName, "[0-9]"))
                    continue;

                var value = regKey.GetValue(valueName);

                var fileName = ExtractUnicodeStringFromBinary(value);
                string shortcutPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Recent)}\\{fileName}.lnk";

                // See if file exists in Recent Docs folder
                if (!string.IsNullOrEmpty(fileName))
                {
                    Wizard.StoreInvalidKey(Strings.InvalidRegKey, regKey.ToString(), valueName);
                    continue;
                }

                if (ScanFunctions.FileExists(shortcutPath) && Utils.ResolveShortcut(shortcutPath, out filePath, out fileArgs))
                    continue;

                if (!Wizard.IsOnIgnoreList(shortcutPath) && !Wizard.IsOnIgnoreList(filePath))
                {
                    Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(), valueName);
                }
            }
        }

        /// <summary>
        ///     Converts registry value to filename
        /// </summary>
        /// <param name="keyObj">Value from registry key</param>
        private static string ExtractUnicodeStringFromBinary(object keyObj)
        {
            var value = keyObj.ToString(); //get object value
            var type = keyObj.GetType().Name; //get object type

            if (type != "Byte[]")
                return value;

            value = "";
            var bytes = (byte[])keyObj;
            //this seems crude but cannot find a way to 'cast' a Unicode string to byte[]
            //even in case where we know the beginning format is Unicode
            //so do it the hard way

            return Encoding.Unicode.GetChars(bytes).TakeWhile(bt => bt != 0).Aggregate(value, (s, c) => s + c);
        }
    }
}