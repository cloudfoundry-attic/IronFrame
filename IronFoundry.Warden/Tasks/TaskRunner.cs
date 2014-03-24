namespace IronFoundry.Warden.Tasks
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Containers;
    using Jobs;
    using Newtonsoft.Json;
    using NLog;
    using Protocol;
    using Utilities;

    public class TaskRunner : IJobRunnable
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        private readonly IContainerClient container;
        private readonly ITaskRequest request;
        private readonly TaskCommandDTO[] commands;

        private ConcurrentQueue<TaskCommandStatus> jobStatusQueue;

        public TaskRunner(IContainerClient container, ITaskRequest request)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            this.container = container;

            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            this.request = request;

            if (this.request.Script.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("request.Script can't be empty.");
            }

            commands = JsonConvert.DeserializeObject<TaskCommandDTO[]>(request.Script);
            if (commands.IsNullOrEmpty())
            {
                throw new ArgumentException("Expected to run at least one command.");
            }
        }

        public bool HasStatus
        {
            get { return !jobStatusQueue.IsEmpty; }
        }

        public IEnumerable<IJobStatus> RetrieveStatus()
        {
            var statusList = new List<IJobStatus>();

            TaskCommandStatus status;
            while (jobStatusQueue.TryDequeue(out status))
            {
                statusList.Add(status);
            }

            return statusList;
        }

        public event EventHandler<JobStatusEventArgs> JobStatusAvailable;

        protected virtual void OnJobStatusAvailable(JobStatusEventArgs e)
        {
            var handlers = JobStatusAvailable;
            if (handlers != null)
            {
                handlers(this, e);
            }
        }

        public Task<IJobResult> RunAsync()
        {
            jobStatusQueue = new ConcurrentQueue<TaskCommandStatus>();
            return DoRunAsync();
        }

        public void Cancel()
        {
            cts.Cancel();
        }

        public IJobResult Run()
        {
            return DoRunAsync().GetAwaiter().GetResult();
        }

        private async Task<IJobResult> DoRunAsync()
        {
            bool shouldImpersonate = !request.Privileged;

            var results = new List<CommandResult>();
            foreach (TaskCommandDTO cmd in commands)
            {
                if (cts.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var commandResult = await container.RunCommandAsync( new RemoteCommand(shouldImpersonate, cmd.Command, cmd.Args));
                    results.Add(commandResult);
                }
                catch (Exception ex)
                {
                    results.Add(new CommandResult() { ExitCode = 1, StdOut = null, StdErr = ex.Message });
                    break;
                }
            }

            return FlattenResults(results);
        }

        private static IJobResult FlattenResults(IEnumerable<CommandResult> results)
        {
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            int lastExitCode = 0;
            foreach (var result in results)
            {
                stdout.SmartAppendLine(result.StdOut);
                stderr.SmartAppendLine(result.StdErr);
                if (result.ExitCode != 0)
                {
                    lastExitCode = result.ExitCode;
                    break;
                }
            }

            //bb: Introduce new result type to keep IJobResult and TaskCommandXXX separate
            return new TaskCommandResult(lastExitCode, stdout.ToString(), stderr.ToString());
        }
    }
}
