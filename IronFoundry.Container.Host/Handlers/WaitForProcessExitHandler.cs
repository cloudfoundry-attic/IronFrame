﻿using System;
using System.Threading.Tasks;
using IronFoundry.Container.Messages;

namespace IronFoundry.Container.Host.Handlers
{
    internal class WaitForProcessExitHandler
    {
        readonly IProcessTracker processTracker;

        public WaitForProcessExitHandler(IProcessTracker processTracker)
        {
            this.processTracker = processTracker;
        }

        public Task<WaitForProcessExitResult> ExecuteAsync(WaitForProcessExitParams p)
        {
            var process = processTracker.GetProcessByKey(p.key);
            if (process == null)
                throw new Exception(String.Format("A process with key '{0}' is not being tracked.", p.key));

            var success = process.WaitForExit(p.timeout);

            var result = new WaitForProcessExitResult
            {
                exited = success,
                exitCode = success ? process.ExitCode : 0,
            };
            return Task.FromResult(result);
        }
    }
}
