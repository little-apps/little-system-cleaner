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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LittleSoftwareStats.Hardware
{
    internal class MacOSXHardware : UnixHardware
    {
        public MacOSXHardware()
        {
        }

        public override string CPUName 
        {
            get { 
                try
                {
                    Regex regex = new Regex(@"Processor Name\s*:\\s*(?<processor>[\w\s\d\.]+)");
                    MatchCollection matches = regex.Matches(Utils.SystemProfilerCommandOutput);
                    return matches[0].Groups["processor"].Value;
                }
                catch { }

                return "Generic"; 
            }
        }

        public override int CPUArchitecture
        {
            get
            {
                Regex regex = new Regex(@"hw\.cpu64bit_capable\s*(:|=)\s*(?<capable>\d+)");
                MatchCollection matches = regex.Matches(Utils.SysctlCommandOutput);
                if (matches[0].Groups["cpus"].Value == "1")
                    return 64;
                return 32;
            }
        }

        public override int CPUCores
        {
            get 
            {
                Regex regex = new Regex(@"hw\.availcpu\s*(:|=)\s*(?<cpus>\d+)");
                MatchCollection matches = regex.Matches(Utils.SysctlCommandOutput);
                return int.Parse(matches[0].Groups["cpus"].Value);
            }
        }

        public override string CPUBrand 
        {
            get { return "GenuineIntel"; }
        }

        public override double CPUFrequency 
        {
            get
            {
                Regex regex = new Regex(@"hw\.cpufrequency\s*(:|=)\s*(?<cpu_frequency>\d+)");
                MatchCollection matches = regex.Matches(Utils.SysctlCommandOutput);

                // Convert from B -> MB
                return double.Parse(matches[0].Groups["cpu_frequency"].Value) / 1024 / 1024;
            }
        }

        public override double MemoryTotal
        {
            get 
            {
                Regex regex = new Regex(@"hw\.memsize\s*(:|=)\s*(?<memory>\d+)");
                MatchCollection matches = regex.Matches(Utils.SysctlCommandOutput);

                // Convert from B -> MB
                return double.Parse(matches[0].Groups["memory"].Value) / 1024 / 1024;
            }
        }
    }
}
