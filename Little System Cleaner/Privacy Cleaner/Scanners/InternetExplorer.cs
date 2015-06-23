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
using System.Windows;
using System.Threading;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class InternetExplorer : ScannerBase
    {
        List<INTERNET_CACHE_ENTRY_INFO> cacheEntriesCookies = new List<INTERNET_CACHE_ENTRY_INFO>();
        List<INTERNET_CACHE_ENTRY_INFO> cacheEntriesCache = new List<INTERNET_CACHE_ENTRY_INFO>();

        #region Internet Explorer Enums
        

        /// <summary>
        /// Flag on the dwFlags parameter of the STATURL structure, used by the SetFilter method.
        /// </summary>
        internal enum STATURLFLAGS : uint
        {
            /// <summary>
            /// Flag on the dwFlags parameter of the STATURL structure indicating that the item is in the cache.
            /// </summary>
            STATURLFLAG_ISCACHED = 0x00000001,
            /// <summary>
            /// Flag on the dwFlags parameter of the STATURL structure indicating that the item is a top-level item.
            /// </summary>
            STATURLFLAG_ISTOPLEVEL = 0x00000002,
        }

        /// <summary>
        /// Used bu the AddHistoryEntry method.
        /// </summary>
        internal enum ADDURL_FLAG : uint
        {
            /// <summary>
            /// Write to both the visited links and the dated containers. 
            /// </summary>
            ADDURL_ADDTOHISTORYANDCACHE = 0,
            /// <summary>
            /// Write to only the visited links container.
            /// </summary>
            ADDURL_ADDTOCACHE = 1
        }

        /// <summary>
        /// Used by QueryUrl method
        /// </summary>
        internal enum STATURL_QUERYFLAGS : uint
        {
            /// <summary>
            /// The specified URL is in the content cache.
            /// </summary>
            STATURL_QUERYFLAG_ISCACHED = 0x00010000,
            /// <summary>
            /// Space for the URL is not allocated when querying for STATURL.
            /// </summary>
            STATURL_QUERYFLAG_NOURL = 0x00020000,
            /// <summary>
            /// Space for the Web page's title is not allocated when querying for STATURL.
            /// </summary>
            STATURL_QUERYFLAG_NOTITLE = 0x00040000,
            /// <summary>
            /// //The item is a top-level item.
            /// </summary>
            STATURL_QUERYFLAG_TOPLEVEL = 0x00080000,

        }

        #endregion
        #region Internet Explorer Structures
        [StructLayout(LayoutKind.Sequential)]
        internal struct UUID
        {
            public int Data1;
            public short Data2;
            public short Data3;
            public byte[] Data4;
        }

        /// <summary>
        /// The structure that contains statistics about a URL. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct STATURL
        {
            /// <summary>
            /// Struct size
            /// </summary>
            public int cbSize;
            /// <summary>
            /// URL
            /// </summary>                                                                   
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pwcsUrl;
            /// <summary>
            /// Page title
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pwcsTitle;
            /// <summary>
            /// Last visited date (UTC)
            /// </summary>
            public FILETIME ftLastVisited;
            /// <summary>
            /// Last updated date (UTC)
            /// </summary>
            public FILETIME ftLastUpdated;
            /// <summary>
            /// The expiry date of the Web page's content (UTC)
            /// </summary>
            public FILETIME ftExpires;
            /// <summary>
            /// Flags. STATURLFLAGS Enumaration.
            /// </summary>
            public STATURLFLAGS dwFlags;

            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public string URL
            {
                get { return pwcsUrl; }
            }
            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public string Title
            {
                get { return pwcsTitle; }
            }
            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public DateTime LastVisited
            {
                get { return DateTime.MinValue; }
            }
            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public DateTime LastUpdated
            {
                get { return DateTime.MinValue; }
            }
            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public DateTime Expires
            {
                get { return DateTime.MinValue; }
            }

        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct INTERNET_CACHE_ENTRY_INFO
        {
            public UInt32 dwStructSize;
            public string lpszSourceUrlName;
            public string lpszLocalFileName;
            public UInt32 CacheEntryType;
            public UInt32 dwUseCount;
            public UInt32 dwHitRate;
            public UInt32 dwSizeLow;
            public UInt32 dwSizeHigh;
            public FILETIME LastModifiedTime;
            public FILETIME ExpireTime;
            public FILETIME LastAccessTime;
            public FILETIME LastSyncTime;
            public IntPtr lpHeaderInfo;
            public UInt32 dwHeaderInfoSize;
            public string lpszFileExtension;
            public ExemptDeltaOrReserverd dwExemptDeltaOrReserved;

        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct ExemptDeltaOrReserverd
        {
            [FieldOffset(0)]
            public UInt32 dwReserved;
            [FieldOffset(0)]
            public UInt32 dwExemptDelta;
        }
        #endregion
        #region Internet Explorer Interfaces
        //UrlHistory class
        [ComImport]
        [Guid("3C374A40-BAE4-11CF-BF7D-00AA006946EE")]
        internal class UrlHistoryClass
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("AFA0DC11-C313-11D0-831A-00C04FD5AE38")]
        internal interface IUrlHistoryStg2
        {
            UInt32 AddUrl(string pocsUrl, string pocsTitle, ADDURL_FLAG dwFlags);
            UInt32 DeleteUrl(string pocsUrl, int dwFlags);
            UInt32 QueryUrl([MarshalAs(UnmanagedType.LPWStr)] string pocsUrl, STATURL_QUERYFLAGS dwFlags, ref STATURL lpSTATURL);
            UInt32 BindToObject([In] string pocsUrl, [In] UUID riid, IntPtr ppvOut);
            UInt32 EnumUrls([Out] IntPtr ppEnum);
            [PreserveSig]
            UInt32 AddUrlAndNotify(IntPtr pocsUrl, IntPtr pocsTitle, int dwFlags, int fWriteHistory, IntPtr IOleCommandTarget, IntPtr punkISFolder);
            UInt32 ClearHistory();
        }
        #endregion
        #region Internet Explorer Functions
        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "DeleteUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        internal static extern bool DeleteUrlCacheEntry([MarshalAs(UnmanagedType.LPStr)] string lpszUrlName);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "UnlockUrlCacheEntryFileA", CallingConvention = CallingConvention.StdCall)]
        internal static extern bool UnlockUrlCacheEntryFile([MarshalAs(UnmanagedType.LPStr)] string lpszUrlName, uint dwReserved);
        #endregion

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
            try
            {
                UrlHistoryClass url = new UrlHistoryClass();
                IUrlHistoryStg2 obj = (IUrlHistoryStg2)url;

                obj.ClearHistory();
            }
            catch (Exception ex)
            {
                Action showMsgBox = new Action(() => MessageBox.Show(App.Current.MainWindow, "An error occurred trying to clear Internet Explorer history. The following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error));

                if (Thread.CurrentThread != App.Current.Dispatcher.Thread)
                    App.Current.Dispatcher.Invoke(showMsgBox);
                else
                    showMsgBox();
            }
            
        }

        private void ScanCookies()
        {
            long folderSize = 0;

            foreach (INTERNET_CACHE_ENTRY_INFO cacheEntry in MiscFunctions.FindUrlCacheEntries("cookie:"))
            {
                cacheEntriesCookies.Add(cacheEntry);

                folderSize += cacheEntry.dwSizeHigh;
            }
            
            if (folderSize > 0)
                Wizard.StoreCleanDelegate(new CleanDelegate(ClearIECookies), "Clear Cookies", folderSize);
        }

        private void ClearIECookies()
        {
            foreach (INTERNET_CACHE_ENTRY_INFO cacheEntry in cacheEntriesCookies)
            {
                if (!DeleteUrlCacheEntry(cacheEntry.lpszSourceUrlName))
                {
                    // ERROR_ACCESS_DENIED
                    if (Marshal.GetLastWin32Error() == 5)
                    {
                        // Unlock file and try again
                        if (UnlockUrlCacheEntryFile(cacheEntry.lpszSourceUrlName, 0))
                            DeleteUrlCacheEntry(cacheEntry.lpszSourceUrlName);
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

            foreach (INTERNET_CACHE_ENTRY_INFO cacheEntry in MiscFunctions.FindUrlCacheEntries(null))
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

            foreach (INTERNET_CACHE_ENTRY_INFO cacheEntry in cacheEntriesCache)
            {

                // Delete entry if its not a cookie
                if ((cacheEntry.CacheEntryType & COOKIE_CACHE_ENTRY) == 0)
                {
                    if (!DeleteUrlCacheEntry(cacheEntry.lpszSourceUrlName))
                    {
                        // ERROR_ACCESS_DENIED
                        if (Marshal.GetLastWin32Error() == 5)
                        {
                            // Unlock file and try again
                            if (UnlockUrlCacheEntryFile(cacheEntry.lpszSourceUrlName, 0))
                                DeleteUrlCacheEntry(cacheEntry.lpszSourceUrlName);
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