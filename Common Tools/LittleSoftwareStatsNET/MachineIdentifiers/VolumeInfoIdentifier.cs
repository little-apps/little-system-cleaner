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
    public class VolumeInfoIdentifier : MachineIdentifierBase
    {
        [DllImport("kernel32.dll")]
        private static extern long GetVolumeInformation(string pathName, StringBuilder volumeNameBuffer, UInt32 volumeNameSize, ref UInt32 volumeSerialNumber, ref UInt32 maximumComponentLength, ref UInt32 fileSystemFlags, StringBuilder fileSystemNameBuffer, UInt32 fileSystemNameSize);

        protected override byte[] GetIdentifierHash()
        {
            string identifier = "NOTFOUND";

            if (Environment.OSVersion.Platform != PlatformID.MacOSX && Environment.OSVersion.Platform != PlatformID.Unix)
            {
                try
                {
                    uint serNum = 0;
                    uint maxCompLen = 0;
                    StringBuilder volLabel = new StringBuilder(256);
                    UInt32 volFlags = new UInt32();
                    StringBuilder fsName = new StringBuilder(256);
                    GetVolumeInformation(null, volLabel, (UInt32)volLabel.Capacity, ref serNum, ref maxCompLen, ref volFlags, fsName, (UInt32)fsName.Capacity);
                    identifier = serNum.ToString();
                }
                catch
                {
                    // ignored
                }
            }
            
            return ComputeHash(identifier);
        }
    }
}
