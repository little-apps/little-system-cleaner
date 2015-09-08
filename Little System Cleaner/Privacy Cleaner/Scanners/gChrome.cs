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
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Privacy_Cleaner.Controls;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Properties;
using Microsoft.Win32;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class GChrome : ScannerBase
    {
        private static string _chromeProfileDir = string.Empty;

        private string ChromeDefaultDir => _chromeProfileDir;

        public GChrome() 
        {
            Name = "Google Chrome";
            Icon = Resources.gChrome;

            Children.Add(new GChrome(this, "Cookies"));
            Children.Add(new GChrome(this, "Download History"));
            Children.Add(new GChrome(this, "Internet Cache"));
            Children.Add(new GChrome(this, "Internet History"));
        }

        public GChrome(ScannerBase parent, string header)
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
                regKey?.Close();
            }

            return installed;
        }

        public override string ProcessName => "chrome";

        public override void Scan(ScannerBase child)
        {
            if (!Children.Contains(child))
                return;

            if (!child.IsChecked.GetValueOrDefault())
                return;

            // Just in case
            if (string.IsNullOrEmpty(ChromeDefaultDir))
            {
                Utils.MessageBoxThreadSafe("Unable to determine Google Chrome profile directory. Skipping...", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

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
            string[] userDataDirs = {
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Google\Chrome\User Data",
                // Taken from http://www.chromium.org/user-experience/user-data-directory
                $@"C:\Documents and Settings\{Environment.UserName}\Local Settings\Application Data\Google\Chrome\User Data",
                $@"C:\Users\{Environment.UserName}\AppData\Local\Google\Chrome\User Data"
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

                foreach (string dir in Directory.GetDirectories(userDataDir).Where(IsValidProfileDir))
                {
                    _chromeProfileDir = dir;

                    return true;
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
                string[] neededFilesDirs = { "Cookies", "History", "Cache" };
                List<string> fileSysEntries = new List<string>(Directory.EnumerateFileSystemEntries(path));

                if (neededFilesDirs.Any(neededFileDir => !fileSysEntries.Contains(path + "\\" + neededFileDir)))
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void ScanCookies()
        {
            string cookiesFile = $@"{ChromeDefaultDir}\Cookies";

            Wizard.CurrentFile = cookiesFile;

            if (!File.Exists(cookiesFile))
                return;

            if (MiscFunctions.IsFileValid(cookiesFile))
            {
                Wizard.StoreBadFileList("Clear Cookies", new[] { cookiesFile }, MiscFunctions.GetFileSize(cookiesFile));
            }
        }

        private void ScanDownloadHistory()
        {
            if (!Wizard.SqLiteLoaded)
                return;

            Wizard.StoreCleanDelegate(CleanDownloadHistory, "Clear Download History", 0);
        }

        private void CleanDownloadHistory()
        {
            try
            {
                using (SQLiteConnection sqliteConn = new SQLiteConnection($"Data Source={$@"{ChromeDefaultDir}\History"};Version=3;FailIfMissing=True"))
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
                MessageBox.Show(Application.Current.MainWindow, "The following error occurred trying to clear recent downloads in Google Chrome: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ScanCache() 
        {
            string cacheDir = $@"{ChromeDefaultDir}\Cache";
            List<string> fileList = new List<string>();
            long nTotalSize = 0;

            foreach (string filePath in Directory.GetFiles(cacheDir))
            {
                Wizard.CurrentFile = filePath;

                if (!MiscFunctions.IsFileValid(filePath))
                    continue;

                fileList.Add(filePath);
                nTotalSize += MiscFunctions.GetFileSize(filePath);
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
                filePath = $@"{ChromeDefaultDir}\Archived History";
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
                // ignored
            }

            try
            {
                filePath = $@"{ChromeDefaultDir}\Visited Links";
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
                // ignored
            }

            try
            {
                filePath = $@"{ChromeDefaultDir}\Current Tabs";
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
                // ignored
            }

            try
            {
                filePath = $@"{ChromeDefaultDir}\Last Tabs";
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
                // ignored
            }


            try
            {
                foreach (string fileHistory in Directory.GetFiles(ChromeDefaultDir, "History Index *"))
                {
                    Wizard.CurrentFile = filePath;

                    if (!File.Exists(fileHistory))
                        continue;

                    if (!MiscFunctions.IsFileValid(fileHistory))
                        continue;

                    fileList.Add(fileHistory);
                    nTotalSize += MiscFunctions.GetFileSize(fileHistory);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            Wizard.StoreBadFileList("Clear Internet History", fileList.ToArray(), nTotalSize);
        }
    }
}