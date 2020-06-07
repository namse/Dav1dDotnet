using System;
using Dav1dDotnet.Decoder;

namespace Dav1dDotnet
{
    public interface IIvfAv1Decoder : IDisposable
    {
        bool TryGetAv1Frame(int frameNumber, out Av1Frame av1Frame);
        void SkipTo(int frameNumber);
        void CheckConsumedFrameNumber(int frameNumber);
    }
}