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
using System.Threading;
using System.Runtime.InteropServices;
using System.Data.SQLite;
using System.IO;
using Little_System_Cleaner.Privacy_Cleaner.Controls;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class Firefox : ScannerBase
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,string key, string def, StringBuilder retVal,int size, string filePath);

        public Firefox() 
        {
            Name = "Mozilla Firefox";
            Icon = Properties.Resources.Firefox;
        }

        public Firefox(string header)
        {
            Name = header;
        }

        /// <summary>
        /// Checks if Mozilla Firefox is installed
        /// </summary>
        /// <returns>True if its installed</returns>
        public static bool IsInstalled()
        {
            // Get install dir
            string firefoxExe = string.Format(@"{0}\Mozilla Firefox\firefox.exe", ((Utils.Is64BitOS)?(Environment.GetEnvironmentVariable("ProgramFiles(x86)")):(Environment.GetEnvironmentVariable("ProgramFiles"))));
            return File.Exists(firefoxExe);
        }

        
        public override string ProcessName
        {
            get
            {
                return "firefox";
            }
        }

        public override void Scan()
        {
            //if (Utils.IsProcessRunning("firefox"))
            //{
            //    System.Windows.Forms.MessageBox.Show("Mozilla Firefox must be closed to allow the files to be scanned and cleaned", "Little Privacy Cleaner", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);

            //    if (Utils.IsProcessRunning("firefox"))
            //    {
            //        System.Windows.Forms.MessageBox.Show("Skipping the scanning process...", "Little Privacy Cleaner", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            //        return;
            //    }
            //}

            foreach (ScannerBase n in this.Children)
            {
                if (n.IsChecked.GetValueOrDefault() == false)
                    continue;

                switch (n.Name)
                {
                    case "Internet History":
                        ScanInternetHistory();
                        break;
                    case "Cookies":
                        ScanCookies();
                        break;
                    case "Internet Cache":
                        ScanCache();
                        break;
                    case "Saved Form History":
                        ScanFormHistory();
                        break;
                    case "Download History":
                        ScanDownloadHistory();
                        break;
                }
            }
        }

        private string[] _firefoxProfilePaths = null;
        public string[] FirefoxProfilePaths
        {
            get {
                if (_firefoxProfilePaths == null)
                {
                    string firefoxProfilesFile = string.Format(@"{0}\Mozilla\Firefox\profiles.ini", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                    List<string> profilePaths = new List<string>();

                    int i = 0;
                    while (true)
                    {
                        string sectionName = string.Format("Profile{0}", i);
                        StringBuilder retVal = new StringBuilder(65536);

                        GetPrivateProfileString(sectionName, "Path", null, retVal, retVal.Capacity, firefoxProfilesFile);

                        if (retVal.Length <= 0)
                            break;

                        profilePaths.Add(string.Format(@"{0}\Mozilla\Firefox\{1}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), retVal.ToString()));

                        i++;
                    }

                    this._firefoxProfilePaths = profilePaths.ToArray();
                }

                return this._firefoxProfilePaths;
            }
            
        }

        private void ScanInternetHistory()
        {
            // Firefox 2 and below
            List<string> fileList = new List<string>();
            long nTotalSize = 0;

            foreach (string firefoxProfilePath in this.FirefoxProfilePaths)
            {
                string historyFile = string.Format(@"{0}\history.dat", firefoxProfilePath);

                Wizard.CurrentFile = historyFile;

                if (File.Exists(historyFile))
                    if (Utils.IsFileValid(historyFile)) 
                    {
                        fileList.Add(historyFile);
                        nTotalSize += Utils.GetFileSize(historyFile);
                    }
            }

            if (fileList.Count > 0)
                Wizard.StoreBadFileList("Firefox v2 Internet History", fileList.ToArray(), nTotalSize);

            Wizard.StoreCleanDelegate(new CleanDelegate(CleanInternetHistory), "Clear Internet History", 0);
        }

        private void CleanInternetHistory() {
            foreach (string firefoxProfilePath in this.FirefoxProfilePaths)
            {
                // Firefox 3
                string historyFile = string.Format(@"{0}\places.sqlite", firefoxProfilePath);

                if (!File.Exists(historyFile))
                    continue;

                using (SQLiteConnection sqliteConn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", historyFile)))
                {
                    sqliteConn.Open();

                    using (SQLiteCommand command = sqliteConn.CreateCommand())
                    {
                        command.CommandText = "TRUNCATE TABLE moz_places";
                        command.ExecuteNonQuery();
                    }

                    using (SQLiteCommand command = sqliteConn.CreateCommand())
                    {
                        command.CommandText = "TRUNCATE TABLE moz_historyvisits";
                        command.ExecuteNonQuery();
                    }

                    sqliteConn.Close();
                }
            }
        }

        private void ScanCookies()
        {
            // Firefox 2 and below
            List<string> fileList = new List<string>();
            long nTotalSize = 0;

            foreach (string firefoxProfilePath in this.FirefoxProfilePaths)
            {
                string cookiesFile = string.Format(@"{0}\cookies.txt", firefoxProfilePath);

                Wizard.CurrentFile = cookiesFile;

                if (File.Exists(cookiesFile))
                    if (Utils.IsFileValid(cookiesFile))
                    {
                        fileList.Add(cookiesFile);
                        nTotalSize += Utils.GetFileSize(cookiesFile);
                    }
            }

            if (fileList.Count > 0)
                Wizard.StoreBadFileList("Firefox v2 Cookies", fileList.ToArray(), nTotalSize);

            Wizard.StoreCleanDelegate(new CleanDelegate(CleanCookies), "Cookies", 0);
        }

        private void CleanCookies()
        {
            foreach (string firefoxProfilePath in this.FirefoxProfilePaths)
            {
                // Firefox 3
                string cookiesFile = string.Format(@"{0}\cookies.sqlite", firefoxProfilePath);

                if (!File.Exists(cookiesFile))
                    continue;

                using (SQLiteConnection sqliteConn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", cookiesFile)))
                {
                    sqliteConn.Open();

                    using (SQLiteCommand command = sqliteConn.CreateCommand())
                    {
                        command.CommandText = "TRUNCATE TABLE moz_cookies";
                        command.ExecuteNonQuery();
                    }

                    sqliteConn.Close();
                }
            }
        }

        private void ScanCache()
        {
            string profilesDir = string.Format(@"{0}\Mozilla\Firefox\Profiles", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            List<string> fileList = new List<string>();
            long nTotalSize = 0;

            if (Directory.Exists(profilesDir))
            {
                foreach (string dir in Directory.GetDirectories(profilesDir, "*.default"))
                {
                    string dirCache = string.Format(@"{0}\Cache", dir);

                    if (Directory.Exists(dirCache))
                    {
                        foreach (string fileCache in Directory.GetFiles(dirCache))
                        {
                            Wizard.CurrentFile = fileCache;

                            fileList.Add(fileCache);
                            nTotalSize += Utils.GetFileSize(fileCache);
                        }
                    }
                }
            }


            Wizard.StoreBadFileList("Internet Cache Files", fileList.ToArray(), nTotalSize);
        }

        private void ScanFormHistory()
        {
            List<string> fileList = new List<string>();
            long nTotalSize = 0;

            foreach (string firefoxProfilePath in this.FirefoxProfilePaths)
            {
                string formHistoryFile = string.Format(@"{0}\formhistory.dat", firefoxProfilePath);

                Wizard.CurrentFile = formHistoryFile;

                if (File.Exists(formHistoryFile))
                    if (Utils.IsFileValid(formHistoryFile))
                    {
                        fileList.Add(formHistoryFile);
                        nTotalSize += Utils.GetFileSize(formHistoryFile);
                    }
            }

            if (fileList.Count > 0)
                Wizard.StoreBadFileList("Firefox v2 Form History", fileList.ToArray(), nTotalSize);

            Wizard.StoreCleanDelegate(new CleanDelegate(CleanFormHistory), "Clear Form History", 0);
        }

        private void CleanFormHistory()
        {
            foreach (string firefoxProfilePath in this.FirefoxProfilePaths)
            {
                // Firefox 3
                string formHistoryFile = string.Format(@"{0}\formhistory.sqlite", firefoxProfilePath);

                if (!File.Exists(formHistoryFile))
                    continue;

                using (SQLiteConnection sqliteConn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", formHistoryFile)))
                {
                    sqliteConn.Open();

                    using (SQLiteCommand command = sqliteConn.CreateCommand())
                    {
                        command.CommandText = "DROP TABLE moz_formhistory";
                        command.ExecuteNonQuery();
                    }

                    sqliteConn.Close();
                }
            }
        }

        private void ScanDownloadHistory()
        {
            // Firefox 2 and below
            List<string> fileList = new List<string>();
            long nTotalSize = 0;

            foreach (string firefoxProfilePath in this.FirefoxProfilePaths)
            {
                string downloadsFile = string.Format(@"{0}\downloads.rdf", firefoxProfilePath);

                Wizard.CurrentFile = downloadsFile;

                if (File.Exists(downloadsFile))
                    if (Utils.IsFileValid(downloadsFile))
                    {
                        fileList.Add(downloadsFile);
                        nTotalSize += Utils.GetFileSize(downloadsFile);
                    }
            }

            if (fileList.Count > 0)
                Wizard.StoreBadFileList("Firefox v2 Download History", fileList.ToArray(), nTotalSize);

            Wizard.StoreCleanDelegate(new CleanDelegate(CleanDownloadHistory), "Clear Download History", 0);
        }

        private void CleanDownloadHistory()
        {
            foreach (string firefoxProfilePath in this.FirefoxProfilePaths)
            {
                // Firefox 3
                string downloadsFile = string.Format(@"{0}\downloads.sqlite", firefoxProfilePath);

                if (!File.Exists(downloadsFile))
                    continue;

                using (SQLiteConnection sqliteConn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", downloadsFile)))
                {
                    sqliteConn.Open();

                    using (SQLiteCommand command = sqliteConn.CreateCommand())
                    {
                        command.CommandText = "DROP TABLE moz_downloads";
                        command.ExecuteNonQuery();
                    }

                    sqliteConn.Close();
                }
            }
        }
    }
}
