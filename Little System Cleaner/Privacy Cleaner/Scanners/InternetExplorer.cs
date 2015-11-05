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
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Privacy_Cleaner.Controls;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class InternetExplorer : ScannerBase
    {
        readonly List<INTERNET_CACHE_ENTRY_INFO> cacheEntriesCookies = new List<INTERNET_CACHE_ENTRY_INFO>();
        readonly List<INTERNET_CACHE_ENTRY_INFO> cacheEntriesCache = new List<INTERNET_CACHE_ENTRY_INFO>();

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
            STATURLFLAG_ISTOPLEVEL = 0x00000002
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
            STATURL_QUERYFLAG_TOPLEVEL = 0x00080000

        }

        #endregion
        #region Internet Explorer Structures
        [StructLayout(LayoutKind.Sequential)]
        internal struct Uuid
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
            public string Url => pwcsUrl;

            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public string Title => pwcsTitle;

            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public DateTime LastVisited => DateTime.MinValue;

            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public DateTime LastUpdated => DateTime.MinValue;

            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public DateTime Expires => DateTime.MinValue;
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
            uint AddUrl(string pocsUrl, string pocsTitle, ADDURL_FLAG dwFlags);
            uint DeleteUrl(string pocsUrl, int dwFlags);
            uint QueryUrl([MarshalAs(UnmanagedType.LPWStr)] string pocsUrl, STATURL_QUERYFLAGS dwFlags, ref STATURL lpSTATURL);
            uint BindToObject([In] string pocsUrl, [In] Uuid riid, IntPtr ppvOut);
            uint EnumUrls([Out] IntPtr ppEnum);
            [PreserveSig]
            uint AddUrlAndNotify(IntPtr pocsUrl, IntPtr pocsTitle, int dwFlags, int fWriteHistory, IntPtr IOleCommandTarget, IntPtr punkIsFolder);
            uint ClearHistory();
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
            Icon = Resources.InternetExplorer;

            Children.Add(new InternetExplorer(this, "History"));
            Children.Add(new InternetExplorer(this, "Cookies"));
            Children.Add(new InternetExplorer(this, "Auto Complete"));
            Children.Add(new InternetExplorer(this, "Temporary Internet Files"));
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

        public override string ProcessName => "iexplore";

        public override void Scan(ScannerBase child)
        {
            if (!Children.Contains(child))
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
            Wizard.StoreCleanDelegate(ClearHistory, "Clear History", 0);
        }

        private void ClearHistory()
        {
            try
            {
                var url = new UrlHistoryClass();
                var obj = (IUrlHistoryStg2)url;

                obj.ClearHistory();
            }
            catch (Exception ex)
            {
                Utils.MessageBoxThreadSafe("An error occurred trying to clear Internet Explorer history. The following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void ScanCookies()
        {
            var folderSize = MiscFunctions.FindUrlCacheEntries("cookie:").Aggregate(0L, (i, info) => i + info.dwSizeHigh);

            cacheEntriesCookies.AddRange(MiscFunctions.FindUrlCacheEntries("cookie:"));

            if (folderSize > 0)
                Wizard.StoreCleanDelegate(ClearIeCookies, "Clear Cookies", folderSize);
        }

        private void ClearIeCookies()
        {
            foreach (
                var cacheEntry in
                    cacheEntriesCookies.Where(cacheEntry => !DeleteUrlCacheEntry(cacheEntry.lpszSourceUrlName))
                        .Where(cacheEntry => Marshal.GetLastWin32Error() == 5)
                        .Where(cacheEntry => UnlockUrlCacheEntryFile(cacheEntry.lpszSourceUrlName, 0)))
            {
                DeleteUrlCacheEntry(cacheEntry.lpszSourceUrlName);
            }
        }

        private static void ScanAutoComplete()
        {
            Wizard.StoreCleanDelegate(ClearFormData, "Clear Form Data & Passwords", 0);
        }

        private static void ClearFormData()
        {
            if (Utils.MessageBoxThreadSafe("This will delete your saved form data and passwords. Continue?", Utils.ProductName, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;

            // Clear Form Data
            var proc = Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 16");
            proc?.WaitForExit();

            // Clear Saved Passwords
            proc = Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 32");
            proc?.WaitForExit();
        }

        private void ScanTemporaryFiles()
        {
            var cacheEntries = MiscFunctions.FindUrlCacheEntries(null);
            var folderSize = cacheEntries.Aggregate(0L, (i, info) => i + info.dwSizeHigh);

            cacheEntriesCache.AddRange(cacheEntries);

            if (folderSize > 0)
                Wizard.StoreCleanDelegate(ClearIeCache, "Clear Cache Files", folderSize);
        }

        private void ClearIeCache()
        {
            const uint COOKIE_CACHE_ENTRY = 0x00100000;

            foreach (
                var sourceUrlName in
                    cacheEntriesCache.Where(cacheEntry => (cacheEntry.CacheEntryType & COOKIE_CACHE_ENTRY) == 0)
                        .Where(cacheEntry => !DeleteUrlCacheEntry(cacheEntry.lpszSourceUrlName))
                        .Where(cacheEntry => Marshal.GetLastWin32Error() == 5)
                        .Where(cacheEntry => UnlockUrlCacheEntryFile(cacheEntry.lpszSourceUrlName, 0))
                        .Select(cacheEntry => cacheEntry.lpszSourceUrlName))
            {
                DeleteUrlCacheEntry(sourceUrlName);
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