/*
    Little System Cleaner
    Copyright (C) 2008 Little Apps (http://www.little-apps.com/)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonTools
{
    public class ParseGif
    {
        List<int> Delays = new List<int>();

        public List<int> ParseGifDataStream(byte[] gifData, int offset)
        {
            Delays.Clear();
            offset = ParseHeader(ref gifData, offset);
            offset = ParseLogicalScreen(ref gifData, offset);
            while (offset != -1)
            {
                offset = ParseBlock(ref gifData, offset);
            }
            return Delays;
        }

        private int ParseHeader(ref byte[] gifData, int offset)
        {
            string str = System.Text.ASCIIEncoding.ASCII.GetString(gifData, offset, 3);
            if (str != "GIF")
            {
                throw new FormatException("Not a proper GIF file: missing GIF header");
            }
            return 6;
        }

        private int ParseLogicalScreen(ref byte[] gifData, int offset)
        {
            int _logicalWidth = BitConverter.ToUInt16(gifData, offset);
            int _logicalHeight = BitConverter.ToUInt16(gifData, offset + 2);

            byte packedField = gifData[offset + 4];
            bool hasGlobalColorTable = (int)(packedField & 0x80) > 0 ? true : false;

            int currentIndex = offset + 7;
            if (hasGlobalColorTable)
            {
                int colorTableLength = packedField & 0x07;
                colorTableLength = (int)Math.Pow(2, colorTableLength + 1) * 3;
                currentIndex = currentIndex + colorTableLength;
            }
            return currentIndex;
        }

        private int ParseBlock(ref byte[] gifData, int offset)
        {
            switch (gifData[offset])
            {
                case 0x21:
                    if (gifData[offset + 1] == 0xF9)
                    {
                        return ParseGraphicControlExtension(ref gifData, offset);
                    }
                    else
                    {
                        return ParseExtensionBlock(ref gifData, offset);
                    }
                case 0x2C:
                    offset = ParseGraphicBlock(ref gifData, offset);
                    return offset;
                case 0x3B:
                    return -1;
                default:
                    throw new FormatException("GIF format incorrect: missing graphic block or special-purpose block. ");
            }
        }

        private int ParseGraphicControlExtension(ref byte[] gifData, int offset)
        {
            int returnOffset = offset;
            // Extension Block
            int length = gifData[offset + 2];
            returnOffset = offset + length + 2 + 1;

            byte packedField = gifData[offset + 3];

            // Get DelayTime
            int delay = BitConverter.ToUInt16(gifData, offset + 4);
            int delayTime = (delay < 10) ? 10 : delay;
            Delays.Add(delayTime);
            while (gifData[returnOffset] != 0x00)
            {
                returnOffset = returnOffset + gifData[returnOffset] + 1;
            }

            returnOffset++;

            return returnOffset;
        }

        private int ParseExtensionBlock(ref byte[] gifData, int offset)
        {
            int returnOffset = offset;
            // Extension Block
            int length = gifData[offset + 2];
            returnOffset = offset + length + 2 + 1;
            // check if netscape continousLoop extension
            if (gifData[offset + 1] == 0xFF && length > 10)
            {
                string netscape = System.Text.ASCIIEncoding.ASCII.GetString(gifData, offset + 3, 8);
                if (netscape == "NETSCAPE")
                {
                    int _numberOfLoops = BitConverter.ToUInt16(gifData, offset + 16);
                    if (_numberOfLoops > 0)
                    {
                        _numberOfLoops++;
                    }
                }
            }
            while (gifData[returnOffset] != 0x00)
            {
                returnOffset = returnOffset + gifData[returnOffset] + 1;
            }

            returnOffset++;

            return returnOffset;
        }

        private int ParseGraphicBlock(ref byte[] gifData, int offset)
        {
            byte packedField = gifData[offset + 9];
            bool hasLocalColorTable = (int)(packedField & 0x80) > 0 ? true : false;

            int currentIndex = offset + 9;
            if (hasLocalColorTable)
            {
                int colorTableLength = packedField & 0x07;
                colorTableLength = (int)Math.Pow(2, colorTableLength + 1) * 3;
                currentIndex = currentIndex + colorTableLength;
            }
            currentIndex++; // Skip 0x00

            currentIndex++; // Skip LZW Minimum Code Size;

            while (gifData[currentIndex] != 0x00)
            {
                int length = gifData[currentIndex];
                currentIndex = currentIndex + gifData[currentIndex];
                currentIndex++; // Skip initial size byte
            }
            currentIndex = currentIndex + 1;
            return currentIndex;
        }
    }
}
