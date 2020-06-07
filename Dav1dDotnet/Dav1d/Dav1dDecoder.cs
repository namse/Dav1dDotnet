using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dav1dDotnet.Dav1d.Definitions;
using Dav1dDotnet.Decoder;
using Dav1dDotnet.Ivf;

namespace Dav1dDotnet.Dav1d
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MonoPInvokeCallbackAttribute : Attribute
    {
    }

    public class Dav1dDecoder: IDav1dDecoder
    {
        [DllImport("dav1d", CallingConvention = CallingConvention.Cdecl)]
        private static extern void dav1d_default_settings(ref Dav1dSettings s);

        /// <summary>
        /// Allocate and open a decoder instance.
        /// Note: The context must be freed using dav1d_close() when decoding is finished.
        /// </summary>
        /// <param name="contextOut">
        ///     The decoder instance to open. *c_out will be set to the allocated context.
        /// </param>
        /// <param name="s">
        ///     Input settings context.
        /// </param>
        /// <returns>0 on success, or &gt; 0 (a negative DAV1D_ERR code) on error.</returns>

        
        [DllImport("dav1d")]
        private static extern int dav1d_open(ref UIntPtr contextOut, ref Dav1dSettings s);

        [DllImport("dav1d")]
        private static extern int dav1d_get_picture(UIntPtr dav1dContext, out Dav1dPicture picture);

        [DllImport("dav1d")]
        private static extern int dav1d_data_wrap(ref Dav1dData data, byte[] buffer, UIntPtr size,
            FreeCallback freeCallback, IntPtr cookie);

        [DllImport("dav1d")]
        private static extern int dav1d_send_data(UIntPtr context, ref Dav1dData @in);

        [DllImport("dav1d")]
        private static extern IntPtr dav1d_data_create(ref Dav1dData data, UIntPtr sz);

        [DllImport("dav1d")]
        private static extern void dav1d_picture_unref(ref Dav1dPicture p);

        [DllImport("dav1d")]
        private static extern void dav1d_close(ref UIntPtr context);

        [DllImport("dav1d")]
        private static extern void dav1d_flush(UIntPtr context);

        public const int EAgain = -11;

        private UIntPtr _context;
        private readonly object _mutex = new object();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken Token => _cancellationTokenSource.Token;
        private readonly ConcurrentBag<Dav1dPicture> _usedPictures = new ConcurrentBag<Dav1dPicture>();

        public Dav1dDecoder(int frameThreads, int tileThreads)
        {
            var setting = new Dav1dSettings();
            dav1d_default_settings(ref setting);

            setting.nFrameThreads = frameThreads;
            setting.nTileThreads = tileThreads;

            var result = dav1d_open(ref _context, ref setting);
            if (result != 0)
            {
                throw new Exception($"fail to open dav1d {result}");
            }

            Timer.Run(Timer_UnrefAllPicturesAsync, _cancellationTokenSource.Token);
        }

        private async Task Timer_UnrefAllPicturesAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                UnrefAllPictures();
                await Task.Delay(1);
            }
        }

        private void UnrefAllPictures()
        {
            while (_usedPictures.TryTake(out var picture))
            {
                lock (_mutex)
                {
                    dav1d_picture_unref(ref picture);
                }
            }
        }

        public void SendIvfFrame(IvfFrame ivfFrame)
        {
            lock (_mutex)
            {
                var dav1dData = new Dav1dData();

                var result = dav1d_data_wrap(ref dav1dData,
                    ivfFrame.Memory.Buffer,
                    (UIntPtr)ivfFrame.Memory.Length, FreeCallback, IntPtr.Zero);

                if (result != 0)
                {
                    Debug.Print($"Data Wrap Error {result}");
                    throw new Exception($"Fail to wrap data {result}");
                }

                Debug.Print($"Send Data {ivfFrame.Memory.Length}");

                result = dav1d_send_data(_context, ref dav1dData);

                if (result < 0 && result != EAgain)
                {
                    Debug.Print($"Send Data Error {result}");
                    throw new Exception($"fail to send data {result}");
                }
            }
            
        }

        // For Unity, IL2CPP does not support marshaling delegates that point to instance methods to native code.
        // So it should be static method.
        [MonoPInvokeCallback]
        private static void FreeCallback(IntPtr data, IntPtr userData)
        {
            // TODO
        }

        public bool TryGetDav1dPicture(out Dav1dPicture picture)
        {
            lock (_mutex)
            {
                Debug.Print($"try get picture");
                var result = dav1d_get_picture(_context, out picture);
                if (result < 0)
                {
                    if (result != EAgain)
                    {
                        Debug.Print($"fail to get next frame {result}");
                        throw new Exception($"fail to get next frame {result}");
                    }

                    return false;
                }

                return true;
            }
        }

        public void UnrefFrame(ref Av1Frame frame)
        {
            var picture = frame.Picture;
            _usedPictures.Add(picture);
        }

        public void Dispose()
        {
            lock (_mutex)
            {
                dav1d_flush(_context);
                dav1d_close(ref _context);
            }
            _cancellationTokenSource?.Cancel(false);
            _cancellationTokenSource?.Dispose();
            UnrefAllPictures();
        }
    }
}
