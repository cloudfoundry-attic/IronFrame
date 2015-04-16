using System;
using System.Threading;
using System.Threading.Tasks;

namespace IronFrame.Messaging
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Based on Stephen Toub's article(s) of Async Coordination primitives.
    /// See http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx
    /// </remarks>
    internal class AsyncLock
    {
        private SemaphoreSlim semaphor = new SemaphoreSlim(1, 1);
        private Task<IDisposable> releaser;

        public AsyncLock()
        {
            releaser = Task.FromResult((IDisposable)new LockReleaser(this));
        }

        public Task<IDisposable> LockAsync()
        {
            var wait = semaphor.WaitAsync();

            if (wait.IsCompleted)
                return releaser;
            else
                return wait.ContinueWith((_, state) => (IDisposable)new LockReleaser((AsyncLock)state),
                    this,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        public struct LockReleaser : IDisposable
        {
            private readonly AsyncLock toRelease;

            internal LockReleaser(AsyncLock asyncLock) { this.toRelease = asyncLock; }

            public void Dispose()
            {
                if (toRelease != null)
                {
                    toRelease.semaphor.Release();
                }
            }
        }
    }
}
