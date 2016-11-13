using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Little_System_Cleaner.Misc
{
    internal static class PInvoke
    {
        #region Functions

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(IntPtr tokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
            ref TokPriv1Luid newState,
            uint zero,
            IntPtr null1,
            IntPtr null2);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

        [DllImport("kernel32.dll")]
        internal static extern int SearchPath(string strPath, string strFileName, string strExtension,
            uint nBufferLength, StringBuilder strBuffer, string strFilePart);

        [DllImport("kernel32.dll")]
        internal static extern DriveType GetDriveType([MarshalAs(UnmanagedType.LPStr)] string lpRootPathName);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport("Kernel32.dll")]
        internal static extern bool GetProductInfo(
            uint osMajorVersion,
            uint osMinorVersion,
            uint spMajorVersion,
            uint spMinorVersion,
            out uint edition);

        [DllImport("kernel32.dll")]
        internal static extern void GetSystemInfo(ref SystemInfo pSi);

        [DllImport("kernel32.dll")]
        internal static extern bool GetVersionEx(ref OsVersionInfoEx osVersionInfo);

        [DllImport("shell32.dll")]
        internal static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder,
            bool fCreate);

        [DllImport("shell32.dll", EntryPoint = "FindExecutable")]
        internal static extern long FindExecutableA(string lpFile, string lpDirectory, StringBuilder lpResult);

        [DllImport("shell32.dll", EntryPoint = "ExtractIconEx")]
        internal static extern int ExtractIconExA(string lpszFile, int nIconIndex, ref IntPtr phiconLarge,
            ref IntPtr phiconSmall, int nIcons);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        internal static extern int SHFileOperation(ref ShFileOpStruct fileOp);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr PathGetArgs(string path);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void PathRemoveArgs([In, Out] StringBuilder path);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int PathParseIconLocation([In, Out] StringBuilder path);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern void PathUnquoteSpaces([In, Out] StringBuilder path);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool PathFileExists(string path);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool PathStripToRoot([In, Out] StringBuilder path);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool PathRemoveFileSpec([In, Out] StringBuilder path);

        [DllImport("user32.dll")]
        internal static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(int smIndex);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        internal static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width,
            int height, uint flags);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        #endregion Functions

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        internal struct OsVersionInfoEx
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;

            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SystemInfo
        {
            public uint wProcessorArchitecture;
            public uint wReserved;
            public uint dwPageSize;
            public uint lpMinimumApplicationAddress;
            public uint lpMaximumApplicationAddress;
            public uint dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public uint dwProcessorLevel;
            public uint dwProcessorRevision;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class MemoryStatusEx
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MemoryStatusEx()
            {
                dwLength = (uint)Marshal.SizeOf(this);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct Win32FindDataWide
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
        public struct ShFileOpStruct
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

        #endregion Structures

        #region Enumerations

        [Flags]
        internal enum SlGpFlags
        {
            /// <summary>Retrieves the standard short (8.3 format) file name</summary>
            SlGpShortpath = 0x1,

            /// <summary>Retrieves the Universal Naming Convention (UNC) path name of the file</summary>
            SlGpUncpriority = 0x2,

            /// <summary>
            ///     Retrieves the raw path name. A raw path is something that might not exist and may include environment
            ///     variables that need to be expanded
            /// </summary>
            SlGpRawpath = 0x4
        }

        [Flags]
        internal enum SlrFlags
        {
            /// <summary>
            ///     Do not display a dialog box if the link cannot be resolved. When SlrNoUi is set,
            ///     the high-order word of fFlags can be set to a time-out value that specifies the
            ///     maximum amount of time to be spent resolving the link. The function returns if the
            ///     link cannot be resolved within the time-out duration. If the high-order word is set
            ///     to zero, the time-out duration will be set to the default value of 3,000 milliseconds
            ///     (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
            ///     duration, in milliseconds.
            /// </summary>
            SlrNoUi = 0x1,

            /// <summary>Obsolete and no longer used</summary>
            SlrAnyMatch = 0x2,

            /// <summary>
            ///     If the link object has changed, update its path and list of identifiers.
            ///     If SlrUpdate is set, you do not need to call IPersistFile::IsDirty to determine
            ///     whether or not the link object has changed.
            /// </summary>
            SlrUpdate = 0x4,

            /// <summary>Do not update the link information</summary>
            SlrNoupdate = 0x8,

            /// <summary>Do not execute the search heuristics</summary>
            SlrNosearch = 0x10,

            /// <summary>Do not use distributed link tracking</summary>
            SlrNotrack = 0x20,

            /// <summary>
            ///     Disable distributed link tracking. By default, distributed link tracking tracks
            ///     removable media across multiple devices based on the volume name. It also uses the
            ///     Universal Naming Convention (UNC) path to track remote file systems whose drive letter
            ///     has changed. Setting SlrNolinkinfo disables both types of tracking.
            /// </summary>
            SlrNolinkinfo = 0x40,

            /// <summary>Call the Microsoft Windows Installer</summary>
            SlrInvokeMsi = 0x80
        }

        #endregion Enumerations

        #region Definitions

        // GetVersionEx
        internal const byte VER_NT_WORKSTATION = 1;

        internal const byte VER_NT_DOMAIN_CONTROLLER = 2;
        internal const byte VER_NT_SERVER = 3;

        internal const ushort VER_SUITE_SMALLBUSINESS = 1;
        internal const ushort VER_SUITE_ENTERPRISE = 2;
        internal const ushort VER_SUITE_TERMINAL = 16;
        internal const ushort VER_SUITE_DATACENTER = 128;
        internal const ushort VER_SUITE_SINGLEUSERTS = 256;
        internal const ushort VER_SUITE_PERSONAL = 512;
        internal const ushort VER_SUITE_BLADE = 1024;
        internal const ushort VER_SUITE_WH_SERVER = 32768;

        internal const uint PRODUCT_UNDEFINED = 0x00000000;
        internal const uint PRODUCT_ULTIMATE = 0x00000001;
        internal const uint PRODUCT_HOME_BASIC = 0x00000002;
        internal const uint PRODUCT_HOME_PREMIUM = 0x00000003;
        internal const uint PRODUCT_ENTERPRISE = 0x00000004;
        internal const uint PRODUCT_HOME_BASIC_N = 0x00000005;
        internal const uint PRODUCT_BUSINESS = 0x00000006;
        internal const uint PRODUCT_STANDARD_SERVER = 0x00000007;
        internal const uint PRODUCT_DATACENTER_SERVER = 0x00000008;
        internal const uint PRODUCT_SMALLBUSINESS_SERVER = 0x00000009;
        internal const uint PRODUCT_ENTERPRISE_SERVER = 0x0000000A;
        internal const uint PRODUCT_STARTER = 0x0000000B;
        internal const uint PRODUCT_DATACENTER_SERVER_CORE = 0x0000000C;
        internal const uint PRODUCT_STANDARD_SERVER_CORE = 0x0000000D;
        internal const uint PRODUCT_ENTERPRISE_SERVER_CORE = 0x0000000E;
        internal const uint PRODUCT_ENTERPRISE_SERVER_IA64 = 0x0000000F;
        internal const uint PRODUCT_BUSINESS_N = 0x00000010;
        internal const uint PRODUCT_WEB_SERVER = 0x00000011;
        internal const uint PRODUCT_CLUSTER_SERVER = 0x00000012;
        internal const uint PRODUCT_HOME_SERVER = 0x00000013;
        internal const uint PRODUCT_STORAGE_EXPRESS_SERVER = 0x00000014;
        internal const uint PRODUCT_STORAGE_STANDARD_SERVER = 0x00000015;
        internal const uint PRODUCT_STORAGE_WORKGROUP_SERVER = 0x00000016;
        internal const uint PRODUCT_STORAGE_ENTERPRISE_SERVER = 0x00000017;
        internal const uint PRODUCT_SERVER_FOR_SMALLBUSINESS = 0x00000018;
        internal const uint PRODUCT_SMALLBUSINESS_SERVER_PREMIUM = 0x00000019;
        internal const uint PRODUCT_HOME_PREMIUM_N = 0x0000001A;
        internal const uint PRODUCT_ENTERPRISE_N = 0x0000001B;
        internal const uint PRODUCT_ULTIMATE_N = 0x0000001C;
        internal const uint PRODUCT_WEB_SERVER_CORE = 0x0000001D;
        internal const uint PRODUCT_MEDIUMBUSINESS_SERVER_MANAGEMENT = 0x0000001E;
        internal const uint PRODUCT_MEDIUMBUSINESS_SERVER_SECURITY = 0x0000001F;
        internal const uint PRODUCT_MEDIUMBUSINESS_SERVER_MESSAGING = 0x00000020;
        internal const uint PRODUCT_SERVER_FOR_SMALLBUSINESS_V = 0x00000023;
        internal const uint PRODUCT_STANDARD_SERVER_V = 0x00000024;
        internal const uint PRODUCT_ENTERPRISE_SERVER_V = 0x00000026;
        internal const uint PRODUCT_STANDARD_SERVER_CORE_V = 0x00000028;
        internal const uint PRODUCT_ENTERPRISE_SERVER_CORE_V = 0x00000029;
        internal const uint PRODUCT_HYPERV = 0x0000002A;
        internal const uint PRODUCT_PROFESSIONAL = 0x00000030;
        internal const uint PRODUCT_PROFESSIONAL_N = 0x00000031;
        internal const uint PRODUCT_STARTER_N = 0x0000002F;

        internal const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;
        internal const ushort PROCESSOR_ARCHITECTURE_IA64 = 6;
        internal const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;
        internal const ushort PROCESSOR_ARCHITECTURE_UNKNOWN = 0xFFFF;

        internal const int SM_SERVERR2 = 89;

        // AdjustTokenPrivileges
        internal const int SePrivilegeEnabled = 0x00000002;

        internal const int SePrivilegeRemoved = 0x00000004;
        internal const int TokenQuery = 0x00000008;
        internal const int TokenAdjustPrivileges = 0x00000020;

        // SHGetSpecialFolderPath
        internal const int CsidlStartup = 0x0007; // All Users\Startup

        internal const int CsidlCommonStartup = 0x0018; // Common Users\Startup
        internal const int CsidlPrograms = 0x0002; // All Users\Start Menu\Programs
        internal const int CsidlCommonPrograms = 0x0017; // Start Menu\Programs

        internal const int MaxPath = 260;
        internal const uint StgmRead = 0;

        // SHFileOperation
        internal const int FoDelete = 3;

        internal const int FofAllowundo = 0x40;
        internal const int FofNoconfirmation = 0x10;

        // GetWindowLong + SetWindowLong
        internal const int GwlStyle = -16;

        internal const int WsSysmenu = 0x80000;
        internal const int GwlExstyle = -20;
        internal const int WsExDlgmodalframe = 0x0001;

        // SetWindowPos
        internal const int SwpNosize = 0x0001;

        internal const int SwpNomove = 0x0002;
        internal const int SwpNozorder = 0x0004;
        internal const int SwpFramechanged = 0x0020;

        #endregion Definitions

        #region Classes/Interfaces

        /// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLinkW
        {
            /// <summary>Retrieves the path and file name of a Shell link object</summary>
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath,
                out Win32FindDataWide pfd, SlGpFlags fFlags);

            /// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
            void GetIDList(out IntPtr ppidl);

            /// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
            void SetIDList(IntPtr pidl);

            /// <summary>Retrieves the description string for a Shell link object</summary>
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

            /// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

            /// <summary>Retrieves the name of the working directory for a Shell link object</summary>
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

            /// <summary>Sets the name of the working directory for a Shell link object</summary>
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

            /// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

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
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
                int cchIconPath, out int piIcon);

            /// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

            /// <summary>Sets the relative path to the Shell link object</summary>
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

            /// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
            void Resolve(IntPtr hwnd, SlrFlags fFlags);

            /// <summary>Sets the path and file name of a Shell link object</summary>
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport, Guid("0000010c-0000-0000-c000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersist
        {
            [PreserveSig]
            void GetClassID(out Guid pClassId);
        }

        [ComImport, Guid("0000010b-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersistFile : IPersist
        {
            new void GetClassID(out Guid pClassId);

            [PreserveSig]
            int IsDirty();

            [PreserveSig]
            void Load([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

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

        #endregion Classes/Interfaces
    }
}