﻿using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using CryMediaAPI.BaseClasses;

namespace CryMediaAPI.Video
{
    public class VideoWriter : MediaWriter<VideoFrame>, IDisposable
    {
        string ffmpeg;
        CancellationTokenSource csc;
        internal Process ffmpegp;
        
        public int Width { get; }
        public int Height { get; }
        public double Framerate { get; }
        public bool UseFilename { get; }
        public FFmpegVideoEncoderOptions EncoderOptions { get; }

        public Stream DestinationStream { get; private set; }
        public Stream OutputDataStream { get; private set; }


        


        /// <summary>
        /// Used for encoding frames into a new video file
        /// </summary>
        /// <param name="filename">Output video file name/path</param>
        /// <param name="width">Input width of the video in pixels</param>
        /// <param name="height">Input height of the video in pixels </param>
        /// <param name="framerate">Input framerate of the video in fps</param>
        /// <param name="encoderOptions">Extra FFmpeg encoding options that will be passed to FFmpeg</param>
        /// <param name="ffmpegExecutable">Name or path to the ffmpeg executable</param>
        public VideoWriter(string filename, int width, int height, double framerate,
            FFmpegVideoEncoderOptions encoderOptions = null, string ffmpegExecutable = "ffmpeg")
        {
            if (width <= 0 || height <= 0) throw new InvalidDataException("Video frame dimensions have to be bigger than 0 pixels!");
            if (framerate <= 0) throw new InvalidDataException("Video framerate has to be bigger than 0!");
            if (string.IsNullOrEmpty(filename)) throw new NullReferenceException("Filename can't be null or empty!");

            UseFilename = true;
            Filename = filename;

            ffmpeg = ffmpegExecutable;

            Width = width;
            Height = height;
            Framerate = framerate;
            DestinationStream = null;
            EncoderOptions = encoderOptions ?? new FFmpegVideoEncoderOptions();
        }

        /// <summary>
        /// Used for encoding frames into a stream (Requires using a supported format like 'flv' for streaming)
        /// </summary>
        /// <param name="destinationStream">Output stream</param>
        /// <param name="width">Input width of the video in pixels</param>
        /// <param name="height">Input height of the video in pixels </param>
        /// <param name="framerate">Input framerate of the video in fps</param>
        /// <param name="encoderOptions">Extra FFmpeg encoding options that will be passed to FFmpeg</param>
        /// <param name="ffmpegExecutable">Name or path to the ffmpeg executable</param>
        public VideoWriter(Stream destinationStream, int width, int height, double framerate,
            FFmpegVideoEncoderOptions encoderOptions = null, string ffmpegExecutable = "ffmpeg")
        {
            if (width <= 0 || height <= 0) throw new InvalidDataException("Video frame dimensions have to be bigger than 0 pixels!");
            if (framerate <= 0) throw new InvalidDataException("Video framerate has to be bigger than 0!");
            if (destinationStream == null) throw new NullReferenceException("Stream can't be null!");

            UseFilename = false;

            ffmpeg = ffmpegExecutable;

            Width = width;
            Height = height;
            Framerate = framerate;
            DestinationStream = destinationStream;
            EncoderOptions = encoderOptions ?? new FFmpegVideoEncoderOptions();
        }

        /// <summary>
        /// Prepares for writing.
        /// </summary>
        /// <param name="showFFmpegOutput">Show FFmpeg encoding output for debugging purposes.</param>
        public void OpenWrite(bool showFFmpegOutput = false)
        {
            if (OpenedForWriting) throw new InvalidOperationException("File was already opened for writing!");

            var cmd = $"-f rawvideo -video_size {Width}:{Height} -r {Framerate} -pixel_format rgb24 -i - " +
                $"-c:v {EncoderOptions.EncoderName} {EncoderOptions.EncoderArguments} -f {EncoderOptions.Format}";

            if (UseFilename)
            {
                if (File.Exists(Filename)) File.Delete(Filename);

                InputDataStream = FFmpegWrapper.OpenInput(ffmpeg, $"{cmd} \"{Filename}\"", out ffmpegp, showFFmpegOutput);
            }
            else
            {
                csc = new CancellationTokenSource();

                // using stream
                (InputDataStream, OutputDataStream) = FFmpegWrapper.Open(ffmpeg, $"{cmd} -", out ffmpegp, showFFmpegOutput);
                _ = OutputDataStream.CopyToAsync(DestinationStream, csc.Token);
            }         

            OpenedForWriting = true;
        }

        /// <summary>
        /// Closes output video file.
        /// </summary>
        public void CloseWrite()
        {
            if (!OpenedForWriting) throw new InvalidOperationException("File is not opened for writing!");

            try
            {
                csc?.Cancel();

                try
                {
                    if (ffmpegp.HasExited) ffmpegp.Kill();
                }
                catch { }

                InputDataStream.Dispose();

                if (!UseFilename) OutputDataStream?.Dispose();
            }
            finally
            {
                OpenedForWriting = false;
            }
        }

        public void Dispose()
        {
            if (OpenedForWriting) CloseWrite();
        }
    }

    /// <summary>
    /// FFmpeg video encoding options to pass to FFmpeg when encoding. Check the online FFmpeg documentation for more info.
    /// </summary>
    public class FFmpegVideoEncoderOptions
    {
        /// <summary>
        /// Container format. (example: 'mp4', 'flv', 'webm')
        /// </summary>
        public string Format { get; set; } = "mp4";

        /// <summary>
        /// Encoder name. (example: 'libx264', 'libx265', 'libvpx')
        /// </summary>
        public string EncoderName { get; set; } = "libx264";

        /// <summary>
        /// Arguments for the encoder. This depends on the used encoder.
        /// </summary>
        public string EncoderArguments { get; set; } = "-preset veryfast -crf 23 -c:a copy";
    }
}
