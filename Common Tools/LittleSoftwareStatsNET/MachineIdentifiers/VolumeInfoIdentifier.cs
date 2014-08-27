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
using System.Text;
using System.Runtime.InteropServices;

namespace LittleSoftwareStats.MachineIdentifiers
{
    public class VolumeInfoIdentifier : MachineIdentifierBase, IMachineIdentifier
    {
        [DllImport("kernel32.dll")]
        private static extern long GetVolumeInformation(string PathName, StringBuilder VolumeNameBuffer, UInt32 VolumeNameSize, ref UInt32 VolumeSerialNumber, ref UInt32 MaximumComponentLength, ref UInt32 FileSystemFlags, StringBuilder FileSystemNameBuffer, UInt32 FileSystemNameSize);

        protected override byte[] GetIdentifierHash()
        {
            string identifier = "NOTFOUND";

            if (Environment.OSVersion.Platform != PlatformID.MacOSX && Environment.OSVersion.Platform != PlatformID.Unix)
            {
                try
                {
                    uint serNum = 0;
                    uint maxCompLen = 0;
                    StringBuilder VolLabel = new StringBuilder(256);
                    UInt32 VolFlags = new UInt32();
                    StringBuilder FSName = new StringBuilder(256);
                    GetVolumeInformation(null, VolLabel, (UInt32)VolLabel.Capacity, ref serNum, ref maxCompLen, ref VolFlags, FSName, (UInt32)FSName.Capacity);
                    identifier = serNum.ToString();
                }
                catch { }
            }
            
            return base.ComputeHash(identifier);
        }
    }
}
