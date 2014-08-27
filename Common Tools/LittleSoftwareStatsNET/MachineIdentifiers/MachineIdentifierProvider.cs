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
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace LittleSoftwareStats.MachineIdentifiers
{
    public class MachineIdentifierProvider : IMachineIdentifierProvider
    {
        public List<IMachineIdentifier> MachineIdentifiers { get; private set; }

        public MachineIdentifierProvider()
        {
            MachineIdentifiers = new List<IMachineIdentifier>();
        }
        public MachineIdentifierProvider(IMachineIdentifier[] machineIdentifiers)
            : this()
        {
            MachineIdentifiers = new List<IMachineIdentifier>(machineIdentifiers);
        }

        public bool Match(byte[] machineHash)
        {
            int matchs = 0;

            using (MemoryStream stream = new MemoryStream(machineHash))
            {
                byte[] hash = new byte[16];
                for (int n = 0; n < MachineIdentifiers.Count; n++)
                {
                    if (stream.Read(hash, 0, 16) != 16)
                        break;
                    if (MachineIdentifiers[n].Match(hash))
                    {
                        matchs++;
                    }
                }
            }
            return matchs > 0;
        }

        public string MachineHash
        {
            get
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    for (int n = 0; n < MachineIdentifiers.Count; n++)
                    {
                        stream.Write(MachineIdentifiers[n].IdentifierHash, 0, 16);
                    }

                    using (MD5 hasher = new MD5CryptoServiceProvider())
                    {
                        string hash = "";
                        
                        foreach (byte b in hasher.ComputeHash(stream.ToArray()))
                        {
                            hash += b.ToString("X2");
                        }

                        return hash;
                    }
                }
            }
        }
    }
}
