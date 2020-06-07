using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dav1dDotnet
{
    internal static class Timer
    {
        public static void Run(Func<Task> func, CancellationToken token)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await func();
                }
                catch (Exception exception)
                {
                    Debug.Print(exception.ToString());
                }
            }, token);
        }
    }
}
