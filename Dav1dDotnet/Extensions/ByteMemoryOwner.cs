using System;

namespace Dav1dDotnet
{
    public readonly struct ByteMemoryOwner: IDisposable
    {
        public readonly ByteMemory Memory;

        public ByteMemoryOwner(ByteMemory memory)
        {
            Memory = memory;
        }

        public void Dispose()
        {
            ByteMemoryPool.Return(Memory);
        }
    }
}