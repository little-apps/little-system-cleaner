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

using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Privacy_Cleaner.Controls;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class Misc : ScannerBase
    {
        public Misc()
        {
            Name = "Miscellaneous";

            Children.Add(new Misc(this, "Recycle Bin"));
            Children.Add(new Misc(this, "Desktop and Start Menu Icons"));
        }

        public Misc(ScannerBase parent, string header)
        {
            Parent = parent;
            Name = header;
        }

        public override void Scan(ScannerBase child)
        {
            if (!Children.Contains(child))
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
            var sqrbi = new SHQUERYRBINFO { cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
            SHQueryRecycleBin(string.Empty, ref sqrbi);
            if (sqrbi.i64NumItems > 0)
                Wizard.StoreCleanDelegate(CleanRecycleBin, "Empty Recycle Bin", sqrbi.i64Size);
        }

        private void CleanRecycleBin()
        {
            SHEmptyRecycleBin(IntPtr.Zero, string.Empty, SHERB_NOCONFIRMATION | SHERB_NOSOUND);
        }

        private void ScanDesktopStartMenuIcons()
        {
            ScanDesktopShortcuts();
            ScanStartMenuShortcuts();
        }

        private void ScanDesktopShortcuts()
        {
            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var desktopDir2 = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            var fileList = new List<string>(ParseDirectoryShortcuts(desktopDir));

            // If two paths are different, then check 2nd directory
            if (desktopDir != desktopDir2)
                fileList.AddRange(ParseDirectoryShortcuts(desktopDir2));

            Wizard.StoreBadFileList("Invalid Desktop Shortcuts", fileList.ToArray());
        }

        private void ScanStartMenuShortcuts()
        {
            var startMenuDir = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);

            var fileList = new List<string>(ParseDirectoryShortcuts(startMenuDir));

            Wizard.StoreBadFileList("Invalid Start Menu Shortcuts", fileList.ToArray());
        }

        private List<string> ParseDirectoryShortcuts(string path)
        {
            var fileList = new List<string>();

            foreach (
                var dirPath in
                    Directory.GetDirectories(path).TakeWhile(dirPath => !CancellationToken.IsCancellationRequested))
            {
                fileList.AddRange(ParseDirectoryShortcuts(dirPath).ToArray());
            }

            foreach (
                var shortcutPath in
                    Directory.GetFiles(path).TakeWhile(shortcutPath => !CancellationToken.IsCancellationRequested))
            {
                Wizard.CurrentFile = shortcutPath;

                if (fileList.Contains(shortcutPath))
                    continue;

                // Check if shortcut links to a file
                if (Path.GetExtension(shortcutPath) == ".lnk")
                {
                    string filePath, fileArgs;
                    Utils.ResolveShortcut(shortcutPath, out filePath, out fileArgs);

                    if (string.IsNullOrEmpty(filePath))
                        continue;

                    if (!File.Exists(filePath) && !Directory.Exists(filePath))
                        if (MiscFunctions.IsFileValid(shortcutPath))
                            fileList.Add(shortcutPath);
                }
            }

            return fileList;
        }

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        internal struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        #endregion Structures

        #region Constants

        // No dialog box confirming the deletion of the objects will be displayed.
        internal const int SHERB_NOCONFIRMATION = 0x00000001;

        // No dialog box indicating the progress will be displayed.
        internal const int SHERB_NOPROGRESSUI = 0x00000002;

        // No sound will be played when the operation is complete.
        internal const int SHERB_NOSOUND = 0x00000004;

        #endregion Constants

        #region Functions

        [DllImport("shell32.dll", SetLastError = true)]
        internal static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [DllImport("shell32.dll", SetLastError = true)]
        internal static extern int SHEmptyRecycleBin(IntPtr hWnd, string pszRootPath, uint dwFlags);

        #endregion Functions
    }
}