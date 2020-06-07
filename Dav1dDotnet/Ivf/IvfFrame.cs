using System;

namespace Dav1dDotnet.Ivf
{
    public readonly struct IvfFrame: IDisposable
    {
        public readonly int FrameNumber;
        public readonly ByteMemory Memory;
        private readonly ByteMemoryOwner _byteMemoryOwner;

        public IvfFrame(int frameNumber, ByteMemory memory, ByteMemoryOwner byteMemoryOwner)
        {
            FrameNumber = frameNumber;
            Memory = memory;
            _byteMemoryOwner = byteMemoryOwner;
        }

        public void Dispose()
        {
            _byteMemoryOwner.Dispose();
        }
    }
}