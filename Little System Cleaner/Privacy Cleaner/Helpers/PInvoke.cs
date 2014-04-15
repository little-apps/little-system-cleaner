using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers
{
    internal static class PInvoke
    {
        public static int SW_SHOW = 5;
        public static uint SEE_MASK_INVOKEIDLIST = 12;

        /// <summary>
        /// Used by ShellExecuteEx()
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        /// <summary>
        /// Used by QueryUrl method
        /// </summary>
        public enum STATURL_QUERYFLAGS : uint
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
        /// <summary>
        /// Flag on the dwFlags parameter of the STATURL structure, used by the SetFilter method.
        /// </summary>
        public enum STATURLFLAGS : uint
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
        public enum ADDURL_FLAG : uint
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
        /// The structure that contains statistics about a URL. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct STATURL
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
        public struct UUID
        {
            public int Data1;
            public short Data2;
            public short Data3;
            public byte[] Data4;
        }

        // Enumerates the cached URLs
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("3C374A42-BAE4-11CF-BF7D-00AA006946EE")]
        public interface IEnumSTATURL
        {
            void Next(int celt, ref STATURL rgelt, out int pceltFetched);	//Returns the next \"celt\" URLS from the cache
            void Skip(int celt);	//Skips the next \"celt\" URLS from the cache. doed not work.
            void Reset();	//Resets the enumeration
            void Clone(out IEnumSTATURL ppenum);	//Clones this object
            void SetFilter([MarshalAs(UnmanagedType.LPWStr)] string poszFilter, STATURLFLAGS dwFlags);	//Sets the enumeration filter

        }


        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("AFA0DC11-C313-11D0-831A-00C04FD5AE38")]
        public interface IUrlHistoryStg2
        {
            void AddUrl(string pocsUrl, string pocsTitle, ADDURL_FLAG dwFlags);
            void DeleteUrl(string pocsUrl, int dwFlags);
            void QueryUrl([MarshalAs(UnmanagedType.LPWStr)] string pocsUrl, STATURL_QUERYFLAGS dwFlags, ref STATURL lpSTATURL);
            void BindToObject([In] string pocsUrl, [In] UUID riid, IntPtr ppvOut);
            object EnumUrls { [return: MarshalAs(UnmanagedType.IUnknown)] get; }

            void AddUrlAndNotify(string pocsUrl, string pocsTitle, int dwFlags, int fWriteHistory, object poctNotify, object punkISFolder);
            void ClearHistory();
        }

        //UrlHistory class
        [ComImport]
        [Guid("3C374A40-BAE4-11CF-BF7D-00AA006946EE")]
        public class UrlHistoryClass
        {
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ExemptDeltaOrReserverd
        {
            [FieldOffset(0)]
            public UInt32 dwReserved;
            [FieldOffset(0)]
            public UInt32 dwExemptDelta;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INTERNET_CACHE_ENTRY_INFO
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

        [DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "ShellExecuteExA")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "FindFirstUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr FindFirstUrlCacheEntry([MarshalAs(UnmanagedType.LPStr)] string lpszUrlSearchPattern, IntPtr lpFirstCacheEntryInfo, ref int lpdwFirstCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "FindNextUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        public static extern bool FindNextUrlCacheEntry(IntPtr hFind, IntPtr lpNextCacheEntryInfo, ref int lpdwNextCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true)]
        public static extern long FindCloseUrlCache(IntPtr hEnumHandle);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "DeleteUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        public static extern bool DeleteUrlCacheEntry([MarshalAs(UnmanagedType.LPStr)] string lpszUrlName);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "UnlockUrlCacheEntryFileA", CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnlockUrlCacheEntryFile([MarshalAs(UnmanagedType.LPStr)] string lpszUrlName, uint dwReserved);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern uint GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer, uint nSize, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

        public static List<INTERNET_CACHE_ENTRY_INFO> FindUrlCacheEntries(string urlPattern)
        {
            List<INTERNET_CACHE_ENTRY_INFO> cacheEntryList = new List<INTERNET_CACHE_ENTRY_INFO>();

            int structSize = 0;

            IntPtr bufferPtr = IntPtr.Zero;
            IntPtr cacheEnumHandle = FindFirstUrlCacheEntry(urlPattern, bufferPtr, ref structSize);

            switch (Marshal.GetLastWin32Error())
            {
                // ERROR_SUCCESS
                case 0:
                    if (cacheEnumHandle.ToInt32() > 0)
                    {
                        // Store entry
                        INTERNET_CACHE_ENTRY_INFO cacheEntry = (INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(bufferPtr, typeof(INTERNET_CACHE_ENTRY_INFO));
                        cacheEntryList.Add(cacheEntry);
                    }
                    break;

                // ERROR_INSUFFICIENT_BUFFER
                case 122:
                    // Repeat call to API with size returned by first call
                    bufferPtr = Marshal.AllocHGlobal(structSize);
                    cacheEnumHandle = FindFirstUrlCacheEntry(urlPattern, bufferPtr, ref structSize);

                    if (cacheEnumHandle.ToInt32() > 0)
                    {
                        // Store entry
                        INTERNET_CACHE_ENTRY_INFO cacheEntry = (INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(bufferPtr, typeof(INTERNET_CACHE_ENTRY_INFO));
                        cacheEntryList.Add(cacheEntry);
                        break;
                    }
                    else
                    {
                        // Failed to get handle, return...
                        Marshal.FreeHGlobal(bufferPtr);
                        FindCloseUrlCache(cacheEnumHandle);
                        return cacheEntryList;
                    }
                default:
                    Marshal.FreeHGlobal(bufferPtr);
                    FindCloseUrlCache(cacheEnumHandle);
                    return cacheEntryList;
            }

            do
            {
                bufferPtr = Marshal.ReAllocHGlobal(bufferPtr, new IntPtr(structSize));
                if (FindNextUrlCacheEntry(cacheEnumHandle, bufferPtr, ref structSize))
                {
                    // Store entry
                    INTERNET_CACHE_ENTRY_INFO cacheEntry = (INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(bufferPtr, typeof(INTERNET_CACHE_ENTRY_INFO));
                    cacheEntryList.Add(cacheEntry);
                }
                else
                {
                    switch (Marshal.GetLastWin32Error())
                    {
                        // ERROR_INSUFFICIENT_BUFFER
                        case 122:
                            // Repeat call to API with size returned by first call
                            bufferPtr = Marshal.ReAllocHGlobal(bufferPtr, new IntPtr(structSize));

                            if (FindNextUrlCacheEntry(cacheEnumHandle, bufferPtr, ref structSize))
                            {
                                // Store entry
                                INTERNET_CACHE_ENTRY_INFO cacheEntry = (INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(bufferPtr, typeof(INTERNET_CACHE_ENTRY_INFO));
                                cacheEntryList.Add(cacheEntry);
                                break;
                            }
                            else
                            {
                                Marshal.FreeHGlobal(bufferPtr);
                                FindCloseUrlCache(cacheEnumHandle);
                                return cacheEntryList;
                            }
                        // ERROR_NO_MORE_ITEMS
                        case 259:
                            Marshal.FreeHGlobal(bufferPtr);
                            FindCloseUrlCache(cacheEnumHandle);
                            return cacheEntryList;
                        default:
                            Marshal.FreeHGlobal(bufferPtr);
                            FindCloseUrlCache(cacheEnumHandle);
                            return cacheEntryList;
                    }
                }
            } while (true);

            // Wont reach here
        }

        public static string[] GetSections(string filePath)
        {
            uint MAX_BUFFER = 32767;
            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);
            uint bytesReturned = GetPrivateProfileSectionNames(pReturnedString, MAX_BUFFER, filePath);
            if (bytesReturned == 0)
            {
                Marshal.FreeCoTaskMem(pReturnedString);
                return new string[] { };
            }

            string local = Marshal.PtrToStringAnsi(pReturnedString, (int)bytesReturned).ToString();
            Marshal.FreeCoTaskMem(pReturnedString);

            return local.Substring(0, local.Length - 1).Split('\0');
        }

        public static StringDictionary GetValues(string filePath, string sectionName)
        {
            uint MAX_BUFFER = 32767;

            StringDictionary ret = new StringDictionary();

            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);

            uint bytesReturned = GetPrivateProfileSection(sectionName, pReturnedString, MAX_BUFFER, filePath);

            if ((bytesReturned == MAX_BUFFER - 2) || (bytesReturned == 0))
            {
                Marshal.FreeCoTaskMem(pReturnedString);
                return ret;
            }

            //bytesReturned -1 to remove trailing \0

            // NOTE: Calling Marshal.PtrToStringAuto(pReturnedString) will
            //       result in only the first pair being returned
            string returnedString = Marshal.PtrToStringAuto(pReturnedString, (int)bytesReturned - 1);

            Marshal.FreeCoTaskMem(pReturnedString);

            foreach (string value in returnedString.Split('\0'))
            {
                string[] valueKey = value.Split('=');

                ret.Add(valueKey[0], valueKey[1]);
            }

            return ret;
        }
    }
}
