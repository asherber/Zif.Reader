/**
 * Copyright 2015 Aaron Sherber
 * 
 * This file is part of Zif.Reader.
 *
 * Zif.Reader is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Zif.Reader is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Zif.Reader. If not, see <http://www.gnu.org/licenses/>.
 */

using MiscUtil.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zif
{
    public static class EndianBinaryReaderHelper
    {
        public static ulong[] ReadUInt64Array(this EndianBinaryReader rdr, int count)
        {
            var result = new ulong[count];
            for (int i = 0; i < count; ++i)
                result[i] = rdr.ReadUInt64();
            return result;
        }

        public static uint[] ReadUInt32Array(this EndianBinaryReader rdr, int count)
        {
            var result = new uint[count];
            for (int i = 0; i < count; ++i)
                result[i] = rdr.ReadUInt32();
            return result;
        }

        public static void Seek(this EndianBinaryReader rdr, ulong offset, SeekOrigin origin)
        {
            int multiples = (int)(offset / int.MaxValue);
            int remainder = (int)(offset % int.MaxValue);

            rdr.Seek(remainder, origin);
            for (int i = 0; i < multiples; ++i)
                rdr.Seek(int.MaxValue, SeekOrigin.Current);
        }

        public static byte[] ReadBytes(this EndianBinaryReader rdr, ulong count)
        {
            int multiples = (int)(count / int.MaxValue);
            int remainder = (int)(count % int.MaxValue);

            var result = new List<byte[]>(multiples + 1);

            result.Add(rdr.ReadBytes(remainder));
            for (int i = 0; i < multiples; ++i)
                result.Add(rdr.ReadBytes(int.MaxValue));

            return result.SelectMany(a => a).ToArray();
        }
    }
}
