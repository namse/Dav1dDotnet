using System;
using Dav1dDotnet.Dav1d.Definitions;
using Dav1dDotnet.Ivf;

namespace Dav1dDotnet.Decoder
{
    public interface IDav1dDecoder: IDisposable
    {
        void SendIvfFrame(IvfFrame ivfFrame);
        bool TryGetDav1dPicture(out Dav1dPicture picture);
        void UnrefFrame(ref Av1Frame frame);
    }
}
