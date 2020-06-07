using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dav1dDotnet
{
    public static class StreamExtension
    {
        public static async Task<bool> TryReadCompletelyAsync(this Stream stream, ByteMemory memory, CancellationToken token)
        {
            var offset = 0;
            while (offset < memory.Length && !token.IsCancellationRequested)
            {   
                var readBytes = await stream.ReadAsync(memory.Buffer, memory.Offset + offset, memory.Length - offset, token);
                if (readBytes == 0)
                {
                    return false;
                }
                offset += readBytes;
            }

            return true;
        }
    }
}