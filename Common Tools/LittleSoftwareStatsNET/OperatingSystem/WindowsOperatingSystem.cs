/*
 * Little Software Stats - .NET Library
 * Copyright (C) 2008-2012 Little Apps (http://www.little-apps.org)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Runtime.InteropServices;
using LittleSoftwareStats.Hardware;
using Microsoft.Win32;

namespace LittleSoftwareStats.OperatingSystem
{
    internal class WindowsOperatingSystem : OperatingSystem
    {
#region P/Invoke Signatures
        public const byte VER_NT_WORKSTATION = 1;

        public const ushort VER_SUITE_WH_SERVER = 32768;

        public const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;
        public const ushort PROCESSOR_ARCHITECTURE_IA64 = 6;
        public const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;

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

        [DllImport("kernel32.dll")]
        internal static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);

        [DllImport("kernel32.dll")]
        internal static extern void GetSystemInfo(ref SYSTEM_INFO pSI);

        [DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(int nIndex);
#endregion

        Hardware.Hardware _hardware;
        public override Hardware.Hardware Hardware => _hardware ?? (_hardware = new WindowsHardware());

        public override Version FrameworkVersion { get; }

        public override int FrameworkSP { get; }

        public override Version JavaVersion { get; }

        public override sealed int Architecture
        {
            get
            {
                string arch = (string)Utils.GetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "PROCESSOR_ARCHITECTURE");

                switch (arch.ToLower())
                {
                    case "x86":
                        return 32;
                    case "amd64":
                    case "ia64":
                        return 64;
                }

                // Just use IntPtr size
                // (note: will always return 32 bit if process is not 64 bit)
                return (IntPtr.Size == 8) ? (64) : (32);
            }
        }

        private string _version;
        public override string Version => _version;

        private int _servicePack;
        public override int ServicePack => _servicePack;

        public WindowsOperatingSystem()
        {
            // Get OS Info
            GetOsInfo();

            // Get .NET Framework version + SP
            FrameworkVersion = new Version(); // 0.0
            FrameworkSP = 0;

            try
            {
                RegistryKey regNet = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\NET Framework Setup\NDP");

                if (regNet != null)
                {
                    if (regNet.OpenSubKey("v4") != null)
                    {
                        FrameworkVersion = new Version(4, 0);
                    }
                    else if (regNet.OpenSubKey("v3.5") != null)
                    {
                        FrameworkVersion = new Version(3, 5);
                        FrameworkSP = (int)regNet.GetValue("SP", 0);
                    }
                    else if (regNet.OpenSubKey("v3.0") != null)
                    {
                        FrameworkVersion = new Version(3, 0);
                        FrameworkSP = (int)regNet.GetValue("SP", 0);
                    }
                    else if (regNet.OpenSubKey("v2.0.50727") != null)
                    {
                        FrameworkVersion = new Version(2, 0, 50727);
                        FrameworkSP = (int)regNet.GetValue("SP", 0);
                    }
                    else if (regNet.OpenSubKey("v1.1.4322") != null)
                    {
                        FrameworkVersion = new Version(1, 1, 4322);
                        FrameworkSP = (int)regNet.GetValue("SP", 0);
                    }
                    else if (regNet.OpenSubKey("v1.0") != null)
                    {
                        FrameworkVersion = new Version(1, 0);
                        FrameworkSP = (int)regNet.GetValue("SP", 0);
                    }

                    regNet.Close();
                }
            }
            catch
            {
                // ignored
            }

            // Get Java version
            JavaVersion = new Version();

            try
            {
                string javaVersion;

                if (Architecture == 32)
                    javaVersion = (string)Utils.GetRegistryValue(Registry.LocalMachine, @"Software\JavaSoft\Java Runtime Environment", "CurrentVersion", "");
                else
                    javaVersion = (string)Utils.GetRegistryValue(Registry.LocalMachine, @"Software\Wow6432Node\JavaSoft\Java Runtime Environment", "CurrentVersion", "");

                JavaVersion = new Version(javaVersion);
            }
            catch
            {
                // ignored
            }
        }

        private void GetOsInfo()
        {
            OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX { dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(OSVERSIONINFOEX)) };

            if (!GetVersionEx(ref osVersionInfo))
            {
                _version = "Unknown";
                _servicePack = 0;
                return;
            }

            string osName = "";

            SYSTEM_INFO systemInfo = new SYSTEM_INFO();
            GetSystemInfo(ref systemInfo);

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
                                osName += "Windows NT 4.0";
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
                                                else if (osVersionInfo.wProductType == VER_NT_WORKSTATION && systemInfo.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64)
                                                    osName += "Windows XP";
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

            _version = osName;
            _servicePack = osVersionInfo.wServicePackMajor;
        }
    } 
}
