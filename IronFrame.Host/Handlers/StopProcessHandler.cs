using System.Threading.Tasks;
using IronFrame.Messages;
using IronFrame.Utilities;

namespace IronFrame.Host.Handlers
{
    internal class StopProcessHandler
    {
        private readonly IProcessTracker processTracker;

        public StopProcessHandler(IProcessTracker processTracker)
        {
            this.processTracker = processTracker;
        }

        public Task ExecuteAsync(StopProcessParams p)
        {
            var process = processTracker.GetProcessByKey(p.key);

            return StopProcessAsync(process, p.timeout);
        }

        public static Task StopProcessAsync(IProcess process, int timeout)
        {
            return Task.Run(
                () =>
                {
                    process.RequestExit();
                    if (!process.WaitForExit(timeout))
                        process.Kill();
                }
            );
        }
    }
}