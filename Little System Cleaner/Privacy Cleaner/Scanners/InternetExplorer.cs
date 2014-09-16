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
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Little_System_Cleaner.Privacy_Cleaner.Controls;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Helpers.Results;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class InternetExplorer : ScannerBase
    {
        List<PInvoke.INTERNET_CACHE_ENTRY_INFO> cacheEntriesCookies = new List<PInvoke.INTERNET_CACHE_ENTRY_INFO>();
        List<PInvoke.INTERNET_CACHE_ENTRY_INFO> cacheEntriesCache = new List<PInvoke.INTERNET_CACHE_ENTRY_INFO>();

        public InternetExplorer() 
        {
            Name = "Internet Explorer";
            Icon = Properties.Resources.InternetExplorer;

            this.Children.Add(new InternetExplorer(this, "History"));
            this.Children.Add(new InternetExplorer(this, "Cookies"));
            this.Children.Add(new InternetExplorer(this, "Auto Complete"));
            this.Children.Add(new InternetExplorer(this, "Temporary Internet Files"));
            //this.Children.Add(new InternetExplorer(this, "Index.dat Files"));
        }

        public InternetExplorer(ScannerBase parent, string header)
        {
            Parent = parent;
            Name = header;
        }

        /// <summary>
        /// Checks if IE is installed
        /// </summary>
        /// <returns>True if its installed</returns>
        internal static bool IsInstalled()
        {
            // Automatically return true
            return true;
        }

        public override string ProcessName
        {
            get
            {
                return "iexplore";
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
                case "History":
                    ScanHistory();
                    break;
                case "Cookies":
                    ScanCookies();
                    break;
                case "Auto Complete":
                    ScanAutoComplete();
                    break;
                case "Temporary Internet Files":
                    ScanTemporaryFiles();
                    break;
                //case "Index.dat Files":
                //    ScanIndexFiles();
                //    break;
            }
        }

        private void ScanHistory()
        {
            Wizard.StoreCleanDelegate(new CleanDelegate(ClearHistory), "Clear History", 0);
        }

        private void ClearHistory()
        {
            PInvoke.UrlHistoryClass url = new PInvoke.UrlHistoryClass();
            PInvoke.IUrlHistoryStg2 obj = (PInvoke.IUrlHistoryStg2)url;

            obj.ClearHistory();
        }

        private void ScanCookies()
        {
            long folderSize = 0;

            foreach (PInvoke.INTERNET_CACHE_ENTRY_INFO cacheEntry in MiscFunctions.FindUrlCacheEntries("cookie:"))
            {
                cacheEntriesCookies.Add(cacheEntry);

                folderSize += cacheEntry.dwSizeHigh;
            }
            
            if (folderSize > 0)
                Wizard.StoreCleanDelegate(new CleanDelegate(ClearIECookies), "Clear Cookies", folderSize);
        }

        private void ClearIECookies()
        {
            foreach (PInvoke.INTERNET_CACHE_ENTRY_INFO cacheEntry in cacheEntriesCookies)
            {
                if (!PInvoke.DeleteUrlCacheEntry(cacheEntry.lpszSourceUrlName))
                {
                    // ERROR_ACCESS_DENIED
                    if (Marshal.GetLastWin32Error() == 5)
                    {
                        // Unlock file and try again
                        if (PInvoke.UnlockUrlCacheEntryFile(cacheEntry.lpszSourceUrlName, 0))
                            PInvoke.DeleteUrlCacheEntry(cacheEntry.lpszSourceUrlName);
                    }
                }
            }
        }

        private void ScanAutoComplete()
        {
            Wizard.StoreCleanDelegate(new CleanDelegate(ClearFormData), "Clear Form Data & Passwords", 0);
        }

        private void ClearFormData()
        {
            if (System.Windows.Forms.MessageBox.Show("This will delete your saved form data and passwords. Continue?", Utils.ProductName, System.Windows.Forms.MessageBoxButtons.OKCancel, System.Windows.Forms.MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
            {
                Process proc;

                // Clear Form Data
                proc = Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 16");
                proc.WaitForExit();

                // Clear Saved Passwords
                proc = Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 32");
                proc.WaitForExit();
            }
        }

        private void ScanTemporaryFiles()
        {
            long folderSize = 0;

            foreach (PInvoke.INTERNET_CACHE_ENTRY_INFO cacheEntry in MiscFunctions.FindUrlCacheEntries(null))
            {
                cacheEntriesCache.Add(cacheEntry);

                folderSize += cacheEntry.dwSizeHigh;
            }

            if (folderSize > 0)
                Wizard.StoreCleanDelegate(new CleanDelegate(ClearIECache), "Clear Cache Files", folderSize);
        }

        private void ClearIECache()
        {
            const uint COOKIE_CACHE_ENTRY = 0x00100000;

            foreach (PInvoke.INTERNET_CACHE_ENTRY_INFO cacheEntry in cacheEntriesCache)
            {

                // Delete entry if its not a cookie
                if ((cacheEntry.CacheEntryType & COOKIE_CACHE_ENTRY) == 0)
                {
                    if (!PInvoke.DeleteUrlCacheEntry(cacheEntry.lpszSourceUrlName))
                    {
                        // ERROR_ACCESS_DENIED
                        if (Marshal.GetLastWin32Error() == 5)
                        {
                            // Unlock file and try again
                            if (PInvoke.UnlockUrlCacheEntryFile(cacheEntry.lpszSourceUrlName, 0))
                                PInvoke.DeleteUrlCacheEntry(cacheEntry.lpszSourceUrlName);
                        }
                    }

                }
            }
        }

        // TODO: Find a way to unlock and remove index.dat files safely
        //private void ScanIndexFiles()
        //{
        //    List<string> fileList = new List<string>();

        //    List<string> fileListTemp = new List<string>() {
        //        Environment.ExpandEnvironmentVariables("%userprofile%\\Local Settings\\History\\History.IE5\\index.dat"),
        //        Environment.ExpandEnvironmentVariables("%userprofile%\\Cookies\\index.dat"),
        //        Environment.ExpandEnvironmentVariables("%userprofile%\\Local Settings\\Temporary Internet Files\\Content.IE5\\index.dat"),
        //    };

        //    foreach (string file in fileListTemp)
        //    {
        //        if (File.Exists(file) && Utils.IsFileValid(file))
        //            fileList.Add(file);
        //    }

        //    Analyze.StoreBadFileList("Clear Index.DAT Files", fileList.ToArray());
        //}
    }
}