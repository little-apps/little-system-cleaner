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

using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace LittleSoftwareStats.MachineIdentifiers
{
    abstract public class MachineIdentifierBase : IMachineIdentifier
    {
        private byte[] _identifierHash;

        virtual public byte[] IdentifierHash => _identifierHash ?? (_identifierHash = GetIdentifierHash());

        abstract protected byte[] GetIdentifierHash();

        virtual public bool Match(byte[] hash)
        {
            if (ReferenceEquals(IdentifierHash, hash))
                return true;

            if (IdentifierHash == null || hash == null)
                return false;

            if (IdentifierHash.Length != hash.Length)
                return false;

            return !IdentifierHash.Where((t, n) => t != hash[n]).Any();
        }

        protected byte[] ComputeHash(string value)
        {
            MD5 hasher = new MD5CryptoServiceProvider();
            byte[] hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(value));
            return hash;
        }
    }
}
