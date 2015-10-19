using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

            return StopProcessAsync(process);
        }

        public static Task StopProcessAsync(IProcess process)
        {
            if (process == null)
                return Task.FromResult<object>(null);

            return Task.Run(() => process.Kill());
        }
    }
}