using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dav1dDotnet.Dav1d;
using Dav1dDotnet.Dav1d.Definitions;
using Dav1dDotnet.Decoder;
using Dav1dDotnet.Ivf;

namespace Dav1dDotnet
{
    public class IvfAv1Decoder : IIvfAv1Decoder
    {
        private readonly IvfParser _ivfParser;
        private readonly IDav1dDecoder _dav1dDecoder = new Dav1dDecoder(2, 2);
        private readonly ConcurrentQueue<IvfFrame> _parsedIvfFrames = new ConcurrentQueue<IvfFrame>();
        private readonly ConcurrentQueue<IvfFrame> _consumingIvfFrames = new ConcurrentQueue<IvfFrame>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Dictionary<int, Av1Frame> _av1Frames = new Dictionary<int, Av1Frame>();
        private int _skippedFrameNumber = -1;
        private int _nextAv1FrameNumber;
        private readonly ConcurrentQueue<Dav1dPicture> _pictures = new ConcurrentQueue<Dav1dPicture>();
        private int _parsedPictureCount;
        private int _consumingFrameNumber = -1;


        public IvfAv1Decoder(Stream stream)
        {
            _ivfParser = new IvfParser(stream);
            _ivfParser.FrameParsed += IvfParserOnFrameParsed;

            Timer.Run(DecodeAllAsync, _cancellationTokenSource.Token);
            Timer.Run(ConvertPicturesAsync, _cancellationTokenSource.Token);
            Timer.Run(ConsumingAllFramesAsync, _cancellationTokenSource.Token);
        }

        private async Task ConsumingAllFramesAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (!_consumingIvfFrames.TryPeek(out var ivfFrame)
                    || ivfFrame.FrameNumber > _consumingFrameNumber)
                {
                    await Task.Delay(1);
                    continue;
                }

                if (!_consumingIvfFrames.TryDequeue(out var _ivfFrame) || !_ivfFrame.Equals(ivfFrame))
                {
                    Debug.Fail("peeked ivfFrame should be same with dequeued");
                }

                Av1Frame av1Frame;
                lock (_av1Frames)
                {
                    if (!_av1Frames.TryGetValue(ivfFrame.FrameNumber, out av1Frame))
                    {
                        Debug.Fail($"fail to get av1 frame of frame number {ivfFrame.FrameNumber}");
                    }
                }

                _dav1dDecoder.UnrefFrame(ref av1Frame);
                _ivfParser.ConsumeFrame(ivfFrame);

                await Task.Delay(1);
            }
        }

        private void IvfParserOnFrameParsed(IvfFrame ivfFrame)
        {
            _parsedIvfFrames.Enqueue(ivfFrame);
        }

        private async Task ConvertPicturesAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                while (_pictures.TryDequeue(out var picture))
                {
                    var frameNumber = _nextAv1FrameNumber;
                    _nextAv1FrameNumber += 1;

                    var av1Frame = new Av1Frame(picture);
                    lock (_av1Frames)
                    {
                        _av1Frames.Add(frameNumber, av1Frame);
                    }
                }

                await Task.Delay(1);
            }
        }

        private async Task DecodeAllAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested
            && !(_ivfParser.Done && _ivfParser.LastFrameNumber <= _parsedPictureCount))
            {
                if (_parsedIvfFrames.TryDequeue(out var ivfFrame))
                {
                    _consumingIvfFrames.Enqueue(ivfFrame);
                    // TODO : Truly skip with _skippedFrameNumber without decoding frame body
                    _dav1dDecoder.SendIvfFrame(ivfFrame);
                }

                while (_dav1dDecoder.TryGetDav1dPicture(out var picture))
                {
                    Debug.Print($"_parsedPictureCount: {_parsedPictureCount}");
                    _parsedPictureCount += 1;
                    _pictures.Enqueue(picture);
                }

                await Task.Delay(1);
            }

        }

        public bool TryGetAv1Frame(int frameNumber, out Av1Frame av1Frame)
        {
            lock (_av1Frames)
            {
                return _av1Frames.TryGetValue(frameNumber, out av1Frame);
            }
        }

        public void SkipTo(int frameNumber)
        {
            SetBigger(ref _skippedFrameNumber, frameNumber);
            SetBigger(ref _consumingFrameNumber, frameNumber);
        }

        private void SetBigger(ref int dest, int value)
        {
            int initialValue;
            do
            {
                initialValue = dest;
                if (initialValue >= value)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref dest, value, initialValue) != initialValue);
        }

        public void CheckConsumedFrameNumber(int frameNumber)
        {
            _consumingFrameNumber = frameNumber;
        }

        public void Dispose()
        {
            _ivfParser?.Dispose();
            _cancellationTokenSource?.Cancel(false);
            _cancellationTokenSource?.Dispose();
            _dav1dDecoder?.Dispose();
        }
    }
}
