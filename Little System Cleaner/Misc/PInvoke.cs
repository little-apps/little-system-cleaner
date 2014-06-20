using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Little_System_Cleaner.Misc
{
    public static class PInvoke
    {
        #region Functions
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
           [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
           ref TokPriv1Luid NewState,
           UInt32 Zero,
           IntPtr Null1,
           IntPtr Null2);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

        [DllImport("kernel32.dll")]
        public static extern int SearchPath(string strPath, string strFileName, string strExtension, uint nBufferLength, StringBuilder strBuffer, string strFilePart);
        [DllImport("kernel32.dll")]
        public static extern DriveType GetDriveType([MarshalAs(UnmanagedType.LPStr)] string lpRootPathName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport("shell32.dll")]
        public static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder, bool fCreate);
        [DllImport("shell32.dll", EntryPoint = "FindExecutable")]
        public static extern long FindExecutableA(string lpFile, string lpDirectory, StringBuilder lpResult);
        [DllImport("shell32.dll", EntryPoint = "ExtractIconEx")]
        public static extern int ExtractIconExA(string lpszFile, int nIconIndex, ref IntPtr phiconLarge, ref IntPtr phiconSmall, int nIcons);
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        internal static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr PathGetArgs(string path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern void PathRemoveArgs([In, Out] StringBuilder path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int PathParseIconLocation([In, Out] StringBuilder path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern void PathUnquoteSpaces([In, Out] StringBuilder path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PathFileExists(string path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PathStripToRoot([In, Out] StringBuilder path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PathRemoveFileSpec([In, Out] StringBuilder path);

        [DllImport("user32.dll")]
        public static extern int DestroyIcon(IntPtr hIcon);
        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int smIndex);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(IntPtr hObject);
        #endregion

        #region Structures
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public long ftCreationTime;
            public long ftLastAccessTime;
            public long ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        internal struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.U4)]
            public int wFunc;
            public string pFrom;
            public string pTo;
            public short fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }
        #endregion

        #region Enumerations
        [Flags()]
        internal enum SLGP_FLAGS
        {
            /// <summary>Retrieves the standard short (8.3 format) file name</summary>
            SLGP_SHORTPATH = 0x1,
            /// <summary>Retrieves the Universal Naming Convention (UNC) path name of the file</summary>
            SLGP_UNCPRIORITY = 0x2,
            /// <summary>Retrieves the raw path name. A raw path is something that might not exist and may include environment variables that need to be expanded</summary>
            SLGP_RAWPATH = 0x4
        }

        [Flags()]
        internal enum SLR_FLAGS
        {
            /// <summary>
            /// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set,
            /// the high-order word of fFlags can be set to a time-out value that specifies the
            /// maximum amount of time to be spent resolving the link. The function returns if the
            /// link cannot be resolved within the time-out duration. If the high-order word is set
            /// to zero, the time-out duration will be set to the default value of 3,000 milliseconds
            /// (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
            /// duration, in milliseconds.
            /// </summary>
            SLR_NO_UI = 0x1,
            /// <summary>Obsolete and no longer used</summary>
            SLR_ANY_MATCH = 0x2,
            /// <summary>If the link object has changed, update its path and list of identifiers.
            /// If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine
            /// whether or not the link object has changed.</summary>
            SLR_UPDATE = 0x4,
            /// <summary>Do not update the link information</summary>
            SLR_NOUPDATE = 0x8,
            /// <summary>Do not execute the search heuristics</summary>
            SLR_NOSEARCH = 0x10,
            /// <summary>Do not use distributed link tracking</summary>
            SLR_NOTRACK = 0x20,
            /// <summary>Disable distributed link tracking. By default, distributed link tracking tracks
            /// removable media across multiple devices based on the volume name. It also uses the
            /// Universal Naming Convention (UNC) path to track remote file systems whose drive letter
            /// has changed. Setting SLR_NOLINKINFO disables both types of tracking.</summary>
            SLR_NOLINKINFO = 0x40,
            /// <summary>Call the Microsoft Windows Installer</summary>
            SLR_INVOKE_MSI = 0x80
        }
        #endregion

        #region Definitions
        // AdjustTokenPrivileges
        internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const int SE_PRIVILEGE_REMOVED = 0x00000004;
        internal const int TOKEN_QUERY = 0x00000008;
        internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

        // SHGetSpecialFolderPath
        public const int CSIDL_STARTUP = 0x0007; // All Users\Startup
        public const int CSIDL_COMMON_STARTUP = 0x0018; // Common Users\Startup
        public const int CSIDL_PROGRAMS = 0x0002;   // All Users\Start Menu\Programs
        public const int CSIDL_COMMON_PROGRAMS = 0x0017;   // Start Menu\Programs

        internal const int MAX_PATH = 260;
        internal const uint STGM_READ = 0;

        // SHFileOperation
        internal const int FO_DELETE = 3;
        internal const int FOF_ALLOWUNDO = 0x40;
        internal const int FOF_NOCONFIRMATION = 0x10;
        #endregion

        #region Classes/Interfaces
        /// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
        [ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLinkW
        {
            /// <summary>Retrieves the path and file name of a Shell link object</summary>
            void GetPath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, SLGP_FLAGS fFlags);
            /// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
            void GetIDList(out IntPtr ppidl);
            /// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
            void SetIDList(IntPtr pidl);
            /// <summary>Retrieves the description string for a Shell link object</summary>
            void GetDescription([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            /// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            /// <summary>Retrieves the name of the working directory for a Shell link object</summary>
            void GetWorkingDirectory([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            /// <summary>Sets the name of the working directory for a Shell link object</summary>
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            /// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
            void GetArguments([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            /// <summary>Sets the command-line arguments for a Shell link object</summary>
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            /// <summary>Retrieves the hot key for a Shell link object</summary>
            void GetHotkey(out short pwHotkey);
            /// <summary>Sets a hot key for a Shell link object</summary>
            void SetHotkey(short wHotkey);
            /// <summary>Retrieves the show command for a Shell link object</summary>
            void GetShowCmd(out int piShowCmd);
            /// <summary>Sets the show command for a Shell link object. The show command sets the initial show state of the window.</summary>
            void SetShowCmd(int iShowCmd);
            /// <summary>Retrieves the location (path and index) of the icon for a Shell link object</summary>
            void GetIconLocation([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
                int cchIconPath, out int piIcon);
            /// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            /// <summary>Sets the relative path to the Shell link object</summary>
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            /// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
            void Resolve(IntPtr hwnd, SLR_FLAGS fFlags);
            /// <summary>Sets the path and file name of a Shell link object</summary>
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);

        }

        [ComImport, Guid("0000010c-0000-0000-c000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersist
        {
            [PreserveSig]
            void GetClassID(out Guid pClassID);
        }


        [ComImport, Guid("0000010b-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersistFile : IPersist
        {
            new void GetClassID(out Guid pClassID);
            [PreserveSig]
            int IsDirty();

            [PreserveSig]
            void Load([In, MarshalAs(UnmanagedType.LPWStr)]
            string pszFileName, uint dwMode);

            [PreserveSig]
            void Save([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
                [In, MarshalAs(UnmanagedType.Bool)] bool fRemember);

            [PreserveSig]
            void SaveCompleted([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

            [PreserveSig]
            void GetCurFile([In, MarshalAs(UnmanagedType.LPWStr)] string ppszFileName);
        }

        // CLSID_ShellLink from ShlGuid.h 
        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        public class ShellLink
        {
        }
        #endregion

        #region Privacy Cleaner
        public static int SW_SHOW = 5;
        public static uint SEE_MASK_INVOKEIDLIST = 12;

        // No dialog box confirming the deletion of the objects will be displayed.
        internal const int SHERB_NOCONFIRMATION = 0x00000001;
        // No dialog box indicating the progress will be displayed.
        internal const int SHERB_NOPROGRESSUI = 0x00000002;
        // No sound will be played when the operation is complete.
        internal const int SHERB_NOSOUND = 0x00000004;

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

        [StructLayout(LayoutKind.Explicit)]
        public struct ExemptDeltaOrReserverd
        {
            [FieldOffset(0)]
            public UInt32 dwReserved;
            [FieldOffset(0)]
            public UInt32 dwExemptDelta;
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

        [StructLayout(LayoutKind.Sequential)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
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



        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern uint GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer, uint nSize, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        internal static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [DllImport("shell32.dll", SetLastError = true)]
        internal static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);
        [DllImport("shell32.dll", SetLastError = true)]
        internal static extern int SHEmptyRecycleBin(IntPtr hWnd, string pszRootPath, uint dwFlags);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "DeleteUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        public static extern bool DeleteUrlCacheEntry([MarshalAs(UnmanagedType.LPStr)] string lpszUrlName);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "UnlockUrlCacheEntryFileA", CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnlockUrlCacheEntryFile([MarshalAs(UnmanagedType.LPStr)] string lpszUrlName, uint dwReserved);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "FindFirstUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr FindFirstUrlCacheEntry([MarshalAs(UnmanagedType.LPStr)] string lpszUrlSearchPattern, IntPtr lpFirstCacheEntryInfo, ref int lpdwFirstCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "FindNextUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        public static extern bool FindNextUrlCacheEntry(IntPtr hFind, IntPtr lpNextCacheEntryInfo, ref int lpdwNextCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true)]
        public static extern long FindCloseUrlCache(IntPtr hEnumHandle);
        #endregion
    }
}
