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
    public class SystemDrivers : ScannerBase
    {
        public override string ScannerName => Strings.SystemDrivers;

        /// <summary>
        /// Scans for invalid references to drivers
        /// </summary>
        public override void Scan()
        {
            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Drivers32"))
                {
                    if (regKey == null)
                        return;

                    Wizard.Report.WriteLine("Scanning for missing drivers");

                    foreach (
                        var driverName in
                            regKey.GetValueNames()
                                .Select(driverName => new { Name = driverName, Value = regKey.GetValue(driverName) as string })
                                .Where(o => !string.IsNullOrEmpty(o.Value))
                                .Where(o => !Utils.FileExists(o.Value) && !Wizard.IsOnIgnoreList(o.Value))
                                .Select(o => o.Name))
                    {
                        Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.Name, (string.IsNullOrWhiteSpace(driverName) ? "(default)" : driverName));
                    }
                }
            }
            catch (System.Security.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
        }
    }
}
