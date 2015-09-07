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
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LittleSoftwareStats.Hardware
{
    internal class WindowsHardware : Hardware
    {
#region P/Invoke signatures
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
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

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }


        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
#endregion

        public override string CpuBrand { get; } = "";

        readonly string _cpuName = "";
        public override string CpuName => _cpuName;

        public override int CpuArchitecture { get; }

        public override int CpuCores => Environment.ProcessorCount;

        public override double CpuFrequency => Convert.ToDouble(Utils.GetRegistryValue(Registry.LocalMachine, @"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "~MHz", 0));

        readonly long _diskFree;
        public override long DiskFree => _diskFree;

        readonly long _diskTotal;
        public override long DiskTotal => _diskTotal;

        public override double MemoryFree { get; }

        public override double MemoryTotal { get; }

        public override string ScreenResolution => Screen.PrimaryScreen.Bounds.Width + "x" + Screen.PrimaryScreen.Bounds.Height;

        public WindowsHardware()
        {
            // Get memory
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                // Convert from bytes -> megabytes
                MemoryTotal = memStatus.ullTotalPhys / 1024 / 1024;
                MemoryFree = memStatus.ullAvailPhys / 1024 / 1024;
            }

            // Get disk space
            foreach (DriveInfo di in DriveInfo.GetDrives())
            {
                if (di.IsReady && di.DriveType == DriveType.Fixed)
                {
                    _diskFree += di.TotalFreeSpace / 1024 / 1024;
                    _diskTotal += di.TotalSize / 1024 / 1024;
                }
            }

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name, Manufacturer, Architecture FROM Win32_Processor");

            foreach (var o in searcher.Get())
            {
                var sysItem = (ManagementObject) o;
                try
                {
                    _cpuName = sysItem["Name"].ToString();

                    if (!string.IsNullOrEmpty(_cpuName))
                    {
                        _cpuName = _cpuName.Replace("(TM)", "");
                        _cpuName = _cpuName.Replace("(R)", "");
                        _cpuName = _cpuName.Replace(" ", "");
                    }
                }
                catch
                {
                    _cpuName = "Unknown";
                }

                try
                {
                    CpuBrand = sysItem["Manufacturer"].ToString();
                }
                catch
                {
                    CpuBrand = "Unknown";
                }

                try
                {
                    int arch = Convert.ToInt32(sysItem["Architecture"].ToString());
                    if (arch == 6 || arch == 9)
                        CpuArchitecture = 64;
                    else
                        CpuArchitecture = 32;
                }
                catch
                {
                    CpuArchitecture = 32;
                }
            }
        }
    } 
}
