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
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using Microsoft.Win32;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public class WindowsSounds : ScannerBase
    {
        public override string ScannerName => Strings.WindowsSounds;

        public override void Scan()
        {
            try
            {
                using (var regKey = Registry.CurrentUser.OpenSubKey("AppEvents\\Schemes\\Apps"))
                {
                    if (regKey == null)
                        return;

                    Wizard.Report.WriteLine("Scanning for missing sound events");
                    ParseSoundKeys(regKey);
                }
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        ///     Goes deep into sub keys to see if files exist
        /// </summary>
        /// <param name="rk">Registry subkey</param>
        private static void ParseSoundKeys(RegistryKey rk)
        {
            foreach (
                var subKey in rk.GetSubKeyNames().TakeWhile(subKey => !CancellationToken.IsCancellationRequested))
            {
                // Ignores ".Default" Subkey
                if ((string.Compare(subKey, ".Current", StringComparison.Ordinal) == 0) ||
                    (string.Compare(subKey, ".Modified", StringComparison.Ordinal) == 0))
                {
                    // Gets the (default) key and sees if the file exists
                    var rk2 = rk.OpenSubKey(subKey);

                    var soundPath = rk2?.GetValue("") as string;

                    if (string.IsNullOrEmpty(soundPath))
                        continue;

                    if (!ScanFunctions.FileExists(soundPath) && !Wizard.IsOnIgnoreList(soundPath))
                        Wizard.StoreInvalidKey(Strings.InvalidFile, rk2.Name, "(default)");
                }
                else if (!string.IsNullOrEmpty(subKey))
                {
                    var rk2 = rk.OpenSubKey(subKey);
                    if (rk2 != null)
                    {
                        ParseSoundKeys(rk2);
                    }
                }
            }
        }
    }
}