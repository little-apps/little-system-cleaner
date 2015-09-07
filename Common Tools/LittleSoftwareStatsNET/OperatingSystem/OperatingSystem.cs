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
using System.Threading;

namespace LittleSoftwareStats.OperatingSystem
{
    internal abstract class OperatingSystem
    {
        abstract public Version FrameworkVersion { get; }
        abstract public int FrameworkSP { get; }
        abstract public Version JavaVersion { get; }

        abstract public int Architecture { get; }
        abstract public string Version { get; }
        abstract public int ServicePack { get; }

        abstract public Hardware.Hardware Hardware { get; }

        public int Lcid
        {
            get
            {
                try
                {
                    return Thread.CurrentThread.CurrentCulture.LCID;
                }
                catch
                {
                    // Just return 1033 (English - USA)
                    return 1033;
                }
            }
        }

        public static OperatingSystem GetOperatingSystemInfo()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return new UnixOperatingSystem();
            else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
                return new MacOsxOperatingSystem();
            else
                return new WindowsOperatingSystem();
        }
    }
}
