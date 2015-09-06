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
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Little_System_Cleaner.Privacy_Cleaner.Controls;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Helpers.Results;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class Misc : ScannerBase
    {
        #region Constants
        // No dialog box confirming the deletion of the objects will be displayed.
        internal const int SHERB_NOCONFIRMATION = 0x00000001;
        // No dialog box indicating the progress will be displayed.
        internal const int SHERB_NOPROGRESSUI = 0x00000002;
        // No sound will be played when the operation is complete.
        internal const int SHERB_NOSOUND = 0x00000004;
        #endregion

        #region Structures
        [StructLayout(LayoutKind.Sequential)]
        internal struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }
        #endregion

        #region Functions
        [DllImport("shell32.dll", SetLastError = true)]
        internal static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);
        [DllImport("shell32.dll", SetLastError = true)]
        internal static extern int SHEmptyRecycleBin(IntPtr hWnd, string pszRootPath, uint dwFlags);
        #endregion

        public Misc() 
        {
            Name = "Miscellaneous";

            this.Children.Add(new Misc(this, "Recycle Bin"));
            this.Children.Add(new Misc(this, "Desktop and Start Menu Icons"));
        }

        public Misc(ScannerBase parent, string header)
        {
            Parent = parent;
            Name = header;
        }

        public override void Scan(ScannerBase child)
        {
            if (!this.Children.Contains(child))
                return;

            if (!child.IsChecked.GetValueOrDefault())
                return;

            switch (child.Name)
            {

                case "Recycle Bin":
                    ScanRecycleBin();
                    break;

                case "Desktop and Start Menu Icons":
                    ScanDesktopStartMenuIcons();
                    break;
            }
        }

        private void ScanRecycleBin()
        {
            SHQUERYRBINFO sqrbi = new SHQUERYRBINFO() { cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
            int hr = (int)SHQueryRecycleBin(string.Empty, ref sqrbi);
            if (sqrbi.i64NumItems > 0)
                Wizard.StoreCleanDelegate(new CleanDelegate(CleanRecycleBin), "Empty Recycle Bin", sqrbi.i64Size);
        }

        private void CleanRecycleBin()
        {
            int hresult = SHEmptyRecycleBin(IntPtr.Zero, string.Empty, SHERB_NOCONFIRMATION | SHERB_NOSOUND);
        }

        private void ScanDesktopStartMenuIcons()
        {
            ScanDesktopShortcuts();
            ScanStartMenuShortcuts();
        }

        private void ScanDesktopShortcuts()
        {
            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string desktopDir2 = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            List<string> fileList = new List<string>(ParseDirectoryShortcuts(desktopDir));

            // If two paths are different, then check 2nd directory
            if (desktopDir != desktopDir2)
                fileList.AddRange(ParseDirectoryShortcuts(desktopDir2));

            Wizard.StoreBadFileList("Invalid Desktop Shortcuts", fileList.ToArray());
        }

        private void ScanStartMenuShortcuts()
        {
            string startMenuDir = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);

            List<string> fileList = new List<string>(ParseDirectoryShortcuts(startMenuDir));

            Wizard.StoreBadFileList("Invalid Start Menu Shortcuts", fileList.ToArray());
        }

        private List<string> ParseDirectoryShortcuts(string path)
        {
            List<string> fileList = new List<string>();

            foreach (string dirPath in Directory.GetDirectories(path))
                fileList.AddRange(ParseDirectoryShortcuts(dirPath).ToArray());

            foreach (string shortcutPath in Directory.GetFiles(path))
            {
                string filePath = "", fileArgs = "";

                Wizard.CurrentFile = shortcutPath;

                if (fileList.Contains(shortcutPath))
                    continue;

                // Check if shortcut links to a file
                if (Path.GetExtension(shortcutPath) == ".lnk")
                {
                    Utils.ResolveShortcut(shortcutPath, out filePath, out fileArgs);

                    if (string.IsNullOrEmpty(filePath))
                        continue;

                    if (!File.Exists(filePath) && !Directory.Exists(filePath))
                        if (MiscFunctions.IsFileValid(shortcutPath))
                            fileList.Add(shortcutPath);
                }

                // TODO: Check .url files
            }

            return fileList;
        }
    }
}