using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Dav1dDotnet;

namespace benchmark
{
    [MemoryDiagnoser]
    public class DecodeBenchmark
    {
        private readonly byte[] _ivfVideoBytes;

        public DecodeBenchmark()
        {
            _ivfVideoBytes = File.ReadAllBytes(@"Resources\whiteAlpha.ivf");
        }

        [Benchmark]
        public async Task DecodeAsync() {
            
            var memoryStream = new MemoryStream(_ivfVideoBytes);

            using var decoder = new IvfAv1Decoder(memoryStream);

            for (var i = 0; i< 180; i += 1)
            {
                while (!decoder.TryGetAv1Frame(i, out var frame))
                {
                    await Task.Delay(1);
                }


                decoder.CheckConsumedFrameNumber(i);
            }
        }
    }


    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly,
                DefaultConfig.Instance
                    .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
                    .WithSummaryStyle(SummaryStyle.Default));
        }
    }
}
