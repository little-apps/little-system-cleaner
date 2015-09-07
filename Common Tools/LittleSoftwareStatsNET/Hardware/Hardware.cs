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

namespace LittleSoftwareStats.Hardware
{
    internal abstract class Hardware
    {
        public abstract string CpuName { get; }
        public abstract int CpuArchitecture { get; }
        public abstract int CpuCores { get; }
        public abstract string CpuBrand { get; }
        public abstract double CpuFrequency { get; }

        public abstract double MemoryTotal { get; }
        public abstract double MemoryFree { get; }

        public abstract long DiskTotal { get; }
        public abstract long DiskFree { get; }

        public abstract string ScreenResolution { get; }
    }
}
