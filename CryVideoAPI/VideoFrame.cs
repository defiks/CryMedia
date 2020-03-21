﻿using System;
using System.IO;

namespace CryVideoAPI
{
    /// <summary>
    /// Video frame containing pixel data in RGB24 format.
    /// </summary>
    public class VideoFrame : IDisposable
    {
        int size, offset = 0;

        byte[] frameBuffer;
        public Memory<byte> RawData { get; }

        public int Width { get; }
        public int Height { get; }

        /// <summary>
        /// Creates an empty video frame with given dimensions using the RGB24 pixel format.
        /// </summary>
        /// <param name="w">Width in pixels</param>
        /// <param name="h">Height in pixels</param>
        public VideoFrame(int w, int h)
        {
            if (w <= 0 || h <= 0) throw new InvalidDataException("Video frame dimensions have to be bigger than 0 pixels!");

            Width = w;
            Height = h;

            size = Width * Height * 3;
            frameBuffer = new byte[size];
            RawData = frameBuffer.AsMemory();
        }

        /// <summary>
        /// Loads frame data from stream.
        /// </summary>
        /// <param name="str">Stream containing raw frame data in RGB24 format</param>
        public bool Load(Stream str)
        {
            offset = 0;

            while (offset < size)
            {
                var r = str.Read(frameBuffer, offset, size - offset);
                if (r <= 0) return false;
                offset += r;
            }

            return true;
        }

        /// <summary>
        /// Returns part of memory that contains the pixel data.
        /// </summary>
        /// <param name="x">Starting X coordinate of wanted pixel/s</param>
        /// <param name="y">Starting Y coordinate of wanted pixel/s</param>
        /// <param name="length">Number of pixels to return from the starting pixel</param>
        public Memory<byte> GetPixels(int x, int y, int length = 1) 
        {
            int index = (x + y * Width) * 3;
            return RawData.Slice(index, length * 3);
        }

        public void Dispose()
        {
            frameBuffer = null;
        }
    }
}