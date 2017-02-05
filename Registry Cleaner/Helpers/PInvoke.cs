using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Registry_Cleaner.Helpers
{
    internal static class PInvoke
    {
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

        [DllImport("kernel32.dll")]
        internal static extern bool GetVersionEx(ref OsVersionInfoEx osVersionInfo);

        [DllImport("kernel32.dll")]
        internal static extern void GetSystemInfo(ref SystemInfo pSi);

        [DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(int smIndex);

        [DllImport("Kernel32.dll")]
        internal static extern bool GetProductInfo(
            uint osMajorVersion,
            uint osMinorVersion,
            uint spMajorVersion,
            uint spMinorVersion,
            out uint edition);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool PathStripToRoot([In, Out] StringBuilder path);

        [DllImport("kernel32.dll")]
        internal static extern DriveType GetDriveType([MarshalAs(UnmanagedType.LPStr)] string lpRootPathName);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool PathFileExists(string path);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int PathParseIconLocation([In, Out] StringBuilder path);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool PathRemoveFileSpec([In, Out] StringBuilder path);
    }
}
