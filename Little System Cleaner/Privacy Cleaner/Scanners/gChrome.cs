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
using Little_System_Cleaner.Misc;
using System.Windows;
using Microsoft.Win32;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class gChrome : ScannerBase
    {
        private static string _chromeProfileDir = string.Empty;

        private string ChromeDefaultDir
        {
            get { return _chromeProfileDir; }
        }

        public gChrome() 
        {
            Name = "Google Chrome";
            Icon = Properties.Resources.gChrome;

            this.Children.Add(new gChrome(this, "Cookies"));
            this.Children.Add(new gChrome(this, "Download History"));
            this.Children.Add(new gChrome(this, "Internet Cache"));
            this.Children.Add(new gChrome(this, "Internet History"));
        }

        public gChrome(ScannerBase parent, string header)
        {
            Parent = parent;
            Name = header;
        }

        /// <summary>
        /// Checks if Google Chrome is installed
        /// </summary>
        /// <returns>True if its installed</returns>
        internal static bool IsInstalled()
        {
            RegistryKey regKey = null;
            bool installed = false;

            try
            {
                regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Google Chrome");

                if (regKey != null)
                {
                    if (GetChromeUserDir())
                        installed = true;
                    else
                        Debug.WriteLine("Unable to determine Google Chrome profile directory.");
                }
            }
            catch
            {
                installed = false;
            }
            finally
            {
                if (regKey != null)
                    regKey.Close();
            }

            return installed;
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

            // Just in case
            if (string.IsNullOrEmpty(ChromeDefaultDir))
            {
                App.Current.Dispatcher.Invoke(new Action(() => MessageBox.Show(App.Current.MainWindow, "Unable to determine Google Chrome profile directory. Skipping...", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error)));
                
                return;
            }

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

        private static bool GetChromeUserDir()
        {
            string username = Environment.UserName;

            string[] userDataDirs = new string[] {
                string.Format(@"{0}\Google\Chrome\User Data", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
                // Taken from http://www.chromium.org/user-experience/user-data-directory
                string.Format(@"C:\Documents and Settings\{0}\Local Settings\Application Data\Google\Chrome\User Data", Environment.UserName),
                string.Format(@"C:\Users\{0}\AppData\Local\Google\Chrome\User Data", Environment.UserName)
            };

            foreach (string userDataDir in userDataDirs)
            {
                if (!Directory.Exists(userDataDir))
                    return false;

                if (IsValidProfileDir(userDataDir + "\\Default"))
                {
                    _chromeProfileDir = userDataDir + "\\Default";

                    return true;
                }

                foreach (string dir in Directory.GetDirectories(userDataDir))
                {
                    if (IsValidProfileDir(dir))
                    {
                        _chromeProfileDir = dir;

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsValidProfileDir(string path)
        {
            if (!Directory.Exists(path))
                return false;

            try
            {
                // Make sure all the needed files + dirs are there
                string[] neededFilesDirs = new string[] { "Cookies", "History", "Cache" };
                List<string> fileSysEntries = new List<string>(Directory.EnumerateFileSystemEntries(path));

                foreach (string neededFileDir in neededFilesDirs)
                {
                    if (!fileSysEntries.Contains(path + "\\" + neededFileDir))
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        

        private void ScanCookies()
        {
            string cookiesFile = string.Format(@"{0}\Cookies", ChromeDefaultDir);

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
            if (!Wizard.SQLiteLoaded)
                return;

            Wizard.StoreCleanDelegate(new CleanDelegate(CleanDownloadHistory), "Clear Download History", 0);
        }

        private void CleanDownloadHistory()
        {
            try
            {
                using (SQLiteConnection sqliteConn = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", string.Format(@"{0}\History", ChromeDefaultDir))))
                {
                    sqliteConn.Open();

                    using (SQLiteCommand command = sqliteConn.CreateCommand())
                    {
                        command.CommandText = "DROP TABLE downloads";
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show(App.Current.MainWindow, "The following error occurred trying to clear recent downloads in Google Chrome: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void ScanCache() 
        {
            string cacheDir = string.Format(@"{0}\Cache", ChromeDefaultDir);
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

            try
            {
                filePath = string.Format(@"{0}\Archived History", ChromeDefaultDir);
                Wizard.CurrentFile = filePath;
                if (File.Exists(filePath))
                {
                    if (MiscFunctions.IsFileValid(filePath))
                    {
                        fileList.Add(filePath);
                        nTotalSize += MiscFunctions.GetFileSize(filePath);
                    }
                }
            }
            catch (Exception)
            {
            }

            try
            {
                filePath = string.Format(@"{0}\Visited Links", ChromeDefaultDir);
                Wizard.CurrentFile = filePath;
                if (File.Exists(filePath))
                {
                    if (MiscFunctions.IsFileValid(filePath))
                    {
                        fileList.Add(filePath);
                        nTotalSize += MiscFunctions.GetFileSize(filePath);
                    }
                }
            }
            catch (Exception)
            {
            }

            try
            {
                filePath = string.Format(@"{0}\Current Tabs", ChromeDefaultDir);
                Wizard.CurrentFile = filePath;
                if (File.Exists(filePath))
                {
                    if (MiscFunctions.IsFileValid(filePath))
                    {
                        fileList.Add(filePath);
                        nTotalSize += MiscFunctions.GetFileSize(filePath);
                    }
                }
            }
            catch (Exception)
            {
            }
            
            try
            {
                filePath = string.Format(@"{0}\Last Tabs", ChromeDefaultDir);
                Wizard.CurrentFile = filePath;
                if (File.Exists(filePath))
                {
                    if (MiscFunctions.IsFileValid(filePath))
                    {
                        fileList.Add(filePath);
                        nTotalSize += MiscFunctions.GetFileSize(filePath);
                    }
                }
            }
            catch (Exception)
            {
            }
            

            try
            {
                foreach (string fileHistory in Directory.GetFiles(ChromeDefaultDir, "History Index *"))
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
            }
            catch (Exception)
            {
            }
            
            Wizard.StoreBadFileList("Clear Internet History", fileList.ToArray(), nTotalSize);
        }
    }
}