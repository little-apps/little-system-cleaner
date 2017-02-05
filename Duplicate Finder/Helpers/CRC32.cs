﻿using System;
using System.Collections.Generic;

namespace Duplicate_Finder.Helpers
{
    public class CRC32 : System.Security.Cryptography.HashAlgorithm
    {
        private const uint Polynomial = 0xedb88320u;
        private const uint Seed = 0xffffffffu;

        private readonly uint[] _table;
        private uint _crc;

        public CRC32()
        {
            _table = BuildTable();
        }

        public override int HashSize => 32;

        private static uint[] BuildTable()
        {
            var createTable = new uint[256];

            for (var i = 0; i < 256; i++)
            {
                var entry = (uint)i;

                for (var j = 0; j < 8; j++)
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ Polynomial;
                    else
                        entry = entry >> 1;

                createTable[i] = entry;
            }

            return createTable;
        }

        public override void Initialize()
        {
            _crc = Seed;
        }

        protected override void HashCore(byte[] array, int start, int size)
        {
            _crc = CalculateHash(_crc, array, start, size);
        }

        protected override byte[] HashFinal()
        {
            var hashBuffer = UIntToBigEndianBytes(~_crc);
            HashValue = hashBuffer;
            return hashBuffer;
        }

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

        // ReSharper disable once SuggestBaseTypeForParameter
        private static uint CalculateHash(uint[] table, uint seed, IList<byte> buffer, int start, int size)
        {
            var crc = seed;
            for (var i = start; i < size - start; i++)
            {
                var b = buffer[i];
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