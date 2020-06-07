using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dav1dDotnet.Ivf
{
    public class IvfParser : IDisposable
    {
        private readonly Stream _stream;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken Token => _cancellationTokenSource.Token;
        private const int FrameHeaderSize = 12;

        public delegate void FrameParsedDelegate(IvfFrame ivfFrame);
        public event FrameParsedDelegate FrameParsed;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10);

        public bool Done { get; private set; }
        public int LastFrameNumber { get; private set; } = -1;

        public IvfParser(Stream stream)
        {
            _stream = stream;

            Timer.Run(ReadStreamAsync, _cancellationTokenSource.Token);
        }

        private async Task ReadStreamAsync()
        {
            var metaHeaderMemory = new ByteMemory(32);
            var isReadSuccessful = await _stream.TryReadCompletelyAsync(metaHeaderMemory, Token);
            if (!isReadSuccessful)
            {
                throw new Exception("Cannot read meta header");
            }
            // TODO : Read frame count from meta header
            /*
                bytes 0-3    signature: 'DKIF'
                bytes 4-5    version (should be 0)
                bytes 6-7    length of header in bytes
                bytes 8-11   codec FourCC (e.g., 'VP80')
                bytes 12-13  width in pixels
                bytes 14-15  height in pixels
                bytes 16-23  time base denominator
                bytes 20-23  time base numerator
                bytes 24-27  number of frames in file
                bytes 28-31  unused
             */

            var headerMemory = new ByteMemory(FrameHeaderSize);
            var frameNumber = 0;

            while (!Token.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(Token);

                isReadSuccessful = await _stream.TryReadCompletelyAsync(headerMemory, Token);
                if (!isReadSuccessful)
                {
                    break;
                }

                LastFrameNumber += 1;

                var frameSize = BitConverter.ToInt32(headerMemory.Buffer, headerMemory.Offset);

                var frameMemoryOwner = ByteMemoryPool.Rent(frameSize);
                var frameMemory = frameMemoryOwner.Memory.Slice(0, frameSize);

                isReadSuccessful = await _stream.TryReadCompletelyAsync(frameMemory, Token);
                if (!isReadSuccessful)
                {
                    throw new Exception("Cannot read frame body");
                }

                var frame = new IvfFrame(frameNumber++, frameMemory, frameMemoryOwner);
                FrameParsed?.Invoke(frame);
            }

            Done = true;
            Debug.Print($"Read Stream Done. Frame Number {frameNumber - 1}");
        }

        public void ConsumeFrame(IvfFrame frame)
        {
            frame.Dispose();
            _semaphore.Release();
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel(false);
            _cancellationTokenSource?.Dispose();
        }
    }
}