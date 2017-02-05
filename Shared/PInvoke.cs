﻿using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Shared
{
    public static class PInvoke
    {
        #region Functions

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustTokenPrivileges(IntPtr tokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
            ref TokPriv1Luid newState,
            uint zero,
            IntPtr null1,
            IntPtr null2);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

        [DllImport("kernel32.dll")]
        public static extern int SearchPath(string strPath, string strFileName, string strExtension,
            uint nBufferLength, StringBuilder strBuffer, string strFilePart);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("shell32.dll")]
        public static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder,
            bool fCreate);

        [DllImport("shell32.dll", EntryPoint = "FindExecutable")]
        public static extern long FindExecutableA(string lpFile, string lpDirectory, StringBuilder lpResult);

        [DllImport("shell32.dll", EntryPoint = "ExtractIconEx")]
        public static extern int ExtractIconExA(string lpszFile, int nIconIndex, ref IntPtr phiconLarge,
            ref IntPtr phiconSmall, int nIcons);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern int SHFileOperation(ref ShFileOpStruct fileOp);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr PathGetArgs(string path);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern void PathRemoveArgs([In, Out] StringBuilder path);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern void PathUnquoteSpaces([In, Out] StringBuilder path);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PathFileExists(string path);

        [DllImport("user32.dll")]
        public static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width,
            int height, uint flags);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ExitWindowsEx(ShutdownFlags uFlags, ShutdownReasons dwReason);

        #endregion Functions

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct OsVersionInfoEx
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MemoryStatusEx
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
        public struct TokPriv1Luid
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
        public enum SlGpFlags
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
        public enum SlrFlags
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

        [Flags]
        public enum ShutdownFlags : uint
        {
            /// <summary>
            /// Beginning with Windows 8:  You can prepare the system for a faster startup by combining the HybridShutdown flag with the Shutdown flag. 
            /// </summary>
            HybridShutdown = 0x00400000,

            /// <summary>
            /// Shuts down all processes running in the logon session of the process that called the ExitWindowsEx function. Then it logs the user off.
            /// This flag can be used only by processes running in an interactive user's logon session.
            /// </summary>
            Logoff = 0,

            /// <summary>
            /// Shuts down the system and turns off the power. The system must support the power-off feature.
            /// The calling process must have the SE_SHUTDOWN_NAME privilege. For more information, see the following Remarks section.
            /// </summary>
            Poweroff = 0x00000008,

            /// <summary>
            /// Shuts down the system and then restarts the system.
            /// The calling process must have the SE_SHUTDOWN_NAME privilege. For more information, see the following Remarks section.
            /// </summary>
            Reboot = 0x00000002,

            /// <summary>
            /// Shuts down the system and then restarts it, as well as any applications that have been registered for restart using the RegisterApplicationRestart function. These application receive the WM_QUERYENDSESSION message with lParam set to the ENDSESSION_CLOSEAPP value.
            /// </summary>
            RestartApps = 0x00000040,

            /// <summary>
            /// Shuts down the system to a point at which it is safe to turn off the power. All file buffers have been flushed to disk, and all running processes have stopped.
            /// The calling process must have the SE_SHUTDOWN_NAME privilege. For more information, see the following Remarks section.
            /// Specifying this flag will not turn off the power even if the system supports the power-off feature. You must specify EWX_POWEROFF to do this.
            /// </summary>
            Shutdown = 0x00000001,

            /// <summary>
            /// This flag has no effect if terminal services is enabled. Otherwise, the system does not send the WM_QUERYENDSESSION message. This can cause applications to lose data. Therefore, you should only use this flag in an emergency.
            /// </summary>
            Force = 0x00000004,

            /// <summary>
            /// Forces processes to terminate if they do not respond to the WM_QUERYENDSESSION or WM_ENDSESSION message within the timeout interval.
            /// </summary>
            ForceIfHung = 0x00000010,
        }

        [Flags]
        public enum ShutdownReasons : uint
        {
            /// <summary>
			/// Application issue
			/// </summary>
            MajorApplication = 0x00040000,

            /// <summary>
			/// Hardware issue
			/// </summary>
            MajorHardware = 0x00010000,

            /// <summary>
			/// The InitiateSystemShutdown function was used instead of InitiateSystemShutdownEx
			/// </summary>
            MajorLegacyApi = 0x00070000,

            /// <summary>
			/// Operating system issue
			/// </summary>
            MajorOperatingsystem = 0x00020000,

            /// <summary>
			/// Other issue
			/// </summary>
            MajorOther = 0x00000000,

            /// <summary>
			/// Power failure
			/// </summary>
            MajorPower = 0x00060000,

            /// <summary>
			/// Software issue
			/// </summary>
            MajorSoftware = 0x00030000,

            /// <summary>
			/// System failure
			/// </summary>
            MajorSystem = 0x00050000,

            /// <summary>
			/// Blue screen crash event
			/// </summary>
            MinorBluescreen = 0x0000000F,

            /// <summary>
			/// Unplugged
			/// </summary>
            MinorCordUnplugged = 0x0000000b,

            /// <summary>
			/// Disk
			/// </summary>
            MinorDisk = 0x00000007,

            /// <summary>
			/// Environment
			/// </summary>
            MinorEnvironment = 0x0000000c,

            /// <summary>
			/// Driver
			/// </summary>
            MinorHardwareDriver = 0x0000000d,

            /// <summary>
			/// Hot fix
			/// </summary>
            MinorHotfix = 0x00000011,

            /// <summary>
			/// Hot fix uninstallation
			/// </summary>
            MinorHotfixUninstall = 0x00000017,

            /// <summary>
			/// Unresponsive
			/// </summary>
            MinorHung = 0x00000005,

            /// <summary>
			/// Installation
			/// </summary>
            MinorInstallation = 0x00000002,

            /// <summary>
			/// Maintenance
			/// </summary>
            MinorMaintenance = 0x00000001,

            /// <summary>
			/// MMC issue
			/// </summary>
            MinorMMC = 0x00000019,

            /// <summary>
			/// Network connectivity
			/// </summary>
            MinorNetworkConnectivity = 0x00000014,

            /// <summary>
			/// Network card
			/// </summary>
            MinorNetworkCard = 0x00000009,

            /// <summary>
			/// Other issue
			/// </summary>
            MinorOther = 0x00000000,

            /// <summary>
			/// Other driver event
			/// </summary>
            MinorOtherDriver = 0x0000000e,

            /// <summary>
			/// Power supply
			/// </summary>
            MinorPowerSupply = 0x0000000a,

            /// <summary>
			/// Processor
			/// </summary>
            MinorProcessor = 0x00000008,

            /// <summary>
			/// Reconfigure
			/// </summary>
            MinorReconfig = 0x00000004,

            /// <summary>
			/// Security issue
			/// </summary>
            MinorSecurity = 0x00000013,

            /// <summary>
			/// Security patch
			/// </summary>
            MinorSecurityFix = 0x00000012,

            /// <summary>
			/// Security patch uninstallation
			/// </summary>
            MinorSecurityFixUninstall = 0x00000018,

            /// <summary>
			/// Service pack
			/// </summary>
            MinorServicePack = 0x00000010,

            /// <summary>
			/// Service pack uninstallation
			/// </summary>
            MinorServicePackUninstall = 0x00000016,

            /// <summary>
			/// Terminal Services
			/// </summary>
            MinorTermsrv = 0x00000020,

            /// <summary>
			/// Unstable
			/// </summary>
            MinorUnstable = 0x00000006,

            /// <summary>
			/// Upgrade
			/// </summary>
            MinorUpgrade = 0x00000003,

            /// <summary>
			/// WMI issue
			/// </summary>
            MinorWmi = 0x00000015,

            /// <summary>
            /// The reason code is defined by the user. For more information, see Defining a Custom Reason Code.
            /// If this flag is not present, the reason code is defined by the system.
            /// </summary>
            FlagsUserDefined = 0x40000000,

            /// <summary>
            /// The shutdown was planned. The system generates a System State Data (SSD) file. This file contains system state information such as the processes, threads, memory usage, and configuration. 
            /// </summary>
            FlagsPlanned = 0x80000000,
        }

        #endregion Enumerations

        #region Definitions
        // AdjustTokenPrivileges
        public const int SePrivilegeEnabled = 0x00000002;

        public const int SePrivilegeRemoved = 0x00000004;
        public const int TokenQuery = 0x00000008;
        public const int TokenAdjustPrivileges = 0x00000020;

        // SHGetSpecialFolderPath
        public const int CsidlStartup = 0x0007; // All Users\Startup

        public const int CsidlCommonStartup = 0x0018; // Common Users\Startup
        public const int CsidlPrograms = 0x0002; // All Users\Start Menu\Programs
        public const int CsidlCommonPrograms = 0x0017; // Start Menu\Programs

        public const int MaxPath = 260;
        public const uint StgmRead = 0;

        // SHFileOperation
        public const int FoDelete = 3;

        public const int FofAllowundo = 0x40;
        public const int FofNoconfirmation = 0x10;

        // GetWindowLong + SetWindowLong
        public const int GwlStyle = -16;

        public const int WsSysmenu = 0x80000;
        public const int GwlExstyle = -20;
        public const int WsExDlgmodalframe = 0x0001;

        // SetWindowPos
        public const int SwpNosize = 0x0001;

        public const int SwpNomove = 0x0002;
        public const int SwpNozorder = 0x0004;
        public const int SwpFramechanged = 0x0020;

        #endregion Definitions

        #region Classes/Interfaces

        /// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
        public interface IShellLinkW
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