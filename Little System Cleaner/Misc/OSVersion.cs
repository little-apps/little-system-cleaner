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
using System.Runtime.InteropServices;

namespace Little_System_Cleaner
{
    internal static class OSVersion
    {
        #region PInvoke Signatures
        public const byte VER_NT_WORKSTATION = 1;
        public const byte VER_NT_DOMAIN_CONTROLLER = 2;
        public const byte VER_NT_SERVER = 3;

        public const ushort VER_SUITE_SMALLBUSINESS = 1;
        public const ushort VER_SUITE_ENTERPRISE = 2;
        public const ushort VER_SUITE_TERMINAL = 16;
        public const ushort VER_SUITE_DATACENTER = 128;
        public const ushort VER_SUITE_SINGLEUSERTS = 256;
        public const ushort VER_SUITE_PERSONAL = 512;
        public const ushort VER_SUITE_BLADE = 1024;
        public const ushort VER_SUITE_WH_SERVER = 32768;

        public const uint PRODUCT_UNDEFINED = 0x00000000;
        public const uint PRODUCT_ULTIMATE = 0x00000001;
        public const uint PRODUCT_HOME_BASIC = 0x00000002;
        public const uint PRODUCT_HOME_PREMIUM = 0x00000003;
        public const uint PRODUCT_ENTERPRISE = 0x00000004;
        public const uint PRODUCT_HOME_BASIC_N = 0x00000005;
        public const uint PRODUCT_BUSINESS = 0x00000006;
        public const uint PRODUCT_STANDARD_SERVER = 0x00000007;
        public const uint PRODUCT_DATACENTER_SERVER = 0x00000008;
        public const uint PRODUCT_SMALLBUSINESS_SERVER = 0x00000009;
        public const uint PRODUCT_ENTERPRISE_SERVER = 0x0000000A;
        public const uint PRODUCT_STARTER = 0x0000000B;
        public const uint PRODUCT_DATACENTER_SERVER_CORE = 0x0000000C;
        public const uint PRODUCT_STANDARD_SERVER_CORE = 0x0000000D;
        public const uint PRODUCT_ENTERPRISE_SERVER_CORE = 0x0000000E;
        public const uint PRODUCT_ENTERPRISE_SERVER_IA64 = 0x0000000F;
        public const uint PRODUCT_BUSINESS_N = 0x00000010;
        public const uint PRODUCT_WEB_SERVER = 0x00000011;
        public const uint PRODUCT_CLUSTER_SERVER = 0x00000012;
        public const uint PRODUCT_HOME_SERVER = 0x00000013;
        public const uint PRODUCT_STORAGE_EXPRESS_SERVER = 0x00000014;
        public const uint PRODUCT_STORAGE_STANDARD_SERVER = 0x00000015;
        public const uint PRODUCT_STORAGE_WORKGROUP_SERVER = 0x00000016;
        public const uint PRODUCT_STORAGE_ENTERPRISE_SERVER = 0x00000017;
        public const uint PRODUCT_SERVER_FOR_SMALLBUSINESS = 0x00000018;
        public const uint PRODUCT_SMALLBUSINESS_SERVER_PREMIUM = 0x00000019;
        public const uint PRODUCT_HOME_PREMIUM_N = 0x0000001A;
        public const uint PRODUCT_ENTERPRISE_N = 0x0000001B;
        public const uint PRODUCT_ULTIMATE_N = 0x0000001C;
        public const uint PRODUCT_WEB_SERVER_CORE = 0x0000001D;
        public const uint PRODUCT_MEDIUMBUSINESS_SERVER_MANAGEMENT = 0x0000001E;
        public const uint PRODUCT_MEDIUMBUSINESS_SERVER_SECURITY = 0x0000001F;
        public const uint PRODUCT_MEDIUMBUSINESS_SERVER_MESSAGING = 0x00000020;
        public const uint PRODUCT_SERVER_FOR_SMALLBUSINESS_V = 0x00000023;
        public const uint PRODUCT_STANDARD_SERVER_V = 0x00000024;
        public const uint PRODUCT_ENTERPRISE_SERVER_V = 0x00000026;
        public const uint PRODUCT_STANDARD_SERVER_CORE_V = 0x00000028;
        public const uint PRODUCT_ENTERPRISE_SERVER_CORE_V = 0x00000029;
        public const uint PRODUCT_HYPERV = 0x0000002A;
        public const uint PRODUCT_PROFESSIONAL = 0x00000030;
        public const uint PRODUCT_PROFESSIONAL_N = 0x00000031;
        public const uint PRODUCT_STARTER_N = 0x0000002F;

        public const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;
        public const ushort PROCESSOR_ARCHITECTURE_IA64 = 6;
        public const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;
        public const ushort PROCESSOR_ARCHITECTURE_UNKNOWN = 0xFFFF;

        public const int SM_SERVERR2 = 89;

        [StructLayout(LayoutKind.Sequential)]
        public struct OSVERSIONINFOEX
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
        public struct SYSTEM_INFO
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

        [DllImport("Kernel32.dll")]
        internal static extern bool GetProductInfo(
           uint osMajorVersion,
           uint osMinorVersion,
           uint spMajorVersion,
           uint spMinorVersion,
           out uint edition);

        [DllImport("kernel32.dll")]
        internal static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);

        [DllImport("kernel32.dll")]
        internal static extern void GetSystemInfo(ref SYSTEM_INFO pSI);

        [DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(int nIndex);
        #endregion

        internal static string GetOSVersion()
        {
            OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX();
            osVersionInfo.dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(OSVERSIONINFOEX));

            SYSTEM_INFO systemInfo = new SYSTEM_INFO();
            GetSystemInfo(ref systemInfo);

            string osName = "Microsoft ";

            if (!GetVersionEx(ref osVersionInfo))
                return string.Empty;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32Windows:
                    {
                        switch (osVersionInfo.dwMajorVersion)
                        {
                            case 4:
                                {
                                    switch (osVersionInfo.dwMinorVersion)
                                    {
                                        case 0:
                                            if (osVersionInfo.szCSDVersion == "B" ||
                                                osVersionInfo.szCSDVersion == "C")
                                                osName += "Windows 95 R2";
                                            else
                                                osName += "Windows 95";
                                            break;
                                        case 10:
                                            if (osVersionInfo.szCSDVersion == "A")
                                                osName += "Windows 98 SE";
                                            else
                                                osName += "Windows 98";
                                            break;
                                        case 90:
                                            osName += "Windows ME";
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case PlatformID.Win32NT:
                    {
                        switch (osVersionInfo.dwMajorVersion)
                        {
                            case 3:
                                osName += "Windows NT 3.5.1";
                                break;

                            case 4:
                                switch (osVersionInfo.wProductType)
                                {
                                    case 1:
                                        osName += "Windows NT 4.0";
                                        break;
                                    case 3:
                                        osName += "Windows NT 4.0 Server";
                                        break;
                                }
                                break;

                            case 5:
                                {
                                    switch (osVersionInfo.dwMinorVersion)
                                    {
                                        case 0:
                                            osName += "Windows 2000";
                                            break;
                                        case 1:
                                            osName += "Windows XP";
                                            break;
                                        case 2:
                                            {
                                                if (osVersionInfo.wSuiteMask == VER_SUITE_WH_SERVER)
                                                    osName += "Windows Home Server";
                                                else if (osVersionInfo.wProductType == VER_NT_WORKSTATION &&
                                                        systemInfo.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64)
                                                    osName += "Windows XP Professional";
                                                else
                                                    osName += GetSystemMetrics(SM_SERVERR2) == 0 ? "Windows Server 2003" : "Windows Server 2003 R2";
                                            }
                                            break;
                                    }

                                }
                                break;

                            case 6:
                                {
                                    switch (osVersionInfo.dwMinorVersion)
                                    {
                                        case 0:
                                            osName += osVersionInfo.wProductType == VER_NT_WORKSTATION ? "Windows Vista" : "Windows Server 2008";
                                            break;

                                        case 1:
                                            osName += osVersionInfo.wProductType == VER_NT_WORKSTATION ? "Windows 7" : "Windows Server 2008 R2";
                                            break;

                                        case 2:
                                            osName += osVersionInfo.wProductType == VER_NT_WORKSTATION ? "Windows 8" : "Windows Server 2012";
                                            break;

                                        case 3:
                                            osName += osVersionInfo.wProductType == VER_NT_WORKSTATION ? "Windows 8.1" : "Windows Server 2012 R2";
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                    break;
            }

            osName += " ";

            switch (osVersionInfo.dwMajorVersion)
            {
                case 4:
                    {
                        switch (osVersionInfo.wProductType)
                        {
                            case VER_NT_WORKSTATION:
                                osName += "Workstation";
                                break;

                            case VER_NT_SERVER:
                                osName += (osVersionInfo.wSuiteMask & VER_SUITE_ENTERPRISE) != 0 ? "Enterprise Server" : "Standard Server";
                                break;
                        }
                    }
                    break;

                case 5:
                    {
                        switch (osVersionInfo.wProductType)
                        {
                            case VER_NT_WORKSTATION:
                                    osName += (osVersionInfo.wSuiteMask & VER_SUITE_PERSONAL) != 0 ?  "Home" : "Professional";
                                break;

                            case VER_NT_SERVER:
                                {
                                    switch (osVersionInfo.dwMinorVersion)
                                    {
                                        case 0:
                                            {
                                                if ((osVersionInfo.wSuiteMask & VER_SUITE_DATACENTER) != 0)
                                                    osName += "Data Center Server";
                                                else if ((osVersionInfo.wSuiteMask & VER_SUITE_ENTERPRISE) != 0)
                                                    osName += "Advanced Server";
                                                else
                                                    osName += "Server";
                                            }
                                            break;

                                        default:
                                            {
                                                if ((osVersionInfo.wSuiteMask & VER_SUITE_DATACENTER) != 0)
                                                    osName += "Data Center Server";
                                                else if ((osVersionInfo.wSuiteMask & VER_SUITE_ENTERPRISE) != 0)
                                                    osName += "Enterprise Server";
                                                else if ((osVersionInfo.wSuiteMask & VER_SUITE_BLADE) != 0)
                                                    osName += "Web Edition";
                                                else
                                                    osName += "Standard Server";
                                            }
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case 6:
                    {
                        uint ed;
                        if (GetProductInfo(osVersionInfo.dwMajorVersion, osVersionInfo.dwMinorVersion, osVersionInfo.wServicePackMajor, osVersionInfo.wServicePackMinor, out ed))
                        {
                            switch (ed)
                            {
                                case PRODUCT_BUSINESS:
                                    osName += "Business";
                                    break;
                                case PRODUCT_BUSINESS_N:
                                    osName += "Business N";
                                    break;
                                case PRODUCT_CLUSTER_SERVER:
                                    osName += "HPC Edition";
                                    break;
                                case PRODUCT_DATACENTER_SERVER:
                                    osName += "Data Center Server";
                                    break;
                                case PRODUCT_DATACENTER_SERVER_CORE:
                                    osName += "Data Center Server Core";
                                    break;
                                case PRODUCT_ENTERPRISE:
                                    osName += "Enterprise";
                                    break;
                                case PRODUCT_ENTERPRISE_N:
                                    osName += "Enterprise N";
                                    break;
                                case PRODUCT_ENTERPRISE_SERVER:
                                    osName += "Enterprise Server";
                                    break;
                                case PRODUCT_ENTERPRISE_SERVER_CORE:
                                    osName += "Enterprise Server Core Installation";
                                    break;
                                case PRODUCT_ENTERPRISE_SERVER_CORE_V:
                                    osName += "Enterprise Server Without Hyper-V Core Installation";
                                    break;
                                case PRODUCT_ENTERPRISE_SERVER_IA64:
                                    osName += "Enterprise Server For Itanium Based Systems";
                                    break;
                                case PRODUCT_ENTERPRISE_SERVER_V:
                                    osName += "Enterprise Server Without Hyper-V";
                                    break;
                                case PRODUCT_HOME_BASIC:
                                    osName += "Home Basic";
                                    break;
                                case PRODUCT_HOME_BASIC_N:
                                    osName += "Home Basic N";
                                    break;
                                case PRODUCT_HOME_PREMIUM:
                                    osName += "Home Premium";
                                    break;
                                case PRODUCT_HOME_PREMIUM_N:
                                    osName += "Home Premium N";
                                    break;
                                case PRODUCT_HYPERV:
                                    osName += "Hyper-V Server";
                                    break;
                                case PRODUCT_MEDIUMBUSINESS_SERVER_MANAGEMENT:
                                    osName += "Essential Business Management Server";
                                    break;
                                case PRODUCT_MEDIUMBUSINESS_SERVER_MESSAGING:
                                    osName += "Essential Business Messaging Server";
                                    break;
                                case PRODUCT_MEDIUMBUSINESS_SERVER_SECURITY:
                                    osName += "Essential Business Security Server";
                                    break;
                                case PRODUCT_SERVER_FOR_SMALLBUSINESS:
                                    osName += "Essential Server Solutions";
                                    break;
                                case PRODUCT_SERVER_FOR_SMALLBUSINESS_V:
                                    osName += "Essential Server Solutions Without Hyper-V";
                                    break;
                                case PRODUCT_SMALLBUSINESS_SERVER:
                                    osName += "Small Business Server";
                                    break;
                                case PRODUCT_STANDARD_SERVER:
                                    osName += "Standard Server";
                                    break;
                                case PRODUCT_STANDARD_SERVER_CORE:
                                    osName += "Standard Server Core Installation";
                                    break;
                                case PRODUCT_STANDARD_SERVER_CORE_V:
                                    osName += "Standard Server Without Hyper-V Core Installation";
                                    break;
                                case PRODUCT_STANDARD_SERVER_V:
                                    osName += "Standard Server Without Hyper-V";
                                    break;
                                case PRODUCT_STARTER:
                                    osName += "Starter";
                                    break;
                                case PRODUCT_STORAGE_ENTERPRISE_SERVER:
                                    osName += "Enterprise Storage Server";
                                    break;
                                case PRODUCT_STORAGE_EXPRESS_SERVER:
                                    osName += "Express Storage Server";
                                    break;
                                case PRODUCT_STORAGE_STANDARD_SERVER:
                                    osName += "Standard Storage Server";
                                    break;
                                case PRODUCT_STORAGE_WORKGROUP_SERVER:
                                    osName += "Workgroup Storage Server";
                                    break;
                                case PRODUCT_UNDEFINED:
                                    break;
                                case PRODUCT_ULTIMATE:
                                    osName += "Ultimate";
                                    break;
                                case PRODUCT_ULTIMATE_N:
                                    osName += "Ultimate N";
                                    break;
                                case PRODUCT_WEB_SERVER:
                                    osName += "Web Server";
                                    break;
                                case PRODUCT_WEB_SERVER_CORE:
                                    osName += "Web Server Core Installation";
                                    break;
                                case PRODUCT_PROFESSIONAL:
                                    osName += "Professional";
                                    break;
                                case PRODUCT_PROFESSIONAL_N:
                                    osName += "Professional N";
                                    break;
                                case PRODUCT_STARTER_N:
                                    osName += "Starter N";
                                    break;
                            }
                        }
                    }
                    break;
            }

            // If 64 bit OS -> Append (x64)
            if (Environment.Is64BitOperatingSystem)
                osName = osName.Trim() + " (x64)";
            else 
                // Otherwise (x86)
                osName = osName.Trim() + " (x86)";

            return osName;
        }
    }
}
