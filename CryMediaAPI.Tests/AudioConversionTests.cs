﻿using CryMediaAPI.Audio;
using CryMediaAPI.Encoding;
using CryMediaAPI.Encoding.Builders;
using CryMediaAPI.Resources;

using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

namespace CryMediaAPI.Tests
{
    public class AudioConversionTests
    {
        [Fact]
        public async Task FFmpegWrapperProgressTest()
        {
            var path = Res.GetPath(Res.Audio_Ogg);
            var opath = "out-test.mp3";

            double lastval = -1;

            try
            {
                var audio = new AudioReader(path);

                await audio.LoadMetadataAsync();
                var dur = audio.Metadata.Duration;
                audio.Dispose();

                Assert.True(Math.Abs(dur - 1.515102) < 0.01);

                var p = FFmpegWrapper.ExecuteCommand("ffmpeg", $"-i \"{path}\" \"{opath}\"");
                var progress = FFmpegWrapper.RegisterProgressTracker(p, dur);
                progress.ProgressChanged += (s, prg) => lastval = prg;             
                p.WaitForExit();

                await Task.Delay(300);

                Assert.True(lastval > 50 && lastval <= 100);

                audio = new AudioReader(opath);

                await audio.LoadMetadataAsync();

                Assert.True(audio.Metadata.Channels == 2);
                Assert.True(audio.Metadata.Streams.Length == 1);
                Assert.True(Math.Abs(audio.Metadata.Duration - 1.515102) < 0.2);

                audio.Dispose();
            }
            finally
            {
                if (File.Exists(opath)) File.Delete(opath);
            }
        }

        [Fact]
        public async Task ConversionTest()
        {
            var path = Res.GetPath(Res.Audio_Ogg);
            var opath = "out-test-2.mp3";

            try
            {
                using var reader = new AudioReader(path);
                await reader.LoadMetadataAsync();

                using (var writer = new AudioWriter(opath, 
                    reader.Metadata.Channels, 
                    reader.Metadata.SampleRate, 16,
                    new MP3Encoder().Create()))
                {
                    writer.OpenWrite();

                    reader.Load();

                    await reader.CopyToAsync(writer);
                }

                using var audio = new AudioReader(opath);
                await audio.LoadMetadataAsync();

                Assert.True(audio.Metadata.Format.FormatName == "mp3");
                Assert.True(audio.Metadata.Channels == 2);
                Assert.True(audio.Metadata.Streams.Length == 1);
                Assert.True(Math.Abs(audio.Metadata.Duration - 1.515102) < 0.2);              
            }
            finally
            {
                if (File.Exists(opath)) File.Delete(opath);
            }
        }

        [Fact]
        public async Task ConversionStreamTest()
        {
            var path = Res.GetPath(Res.Audio_Mp3);
            var opath = "out-test-v-2.aac";

            try
            {
                using var reader = new AudioReader(path);
                await reader.LoadMetadataAsync();

                var encoder = new AACEncoder
                {
                    Format = "flv"
                };

                using (var filestream = File.Create(opath))
                {
                    using (var writer = new AudioWriter(filestream,
                       reader.Metadata.Channels,
                       reader.Metadata.SampleRate, 16,
                       encoder.Create()))
                    {
                        writer.OpenWrite();

                        reader.Load();

                        await reader.CopyToAsync(writer);
                    }                 
                }

                using var audio = new AudioReader(opath);
                await audio.LoadMetadataAsync();

                Assert.True(audio.Metadata.Format.FormatName == "flv");
                Assert.True(audio.Metadata.Channels == 2);
                Assert.True(audio.Metadata.Streams.Length == 1);
                Assert.True(Math.Abs(audio.Metadata.Duration - 1.515102) < 0.2);
            }
            finally
            {
                if (File.Exists(opath)) File.Delete(opath);
            }
        }
    }
}
