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

using System.Linq;
using Microsoft.Win32;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Little_System_Cleaner.Misc;
using System.Threading;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public class WindowsSounds : ScannerBase
    {
        public override string ScannerName => Strings.WindowsSounds;

        public override void Scan()
        {
            try
            {
                using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey("AppEvents\\Schemes\\Apps"))
                {
                    if (regKey == null)
                        return;

                    Wizard.Report.WriteLine("Scanning for missing sound events");
                    ParseSoundKeys(regKey);
                }
            }
            catch (System.Security.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Goes deep into sub keys to see if files exist
        /// </summary>
        /// <param name="rk">Registry subkey</param>
        private static void ParseSoundKeys(RegistryKey rk)
        {
            foreach (string strSubKey in rk.GetSubKeyNames().TakeWhile(strSubKey => !CancellationToken.IsCancellationRequested))
            {

                // Ignores ".Default" Subkey
                if ((strSubKey.CompareTo(".Current") == 0) || (strSubKey.CompareTo(".Modified") == 0))
                {
                    // Gets the (default) key and sees if the file exists
                    RegistryKey rk2 = rk.OpenSubKey(strSubKey);

                    string strSoundPath = rk2?.GetValue("") as string;

                    if (string.IsNullOrEmpty(strSoundPath))
                        continue;

                    if (!Utils.FileExists(strSoundPath) && !Wizard.IsOnIgnoreList(strSoundPath))
                        Wizard.StoreInvalidKey(Strings.InvalidFile, rk2.Name, "(default)");
                }
                else if (!string.IsNullOrEmpty(strSubKey))
                {
                    RegistryKey rk2 = rk.OpenSubKey(strSubKey);
                    if (rk2 != null)
                    {
                        ParseSoundKeys(rk2);
                    }
                }

            }
        }
    }
}
