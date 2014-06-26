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
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Data.SQLite;
using System.IO;
using Little_System_Cleaner.Privacy_Cleaner.Controls;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Helpers.Results;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class gChrome : ScannerBase
    {
        public gChrome() 
        {
            Name = "Google Chrome";
            Icon = Properties.Resources.gChrome;
        }

        public gChrome(string header)
        {
            Name = header;
        }

        /// <summary>
        /// Checks if Google Chrome is installed
        /// </summary>
        /// <returns>True if its installed</returns>
        internal static bool IsInstalled()
        {
            string chromeExe = string.Format(@"{0}\Google\Chrome\Application\chrome.exe", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            return File.Exists(chromeExe);
        }

        public override string ProcessName
        {
            get
            {
                return "chrome";
            }
        }

        public override void Scan(ScannerBase child)
        {
            if (!this.Children.Contains(child))
                return;

            if (!child.IsChecked.GetValueOrDefault())
                return;

            switch (child.Name)
            {
                case "Cookies":
                    ScanCookies();
                    break;
                case "Download History":
                    ScanDownloadHistory();
                    break;
                case "Internet Cache":
                    ScanCache();
                    break;
                case "Internet History":
                    ScanInternetHistory();
                    break;
            }
        }

        string chromeDefaultDir
        {
            get { return string.Format(@"{0}\Google\Chrome\User Data\Default", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)); }
        }

        private void ScanCookies()
        {
            string cookiesFile = string.Format(@"{0}\Cookies", chromeDefaultDir);

            Wizard.CurrentFile = cookiesFile;

            if (File.Exists(cookiesFile))
            {
                if (MiscFunctions.IsFileValid(cookiesFile))
                {
                    Wizard.StoreBadFileList("Clear Cookies", new string[] { cookiesFile }, MiscFunctions.GetFileSize(cookiesFile));
                }
            }
        }

        private void ScanDownloadHistory()
        {
            Wizard.StoreCleanDelegate(new CleanDelegate(CleanDownloadHistory), "Clear Download History", 0);
        }

        private void CleanDownloadHistory()
        {
            using (SQLiteConnection sqliteConn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", string.Format(@"{0}\History", chromeDefaultDir))))
            {
                sqliteConn.Open();

                using (SQLiteCommand command = sqliteConn.CreateCommand())
                {
                    command.CommandText = "DROP TABLE downloads";
                    command.ExecuteNonQuery();
                }

                sqliteConn.Close();
            }
        }

        private void ScanCache() 
        {
            string cacheDir = string.Format(@"{0}\Cache", chromeDefaultDir);
            List<string> fileList = new List<string>();
            long nTotalSize = 0;

            foreach (string filePath in Directory.GetFiles(cacheDir))
            {
                Wizard.CurrentFile = filePath;

                if (MiscFunctions.IsFileValid(filePath))
                {    
                    fileList.Add(filePath);
                    nTotalSize += MiscFunctions.GetFileSize(filePath);
                }
            }

            Wizard.StoreBadFileList("Clear Internet Cache", fileList.ToArray(), nTotalSize);
        }

        private void ScanInternetHistory()
        {
            List<string> fileList = new List<string>();
            long nTotalSize = 0;
            string filePath = "";

            filePath = string.Format(@"{0}\Archived History", chromeDefaultDir);
            Wizard.CurrentFile = filePath;
            if (File.Exists(filePath))
            {
                if (MiscFunctions.IsFileValid(filePath))
                {
                    fileList.Add(filePath);
                    nTotalSize += MiscFunctions.GetFileSize(filePath);
                }
            }

            filePath = string.Format(@"{0}\Visited Links", chromeDefaultDir);
            Wizard.CurrentFile = filePath;
            if (File.Exists(filePath))
            {
                if (MiscFunctions.IsFileValid(filePath))
                {
                    fileList.Add(filePath);
                    nTotalSize += MiscFunctions.GetFileSize(filePath);
                }
            }

            filePath = string.Format(@"{0}\Current Tabs", chromeDefaultDir);
            Wizard.CurrentFile = filePath;
            if (File.Exists(filePath))
            {
                if (MiscFunctions.IsFileValid(filePath))
                {
                    fileList.Add(filePath);
                    nTotalSize += MiscFunctions.GetFileSize(filePath);
                }
            }

            filePath = string.Format(@"{0}\Last Tabs", chromeDefaultDir);
            Wizard.CurrentFile = filePath;
            if (File.Exists(filePath))
            {
                if (MiscFunctions.IsFileValid(filePath))
                {
                    fileList.Add(filePath);
                    nTotalSize += MiscFunctions.GetFileSize(filePath);
                }
            }

            foreach (string fileHistory in Directory.GetFiles(chromeDefaultDir, "History Index *")) 
            {
                Wizard.CurrentFile = filePath;

                if (File.Exists(fileHistory))
                {
                    if (MiscFunctions.IsFileValid(fileHistory))
                    {
                        fileList.Add(fileHistory);
                        nTotalSize += MiscFunctions.GetFileSize(fileHistory);
                    }
                }
            }

            Wizard.StoreBadFileList("Clear Internet History", fileList.ToArray(), nTotalSize);
        }
    }
}