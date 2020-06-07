using System.Collections.Generic;

namespace Dav1dDotnet
{
    public static class ByteMemoryPool
    {
        private static readonly object Mutex = new object();
        private static readonly Dictionary<int, Queue<ByteMemory>> MemoryBagDictionary = new Dictionary<int, Queue<ByteMemory>>();

        public static int GetIndex(int minBytes)
        {
            var index = 1;
            while (minBytes > index)
            {
                index *= 2;
            }
            return index;
        }

        public static ByteMemoryOwner Rent(int minBytes)
        {
            var index = GetIndex(minBytes);
            lock (Mutex)
            {
                var memory = MemoryBagDictionary.TryGetValue(index, out var bag) && bag.Count > 0
                    ? bag.Dequeue()
                    : new ByteMemory(index);

                return new ByteMemoryOwner(memory);
            }
        }

        public static void Return(ByteMemory memory)
        {
            var index = GetIndex(memory.Length);
            lock (Mutex)
            {
                if (!MemoryBagDictionary.TryGetValue(index, out var bag))
                {
                    bag = new Queue<ByteMemory>();
                    MemoryBagDictionary[index] = bag;
                }
                bag.Enqueue(memory);
            }
        }
    }
}