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
using System.Runtime.InteropServices;

namespace Registry_Cleaner.Helpers
{
    internal static class OsVersion
    {
        /// <summary>
        /// Gets the OS version as a name
        /// </summary>
        /// <remarks>TODO: Fix GetVersionEx from always returning 6.2 on Windows 8.1 and Windows 10</remarks>
        /// <returns>Name of OS version</returns>
        internal static string GetOsVersion()
        {
            var osVersionInfo = new PInvoke.OsVersionInfoEx
            {
                dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(PInvoke.OsVersionInfoEx))
            };

            if (!PInvoke.GetVersionEx(ref osVersionInfo))
                return string.Empty;

            var osName = GetOsName(osVersionInfo);
            var osEdition = GetOsEdition(osVersionInfo);

            var operatingSystem = (osName + " " + osEdition).Trim();

            // If 64 bit OS then append (x64), otherwise, append (x86)
            operatingSystem += " " + (Environment.Is64BitOperatingSystem ? "(x64)" : "(x86)");

            return operatingSystem;
        }

        /// <summary>
        /// Returns the name of the operating system
        /// </summary>
        /// <param name="osVersionInfo">OsVersionInfoEx from GetVersionEx() call</param>
        /// <returns>Operating system name (Microsoft Windows, Unix, etc)</returns>
        private static string GetOsName(PInvoke.OsVersionInfoEx osVersionInfo)
        {
            string osName;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.WinCE:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                    {
                        osName = "Microsoft Windows ";

                        switch (Environment.OSVersion.Version.Major)
                        {
                            case 3:
                                osName += "NT 3.5.1";
                                break;

                            case 4:
                                {
                                    if (Environment.OSVersion.Platform == PlatformID.Win32Windows)
                                    {
                                        switch (Environment.OSVersion.Version.Minor)
                                        {
                                            case 0:
                                                osName += osVersionInfo.szCSDVersion == "B" ||
                                                          osVersionInfo.szCSDVersion == "C"
                                                    ? "95 R2"
                                                    : "95";
                                                break;

                                            case 10:
                                                osName += osVersionInfo.szCSDVersion == "A" ? "98 SE" : "98";
                                                break;

                                            case 90:
                                                osName += "ME";
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        switch (osVersionInfo.wProductType)
                                        {
                                            case 1:
                                                osName += "NT 4.0";
                                                break;

                                            case 3:
                                                osName += "NT 4.0 Server";
                                                break;
                                        }
                                    }

                                    break;
                                }

                            // TODO: Use version helper functions if OS is Windows 2000 Pro/Server or greater (https://msdn.microsoft.com/en-us/library/windows/desktop/dn424972%28v=vs.85%29.aspx)
                            case 5:
                                {
                                    switch (Environment.OSVersion.Version.Minor)
                                    {
                                        case 0:
                                            osName += "2000";
                                            break;

                                        case 1:
                                            osName += "XP";
                                            break;

                                        case 2:
                                            {
                                                var systemInfo = new PInvoke.SystemInfo();
                                                PInvoke.GetSystemInfo(ref systemInfo);

                                                if (osVersionInfo.wSuiteMask == PInvoke.VER_SUITE_WH_SERVER)
                                                    osName += "Home Server";
                                                else if (osVersionInfo.wProductType == PInvoke.VER_NT_WORKSTATION &&
                                                         systemInfo.wProcessorArchitecture ==
                                                         PInvoke.PROCESSOR_ARCHITECTURE_AMD64)
                                                    osName += "XP Professional";
                                                else
                                                    osName += PInvoke.GetSystemMetrics(PInvoke.SM_SERVERR2) == 0
                                                        ? "Server 2003"
                                                        : "Server 2003 R2";

                                                break;
                                            }
                                    }

                                    break;
                                }
                            case 6:
                                {
                                    switch (Environment.OSVersion.Version.Minor)
                                    {
                                        case 0:
                                            osName += osVersionInfo.wProductType == PInvoke.VER_NT_WORKSTATION
                                                ? "Vista"
                                                : "Server 2008";
                                            break;

                                        case 1:
                                            osName += osVersionInfo.wProductType == PInvoke.VER_NT_WORKSTATION
                                                ? "7"
                                                : "Server 2008 R2";
                                            break;

                                        case 2:
                                            osName += osVersionInfo.wProductType == PInvoke.VER_NT_WORKSTATION
                                                ? "8"
                                                : "Server 2012";
                                            break;

                                        case 3:
                                            osName += osVersionInfo.wProductType == PInvoke.VER_NT_WORKSTATION
                                                ? "8.1"
                                                : "Server 2012 R2";
                                            break;

                                        case 4:
                                            // Windows 10 was originally v6.4
                                            osName += "10 (Technical Preview)";
                                            break;
                                    }

                                    break;
                                }
                            case 10:
                                {
                                    switch (Environment.OSVersion.Version.Minor)
                                    {
                                        case 0:
                                            osName += osVersionInfo.wProductType == PInvoke.VER_NT_WORKSTATION
                                                ? "10"
                                                : "Server 2016";
                                            break;
                                    }
                                    break;
                                }
                        }
                        break;
                    }

                default:
                    // Unix, MacOSX, Xbox, etc
                    osName = Environment.OSVersion.Platform.ToString();
                    break;
            }

            return osName.Trim();
        }

        /// <summary>
        /// Gets the edition of the operating system
        /// </summary>
        /// <param name="osVersionInfo">OsVersionInfoEx from GetVersionEx() call</param>
        /// <returns>Returns the operating system edition</returns>
        private static string GetOsEdition(PInvoke.OsVersionInfoEx osVersionInfo)
        {
            switch (Environment.OSVersion.Version.Major)
            {
                case 4:
                    {
                        switch (osVersionInfo.wProductType)
                        {
                            case PInvoke.VER_NT_WORKSTATION:
                                return "Workstation";

                            case PInvoke.VER_NT_SERVER:
                                return (osVersionInfo.wSuiteMask & PInvoke.VER_SUITE_ENTERPRISE) != 0
                                    ? "Enterprise Server"
                                    : "Standard Server";
                        }

                        break;
                    }

                case 5:
                    {
                        switch (osVersionInfo.wProductType)
                        {
                            case PInvoke.VER_NT_WORKSTATION:
                                return (osVersionInfo.wSuiteMask & PInvoke.VER_SUITE_PERSONAL) != 0
                                    ? "Home"
                                    : "Professional";

                            case PInvoke.VER_NT_SERVER:
                                {
                                    switch (osVersionInfo.dwMinorVersion)
                                    {
                                        case 0:
                                            {
                                                if ((osVersionInfo.wSuiteMask & PInvoke.VER_SUITE_DATACENTER) != 0)
                                                    return "Data Center Server";
                                                return (osVersionInfo.wSuiteMask & PInvoke.VER_SUITE_ENTERPRISE) != 0
                                                    ? "Advanced Server"
                                                    : "Server";
                                            }
                                        default:
                                            {
                                                if ((osVersionInfo.wSuiteMask & PInvoke.VER_SUITE_DATACENTER) != 0)
                                                    return "Data Center Server";
                                                if ((osVersionInfo.wSuiteMask & PInvoke.VER_SUITE_ENTERPRISE) != 0)
                                                    return "Enterprise Server";
                                                return (osVersionInfo.wSuiteMask & PInvoke.VER_SUITE_BLADE) != 0
                                                    ? "Web Edition"
                                                    : "Standard Server";
                                            }
                                    }
                                }
                        }

                        break;
                    }

                case 6:
                    {
                        uint ed;
                        if (PInvoke.GetProductInfo(osVersionInfo.dwMajorVersion, osVersionInfo.dwMinorVersion,
                            osVersionInfo.wServicePackMajor, osVersionInfo.wServicePackMinor, out ed))
                        {
                            switch (ed)
                            {
                                case PInvoke.PRODUCT_BUSINESS:
                                    return "Business";

                                case PInvoke.PRODUCT_BUSINESS_N:
                                    return "Business N";

                                case PInvoke.PRODUCT_CLUSTER_SERVER:
                                    return "HPC Edition";

                                case PInvoke.PRODUCT_DATACENTER_SERVER:
                                    return "Data Center Server";

                                case PInvoke.PRODUCT_DATACENTER_SERVER_CORE:
                                    return "Data Center Server Core";

                                case PInvoke.PRODUCT_ENTERPRISE:
                                    return "Enterprise";

                                case PInvoke.PRODUCT_ENTERPRISE_N:
                                    return "Enterprise N";

                                case PInvoke.PRODUCT_ENTERPRISE_SERVER:
                                    return "Enterprise Server";

                                case PInvoke.PRODUCT_ENTERPRISE_SERVER_CORE:
                                    return "Enterprise Server Core Installation";

                                case PInvoke.PRODUCT_ENTERPRISE_SERVER_CORE_V:
                                    return "Enterprise Server Without Hyper-V Core Installation";

                                case PInvoke.PRODUCT_ENTERPRISE_SERVER_IA64:
                                    return "Enterprise Server For Itanium Based Systems";

                                case PInvoke.PRODUCT_ENTERPRISE_SERVER_V:
                                    return "Enterprise Server Without Hyper-V";

                                case PInvoke.PRODUCT_HOME_BASIC:
                                    return "Home Basic";

                                case PInvoke.PRODUCT_HOME_BASIC_N:
                                    return "Home Basic N";

                                case PInvoke.PRODUCT_HOME_PREMIUM:
                                    return "Home Premium";

                                case PInvoke.PRODUCT_HOME_PREMIUM_N:
                                    return "Home Premium N";

                                case PInvoke.PRODUCT_HYPERV:
                                    return "Hyper-V Server";

                                case PInvoke.PRODUCT_MEDIUMBUSINESS_SERVER_MANAGEMENT:
                                    return "Essential Business Management Server";

                                case PInvoke.PRODUCT_MEDIUMBUSINESS_SERVER_MESSAGING:
                                    return "Essential Business Messaging Server";

                                case PInvoke.PRODUCT_MEDIUMBUSINESS_SERVER_SECURITY:
                                    return "Essential Business Security Server";

                                case PInvoke.PRODUCT_SERVER_FOR_SMALLBUSINESS:
                                    return "Essential Server Solutions";

                                case PInvoke.PRODUCT_SERVER_FOR_SMALLBUSINESS_V:
                                    return "Essential Server Solutions Without Hyper-V";

                                case PInvoke.PRODUCT_SMALLBUSINESS_SERVER:
                                    return "Small Business Server";

                                case PInvoke.PRODUCT_STANDARD_SERVER:
                                    return "Standard Server";

                                case PInvoke.PRODUCT_STANDARD_SERVER_CORE:
                                    return "Standard Server Core Installation";

                                case PInvoke.PRODUCT_STANDARD_SERVER_CORE_V:
                                    return "Standard Server Without Hyper-V Core Installation";

                                case PInvoke.PRODUCT_STANDARD_SERVER_V:
                                    return "Standard Server Without Hyper-V";

                                case PInvoke.PRODUCT_STARTER:
                                    return "Starter";

                                case PInvoke.PRODUCT_STORAGE_ENTERPRISE_SERVER:
                                    return "Enterprise Storage Server";

                                case PInvoke.PRODUCT_STORAGE_EXPRESS_SERVER:
                                    return "Express Storage Server";

                                case PInvoke.PRODUCT_STORAGE_STANDARD_SERVER:
                                    return "Standard Storage Server";

                                case PInvoke.PRODUCT_STORAGE_WORKGROUP_SERVER:
                                    return "Workgroup Storage Server";

                                case PInvoke.PRODUCT_ULTIMATE:
                                    return "Ultimate";

                                case PInvoke.PRODUCT_ULTIMATE_N:
                                    return "Ultimate N";

                                case PInvoke.PRODUCT_WEB_SERVER:
                                    return "Web Server";

                                case PInvoke.PRODUCT_WEB_SERVER_CORE:
                                    return "Web Server Core Installation";

                                case PInvoke.PRODUCT_PROFESSIONAL:
                                    return "Professional";

                                case PInvoke.PRODUCT_PROFESSIONAL_N:
                                    return "Professional N";

                                case PInvoke.PRODUCT_STARTER_N:
                                    return "Starter N";

                                default:
                                    return string.Empty;
                            }
                        }

                        break;
                    }
            }

            return string.Empty;
        }
    }
}