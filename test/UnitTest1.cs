using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dav1dDotnet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace test
{
    [TestClass]
    public class UnitTest1
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                Console.WriteLine(args.Exception);
            };
        }

        [TestMethod]
        [Timeout(8 * 1000)]
        public async Task DecodeEveryFramesFromMemory()
        {
            var ivfVideoBytes = await File.ReadAllBytesAsync(@"Resources\whiteAlpha.ivf");
            var memoryStream = new MemoryStream(ivfVideoBytes);

            using var decoder = new IvfAv1Decoder(memoryStream);

            for (var i = 0; i < 180; i += 1)
            {
                while (!decoder.TryGetAv1Frame(i, out var frame))
                {
                    await Task.Delay(1);
                }

                Console.WriteLine(i);

                decoder.CheckConsumedFrameNumber(i);
            }
        }

        [TestMethod]
        [Timeout(5 * 1000)]
        public async Task DecodeEveryFrames_FromMemory_ConsumeEvenNumber()
        {
            var ivfVideoBytes = await File.ReadAllBytesAsync(@"Resources\whiteAlpha.ivf");
            var memoryStream = new MemoryStream(ivfVideoBytes);

            using var decoder = new IvfAv1Decoder(memoryStream);

            for (var i = 0; i < 180; i += 1)
            {
                while (!decoder.TryGetAv1Frame(i, out var frame))
                {
                    await Task.Delay(1);
                }

                if (i % 2 == 0)
                {
                    decoder.CheckConsumedFrameNumber(i);
                }
            }
        }


        [TestMethod]
        [Timeout(5 * 1000)]
        public async Task DecodeEveryFrames_FromMemory_Skip60()
        {
            var ivfVideoBytes = await File.ReadAllBytesAsync(@"Resources\whiteAlpha.ivf");
            var memoryStream = new MemoryStream(ivfVideoBytes);

            using var decoder = new IvfAv1Decoder(memoryStream);
            decoder.SkipTo(60);

            for (var i = 0; i < 120; i += 1)
            {
                while (!decoder.TryGetAv1Frame(i + 60, out var frame))
                {
                    await Task.Delay(1);
                }

                Console.WriteLine(i);
                if (i % 2 == 0)
                {
                    decoder.CheckConsumedFrameNumber(i);
                }
            }

            Assert.IsFalse(decoder.TryGetAv1Frame(181, out _));
        }
    }
}
