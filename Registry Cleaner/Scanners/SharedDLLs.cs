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
using System.Diagnostics;
using System.Linq;
using System.Security;

namespace Registry_Cleaner.Scanners
{
    public class SharedDLLs : ScannerBase
    {
        public override string ScannerName => Strings.SharedDLLs;

        /// <summary>
        ///     Scan for missing links to DLLS
        /// </summary>
        public override void Scan()
        {
            try
            {
                var regKey = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\SharedDLLs");

                if (regKey == null)
                    return;

                Wizard.Report.WriteLine("Scanning for missing shared DLLs");

                // Validate Each DLL from the value names
                foreach (var filePath in regKey.GetValueNames()
                    .Where(filePath => !string.IsNullOrWhiteSpace(filePath))
                    .Where(filePath => !ScanFunctions.FileExists(filePath) && !Wizard.IsOnIgnoreList(filePath))
                    .TakeWhile(filePath => !CancellationToken.IsCancellationRequested))
                {
                    Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.Name, filePath);
                }

                regKey.Close();
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}