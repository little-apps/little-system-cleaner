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
using Microsoft.Win32;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public class ApplicationSettings : ScannerBase
    {
        public override string ScannerName => Strings.ApplicationSettings;

        public override void Scan()
        {
            try
            {
                ScanRegistryKey(Registry.LocalMachine.OpenSubKey("SOFTWARE"));
                ScanRegistryKey(Registry.CurrentUser.OpenSubKey("SOFTWARE"));

                if (Utils.Is64BitOs)
                {
                    ScanRegistryKey(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node"));
                    ScanRegistryKey(Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Wow6432Node"));
                }
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static void ScanRegistryKey(RegistryKey baseRegKey)
        {
            if (baseRegKey == null)
                return;

            Wizard.Report.WriteLine("Scanning " + baseRegKey.Name + " for empty registry keys");

            foreach (var subKey in baseRegKey.GetSubKeyNames()
                .Where(subKey => IsEmptyRegistryKey(baseRegKey.OpenSubKey(subKey, true)))
                .TakeWhile(subKey => !CancellationToken.IsCancellationRequested))
            {
                Wizard.StoreInvalidKey(Strings.NoRegKey, baseRegKey.Name + "\\" + subKey);
            }

            baseRegKey.Close();
        }

        /// <summary>
        ///     Recursively goes through the registry keys and finds how many values there are
        /// </summary>
        /// <param name="regKey">The base registry key</param>
        /// <returns>True if the registry key is empty</returns>
        private static bool IsEmptyRegistryKey(RegistryKey regKey)
        {
            if (regKey == null)
                return false;

            var valueCount = regKey.ValueCount;
            var subKeyCount = regKey.SubKeyCount;

            if (regKey.ValueCount != 0)
                return valueCount == 0 && subKeyCount == 0;

            if (regKey.GetValue("") != null)
                valueCount = 1;

            return valueCount == 0 && subKeyCount == 0;
        }
    }
}