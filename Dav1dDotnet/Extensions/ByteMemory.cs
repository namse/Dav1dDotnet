namespace Dav1dDotnet
{
    public readonly struct ByteMemory
    {
        public readonly byte[] Buffer;
        public readonly int Offset;
        public readonly int Length;

        public ByteMemory(byte[] buffer, int offset, int length)
        {
            Buffer = buffer;
            Offset = offset;
            Length = length;
        }

        public ByteMemory(byte[] buffer)
        {
            Buffer = buffer;
            Offset = 0;
            Length = buffer.Length;
        }

        public ByteMemory(int length)
        {
            Buffer = new byte[length];
            Offset = 0;
            Length = length;
        }

        public ByteMemory Slice(int offset, int length)
        {
            return new ByteMemory(Buffer, Offset + offset, length);
        }
    }
}