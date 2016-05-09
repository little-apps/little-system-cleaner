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
using Little_System_Cleaner.Properties;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class Firefox : ScannerBase
    {
        private string[] _firefoxProfilePaths;

        public Firefox()
        {
            Name = "Mozilla Firefox";
            Icon = Resources.Firefox;

            Children.Add(new Firefox(this, "Internet History"));
            Children.Add(new Firefox(this, "Cookies"));
            Children.Add(new Firefox(this, "Internet Cache"));
            Children.Add(new Firefox(this, "Saved Form Information"));
            Children.Add(new Firefox(this, "Download History"));
        }

        public Firefox(ScannerBase parent, string header)
        {
            Parent = parent;
            Name = header;
        }

        public override string ProcessName => "firefox";

        public string[] FirefoxProfilePaths
        {
            get
            {
                if (_firefoxProfilePaths != null)
                    return _firefoxProfilePaths;

                string firefoxProfilesFile =
                    $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                        }\Mozilla\Firefox\profiles.ini";
                var profilePaths = new List<string>();

                var i = 0;
                while (true)
                {
                    string sectionName = $"Profile{i}";
                    var retVal = new StringBuilder(65536);

                    GetPrivateProfileString(sectionName, "Path", null, retVal, retVal.Capacity, firefoxProfilesFile);

                    if (retVal.Length <= 0)
                        break;

                    profilePaths.Add(
                        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Mozilla\Firefox\{
                            retVal}");

                    i++;
                }

                _firefoxProfilePaths = profilePaths.ToArray();

                return _firefoxProfilePaths;
            }
        }

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal,
            int size, string filePath);

        /// <summary>
        ///     Checks if Mozilla Firefox is installed
        /// </summary>
        /// <returns>True if its installed</returns>
        public static bool IsInstalled()
        {
            // Get install dir
            string firefoxExe =
                $@"{
                    (Utils.Is64BitOs
                        ? Environment.GetEnvironmentVariable("ProgramFiles(x86)")
                        : Environment.GetEnvironmentVariable("ProgramFiles"))}\Mozilla Firefox\firefox.exe";
            return File.Exists(firefoxExe);
        }

        public override void Scan(ScannerBase child)
        {
            if (!Children.Contains(child))
                return;

            if (!child.IsChecked.GetValueOrDefault())
                return;

            //if (Utils.IsProcessRunning("firefox"))
            //{
            //    System.Windows.Forms.MessageBox.Show("Mozilla Firefox must be closed to allow the files to be scanned and cleaned", "Little Privacy Cleaner", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);

            //    if (Utils.IsProcessRunning("firefox"))
            //    {
            //        System.Windows.Forms.MessageBox.Show("Skipping the scanning process...", "Little Privacy Cleaner", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            //        return;
            //    }
            //}

            switch (child.Name)
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

        private void ScanInternetHistory()
        {
            if (!Wizard.SqLiteLoaded)
                return;

            // Firefox 2 and below
            var fileList = new List<string>();
            long nTotalSize = 0;

            foreach (
                var historyFile in
                    FirefoxProfilePaths.Select(firefoxProfilePath => $@"{firefoxProfilePath}\history.dat"))
            {
                Wizard.CurrentFile = historyFile;

                if (File.Exists(historyFile))
                    if (MiscFunctions.IsFileValid(historyFile))
                    {
                        fileList.Add(historyFile);
                        nTotalSize += MiscFunctions.GetFileSize(historyFile);
                    }
            }

            if (fileList.Count > 0)
                Wizard.StoreBadFileList("Firefox v2 Internet History", fileList.ToArray(), nTotalSize);

            Wizard.StoreCleanDelegate(CleanInternetHistory, "Clear Internet History", 0);
        }

        private void CleanInternetHistory()
        {
            foreach (var historyFile in FirefoxProfilePaths.Select(firefoxProfilePath =>
                $@"{firefoxProfilePath}\places.sqlite").Where(File.Exists))
            {
                try
                {
                    using (var sqliteConn = new SQLiteConnection($"Data Source={historyFile};Version=3;"))
                    {
                        sqliteConn.Open();

                        using (var command = sqliteConn.CreateCommand())
                        {
                            command.CommandText = "TRUNCATE TABLE moz_places";
                            command.ExecuteNonQuery();
                        }

                        using (var command = sqliteConn.CreateCommand())
                        {
                            command.CommandText = "TRUNCATE TABLE moz_historyvisits";
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show(Application.Current.MainWindow,
                        "The following error occurred trying to clear the internet history in Mozilla Firefox: " +
                        ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ScanCookies()
        {
            if (!Wizard.SqLiteLoaded)
                return;

            // Firefox 2 and below
            var fileList = new List<string>();
            long nTotalSize = 0;

            foreach (
                var cookiesFile in
                    FirefoxProfilePaths.Select(firefoxProfilePath => $@"{firefoxProfilePath}\cookies.txt"))
            {
                Wizard.CurrentFile = cookiesFile;

                if (File.Exists(cookiesFile))
                    if (MiscFunctions.IsFileValid(cookiesFile))
                    {
                        fileList.Add(cookiesFile);
                        nTotalSize += MiscFunctions.GetFileSize(cookiesFile);
                    }
            }

            if (fileList.Count > 0)
                Wizard.StoreBadFileList("Firefox v2 Cookies", fileList.ToArray(), nTotalSize);

            Wizard.StoreCleanDelegate(CleanCookies, "Cookies", 0);
        }

        private void CleanCookies()
        {
            foreach (
                var cookiesFile in
                    FirefoxProfilePaths.Select(firefoxProfilePath => $@"{firefoxProfilePath}\cookies.sqlite")
                        .Where(File.Exists))
            {
                try
                {
                    using (var sqliteConn = new SQLiteConnection($"Data Source={cookiesFile};Version=3;"))
                    {
                        sqliteConn.Open();

                        using (var command = sqliteConn.CreateCommand())
                        {
                            command.CommandText = "TRUNCATE TABLE moz_cookies";
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show(Application.Current.MainWindow,
                        "The following error occurred trying to clear the cookies in Mozilla Firefox: " + ex.Message,
                        Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ScanCache()
        {
            string profilesDir =
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Mozilla\Firefox\Profiles";
            var fileList = new List<string>();
            long nTotalSize = 0;

            if (Directory.Exists(profilesDir))
            {
                foreach (
                    var fileCache in
                        Directory.GetDirectories(profilesDir, "*.default")
                            .Select(dir => $@"{dir}\Cache")
                            .Where(Directory.Exists)
                            .SelectMany(Directory.GetFiles))
                {
                    Wizard.CurrentFile = fileCache;

                    fileList.Add(fileCache);
                    nTotalSize += MiscFunctions.GetFileSize(fileCache);
                }
            }

            Wizard.StoreBadFileList("Internet Cache Files", fileList.ToArray(), nTotalSize);
        }

        private void ScanFormHistory()
        {
            if (!Wizard.SqLiteLoaded)
                return;

            var fileList = new List<string>();
            long nTotalSize = 0;

            foreach (
                var formHistoryFile in
                    FirefoxProfilePaths.Select(firefoxProfilePath => $@"{firefoxProfilePath}\formhistory.dat"))
            {
                Wizard.CurrentFile = formHistoryFile;

                if (!File.Exists(formHistoryFile))
                    continue;

                if (!MiscFunctions.IsFileValid(formHistoryFile))
                    continue;

                fileList.Add(formHistoryFile);
                nTotalSize += MiscFunctions.GetFileSize(formHistoryFile);
            }

            if (fileList.Count > 0)
                Wizard.StoreBadFileList("Firefox v2 Form History", fileList.ToArray(), nTotalSize);

            Wizard.StoreCleanDelegate(CleanFormHistory, "Clear Form History", 0);
        }

        private void CleanFormHistory()
        {
            foreach (
                var formHistoryFile in
                    FirefoxProfilePaths.Select(firefoxProfilePath => $@"{firefoxProfilePath}\formhistory.sqlite")
                        .Where(File.Exists))
            {
                try
                {
                    using (var sqliteConn = new SQLiteConnection($"Data Source={formHistoryFile};Version=3;"))
                    {
                        sqliteConn.Open();

                        using (var command = sqliteConn.CreateCommand())
                        {
                            command.CommandText = "DROP TABLE moz_formhistory";
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show(Application.Current.MainWindow,
                        "The following error occurred trying to clear the form history in Mozilla Firefox: " +
                        ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ScanDownloadHistory()
        {
            if (!Wizard.SqLiteLoaded)
                return;

            // Firefox 2 and below
            var fileList = new List<string>();
            long nTotalSize = 0;

            foreach (
                var downloadsFile in
                    FirefoxProfilePaths.Select(firefoxProfilePath => $@"{firefoxProfilePath}\downloads.rdf"))
            {
                Wizard.CurrentFile = downloadsFile;

                if (!File.Exists(downloadsFile))
                    continue;

                if (!MiscFunctions.IsFileValid(downloadsFile))
                    continue;

                fileList.Add(downloadsFile);
                nTotalSize += MiscFunctions.GetFileSize(downloadsFile);
            }

            if (fileList.Count > 0)
                Wizard.StoreBadFileList("Firefox v2 Download History", fileList.ToArray(), nTotalSize);

            Wizard.StoreCleanDelegate(CleanDownloadHistory, "Clear Download History", 0);
        }

        private void CleanDownloadHistory()
        {
            foreach (
                var downloadsFile in
                    FirefoxProfilePaths.Select(firefoxProfilePath => $@"{firefoxProfilePath}\downloads.sqlite")
                        .Where(File.Exists))
            {
                try
                {
                    using (var sqliteConn = new SQLiteConnection($"Data Source={downloadsFile};Version=3;"))
                    {
                        sqliteConn.Open();

                        using (var command = sqliteConn.CreateCommand())
                        {
                            command.CommandText = "DROP TABLE moz_downloads";
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show(Application.Current.MainWindow,
                        "The following error occurred trying to clear the download history in Mozilla Firefox: " +
                        ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}