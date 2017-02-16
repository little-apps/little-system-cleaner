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

using Microsoft.Win32;
using Registry_Cleaner.Controls;
using Registry_Cleaner.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Registry_Cleaner.Scanners
{
    public class WindowsFonts : ScannerBase
    {
        private const int CSIDL_FONTS = 0x0014; // windows\fonts
        public override string ScannerName => Strings.WindowsFonts;

        [DllImport("shell32.dll")]
        internal static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder strPath, int nFolder,
            bool fCreate);

        /// <summary>
        ///     Finds invalid font references
        /// </summary>
        public override void Scan()
        {
            var path = new StringBuilder(260);

            try
            {
                using (
                    var regKey =
                        Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts"))
                {
                    if (regKey == null)
                        return;

                    Wizard.Report.WriteLine("Scanning for invalid fonts");

                    if (!SHGetSpecialFolderPath(IntPtr.Zero, path, CSIDL_FONTS, false))
                        return;

                    foreach (var fontName in
                        regKey.GetValueNames()
                            .Select(valueName => new { Name = valueName, Value = regKey.GetValue(valueName) as string })
                            // Skip if value is empty
                            .Where(o => !string.IsNullOrEmpty(o.Value))
                            // Check value by itself
                            .Where(o => !ScanFunctions.FileExists(o.Value))
                            .Where(o => !Wizard.IsOnIgnoreList(o.Value))
                            .Select(o => new { o.Name, o.Value, Path = $"{path.ToString()}\\{o.Value}" })
                            // Check for font in fonts folder
                            .Where(o => !File.Exists(o.Path) && !Wizard.IsOnIgnoreList(o.Path))
                            .Select(o => o.Name)
                            .TakeWhile(fontName => !CancellationToken.IsCancellationRequested)
                        )
                    {
                        Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(),
                            string.IsNullOrWhiteSpace(fontName) ? "(default)" : fontName);
                    }
                }
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}