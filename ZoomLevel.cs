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

using MiscUtil.Conversion;
using MiscUtil.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zif
{
    public class ZoomLevel : IDisposable
    {
        private Dictionary<int, ulong[]> _map = new Dictionary<int, ulong[]>();
        private EndianBinaryReader _reader = null;
        private ulong[][] _allTileInfos = null;

        // This all looked nicer with expression-bodied properties, but I didn't want to require C#6
        public int Width { get { return (int)GetFromMap(0x0100, 1); } }                 // 256
        public int Height { get { return (int)GetFromMap(0x0101, 1); } }                // 257
        public int TileWidth { get { return (int)GetFromMap(0x0142, 1); } }             // 322
        public int TileHeight { get { return (int)GetFromMap(0x0143, 1); } }            // 323
        public int TileCount { get { return (int)GetFromMap(0x0144, 0); } }             // 324
        internal ulong PositionFileOffset { get { return GetFromMap(0x0144, 1); } }     // 324
        internal ulong SizeFileOffset { get { return GetFromMap(0x0145, 1); } }         // 325

        public Size Dimensions { get { return new Size(this.Width, this.Height); } }
        public Size TileDimensions { get { return new Size(this.TileWidth, this.TileHeight); } }
        public int WidthInTiles { get { return (int)Math.Ceiling(1.0 * this.Width / this.TileWidth); } }
        public int HeightInTiles { get { return (int)Math.Ceiling(1.0 * this.Height / this.TileHeight); } }

        internal ZoomLevel(EndianBinaryReader reader)
        {
            _reader = reader;
        }

        private ulong GetFromMap(int v1, int v2)
        {
            return _map[v1][v2];
        }

        internal void AddTag(int key, ulong v1, ulong v2)
        {
            _map[key] = new[] { v1, v2 };
        }

        private int XYToNum(int x, int y)
        {
            return x + y * WidthInTiles;
        }

        public Image GetTileJpeg(int x, int y)
        {
            var allTileInfos = this.GetAllTileInfos();
            var thisTileInfo = allTileInfos[this.XYToNum(x, y)];

            ulong pos = thisTileInfo[0];
            ulong size = thisTileInfo[1];
            
            _reader.Seek(pos, SeekOrigin.Begin);
            var bytes = _reader.ReadBytes(size);

            return Image.FromStream(new MemoryStream(bytes));
        }

        public Image GetImage()
        {
            if (this.Width > int.MaxValue || this.Height > int.MaxValue)
                throw new Exception("Image is too large to be rendered");

            var result = new Bitmap(this.Width, this.Height, PixelFormat.Format24bppRgb);

            using (var gr = Graphics.FromImage(result))
            {
                for (int x = 0; x < this.WidthInTiles; x++)
                {
                    for (int y = 0; y < this.HeightInTiles; y++)
                    {
                        using (var tile = this.GetTileJpeg(x, y))
                        {
                            gr.DrawImage(tile, x * this.TileWidth, y * this.TileHeight);
                        }                   
                    }
                }
            }

            return result;
        }

        private ulong[][] GetAllTileInfos()
        {
            if (_allTileInfos == null)
            {
                // For minimal values of TileCount, PositionFileOffset and SizeFileOffset
                // store the information directly rather than being pointers.
                ulong[] positions;
                if (this.TileCount == 1)
                    positions = new[] { this.PositionFileOffset };
                else
                {
                    _reader.Seek(this.PositionFileOffset, SeekOrigin.Begin);
                    positions = _reader.ReadUInt64Array(this.TileCount);
                }


                uint[] sizes;
                if (this.TileCount < 3)
                {
                    var sfo = this.SizeFileOffset;
                    sizes = new[] { (uint)(sfo & 0xFFFFFFFF), (uint)(sfo >> 32) };
                    sizes = sizes.Take(this.TileCount).ToArray();
                }
                else
                {
                    _reader.Seek(this.SizeFileOffset, SeekOrigin.Begin);
                    sizes = _reader.ReadUInt32Array(this.TileCount);
                }

                _allTileInfos = positions.Select((val, index) => new[] { val, sizes[index] }).ToArray();
            }

            return _allTileInfos;
        }

        public void Dispose()
        {
            ((IDisposable)_reader).Dispose();
        }
    }
}
