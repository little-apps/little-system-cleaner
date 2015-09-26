using System;
using System.Collections.Generic;
using System.Linq;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class CRC32 : System.Security.Cryptography.HashAlgorithm
    {
        private const uint Polynomial = 0xedb88320u;
        private const uint Seed = 0xffffffffu;

        readonly uint[] _table;
        private uint _crc;

        public CRC32()
        {
            _table = BuildTable();
        }

        private static uint[] BuildTable()
        {
            return Enumerable.Range(0, 256).Select(i =>
            {
                uint entry = (uint)i;
                Enumerable.Range(0, 8).ToList().ForEach(j =>
                {
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ Polynomial;
                    else
                        entry = entry >> 1;
                });

                return entry;
            }).ToArray();
        }

        public override void Initialize()
        {
            _crc = Seed;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _crc = CalculateHash(_crc, array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            var hashBuffer = UIntToBigEndianBytes(~_crc);
            HashValue = hashBuffer;
            return hashBuffer;
        }

        public override int HashSize => 32;

        public static uint Compute(byte[] buffer)
        {
            return Compute(Seed, buffer);
        }

        public static uint Compute(uint seed, byte[] buffer)
        {
            return Compute(Polynomial, seed, buffer);
        }

        public static uint Compute(uint polynomial, uint seed, byte[] buffer)
        {
            var table = BuildTable();
            return ~CalculateHash(table, seed, buffer, 0, buffer.Length);
        }

        private uint CalculateHash(uint seed, IList<byte> buffer, int start, int size)
        {
            return CalculateHash(_table, seed, buffer, start, size);
        }

        private static uint CalculateHash(uint[] table, uint seed, IList<byte> buffer, int start, int size)
        {
            var crc = seed;
            for (var i = start; i < size - start; i++)
            {
                byte b = buffer[i];
                crc = (crc >> 8) ^ table[b ^ crc & 0xff];
            }

            return crc;
        }

        private static byte[] UIntToBigEndianBytes(uint val)
        {
            var result = BitConverter.GetBytes(val);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }
    }
}
