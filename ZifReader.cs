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
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/**
 * I am indebted to Ophir LOJKINE for his Dezoomify project (https://github.com/lovasoa/dezoomify)
 * and also for the work he did in making a JS parser for ZIF:
 *
 *   Documentation: https://github.com/lovasoa/ZIF/blob/master/README.md
 *   JavaScript parser: http://lovasoa.github.io/ZIF/ZIF.js
 *   Demo page: http://lovasoa.github.io/ZIF/
 */

namespace Zif
{
    public class ZifReader: IDisposable
    {        
        private EndianBinaryReader _reader;
        private const int MAX_ZIF_BYTES = 8192;
        private Stream _internalStream = null;

        private List<ZoomLevel> _zoomLevels = new List<ZoomLevel>();
        private ReadOnlyCollection<ZoomLevel> _roZoomLevels;
        public ReadOnlyCollection<ZoomLevel> ZoomLevels { get { return _roZoomLevels; } }

        public ZifReader()
        {
            _roZoomLevels = new ReadOnlyCollection<ZoomLevel>(_zoomLevels);
        }

        public ZifReader(string fileName)
        {
            this.Load(fileName);
        }

        public ZifReader(Stream inputStream)
        {
            this.Load(inputStream);
        }

        public ZifReader(byte[] bytes)
        {
            this.Load(bytes);
        }

        public void Load(string fileName)
        {
            this.Load(File.OpenRead(fileName));
        }

        public void Load(byte[] bytes)
        {
            this.Load(new MemoryStream(bytes));
        }

        public void Load(Stream inputStream)
        {
            this.SetInternalStream(inputStream);

            DisposeLevels();
            _zoomLevels.Clear();

            _reader = new EndianBinaryReader(EndianBitConverter.Little, inputStream);

            // Check magic bytes
            if (_reader.ReadInt64() != 0x08002b4949)
                throw new Exception("Invalid ZIF file");

            while (_reader.BaseStream.Position < MAX_ZIF_BYTES)
            {
                ulong offset = _reader.ReadUInt64();
                if (offset == 0)
                    break;

                _reader.Seek(offset, SeekOrigin.Begin);
                var numTags = _reader.ReadUInt64();

                var level = new ZoomLevel(_reader);
                for (ulong i = 0; i < numTags; ++i)
                {
                    var key = _reader.ReadUInt16();
                    var notUsed = _reader.ReadUInt16();
                    var val1 = _reader.ReadUInt64();
                    var val2 = _reader.ReadUInt64();
                    level.AddTag(key, val1, val2);
                }
                _zoomLevels.Insert(0, level);
            }            
        }

        public Image GetTileJpeg(int zoomLevel, int x, int y)
        {
            return _zoomLevels[zoomLevel].GetTileJpeg(x, y);
        }

        public Image GetImage(int zoomLevel)
        {
            return _zoomLevels[zoomLevel].GetImage();
        }

        private void DisposeLevels()
        {
            foreach (var level in _zoomLevels)
                ((IDisposable)level).Dispose();
        }

        private void SetInternalStream(Stream stream)
        {
            if (_internalStream != null)
            {
                _internalStream.Close();
            }

            _internalStream = stream;
        }

        public void Dispose()
        {
            DisposeLevels();
            SetInternalStream(null);
            ((IDisposable)_reader).Dispose();
        }
    }
}
