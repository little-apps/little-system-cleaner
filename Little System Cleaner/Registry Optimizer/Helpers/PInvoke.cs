using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Little_System_Cleaner.Registry_Optimizer.Helpers
{
    internal static class PInvoke
    {
        [DllImport("advapi32.dll", EntryPoint = "RegOpenKey", SetLastError = true)]
        public static extern int RegOpenKeyA(uint hKey, string lpSubKey, ref int phkResult);
        [DllImport("advapi32.dll", EntryPoint = "RegReplaceKey", SetLastError = true)]
        public static extern int RegReplaceKeyA(int hKey, string lpSubKey, string lpNewFile, string lpOldFile);
        [DllImport("advapi32.dll", EntryPoint = "RegSaveKey", SetLastError = true)]
        public static extern int RegSaveKeyA(int hKey, string lpFile, int lpSecurityAttributes);
        [DllImport("advapi32.dll")]
        public static extern int RegCloseKey(int hKey);
        [DllImport("advapi32.dll")]
        public static extern int RegFlushKey(int hKey);
        [DllImport("advapi32.dll")]
        public static extern int RegSaveKeyEx(IntPtr hKey, string lpFile, IntPtr lpSecurityAttributes, int Flags);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);
        [DllImport("shell32.dll")]
        public static extern bool IsUserAnAdmin();

        [DllImport("kernel32.dll")]
        public static extern uint QueryDosDevice([In, Optional] string lpDeviceName, [Out] StringBuilder lpTargetPath, [In] int ucchMax);

        // Shutdown reason codes
        public const uint MajorOperatingSystem = 0x00020000;
        public const uint MinorReconfig = 0x00000004;
        public const uint FlagPlanned = 0x80000000;

        public enum HKEY : uint
        {
            HKEY_CLASSES_ROOT = 0x80000000,
            HKEY_CURRENT_USER = 0x80000001,
            HKEY_LOCAL_MACHINE = 0x80000002,
            HKEY_USERS = 0x80000003,
            HKEY_PERFORMANCE_DATA = 0x80000004,
            HKEY_PERFORMANCE_TEXT = 0x80000050,
            HKEY_PERFORMANCE_NLSTEXT = 0x80000060,
            HKEY_CURRENT_CONFIG = 0x80000005,
        }

        private enum REGFORMAT : int
        {
            REG_STANDARD_FORMAT = 1,
            REG_LATEST_FORMAT = 2,
            REG_NO_COMPRESSION = 4,
        }
    }
}
