using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Test.TestSupport
{
    class InputSource : TextReader
    {
        object syncRoot = new object();
        bool disposed = false;
        Queue<TaskCompletionSource<string>> pending = new Queue<TaskCompletionSource<string>>();
        Queue<TaskCompletionSource<string>> completed = new Queue<TaskCompletionSource<string>>();

        public void AddLine(string line)
        {
            lock (syncRoot)
            {
                if (pending.Count > 0)
                {
                    var source = pending.Dequeue();
                    source.SetResult(line);
                }
                else
                {
                    var source = new TaskCompletionSource<string>();
                    source.SetResult(line);
                    completed.Enqueue(source);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            disposed = true;

            while (pending.Count > 0)
            {
                var source = pending.Dequeue();
                source.SetResult(null);
            }

            base.Dispose(disposing);
        }

        public override Task<string> ReadLineAsync()
        {
            if (disposed)
                return Task.FromResult<string>(null);

            lock (syncRoot)
            {
                if (completed.Count > 0)
                {
                    var source = completed.Dequeue();
                    return source.Task;
                }
                else
                {
                    var source = new TaskCompletionSource<string>();
                    pending.Enqueue(source);
                    return source.Task;
                }
            }
        }

        public override string ReadLine()
        {
            lock (syncRoot)
            {
                return ReadLineAsync().GetAwaiter().GetResult();
            }
        }
    }
}
