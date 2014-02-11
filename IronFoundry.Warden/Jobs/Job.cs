namespace IronFoundry.Warden.Jobs
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NLog;

    public class Job
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly uint jobId;
        private readonly IJobRunnable runnable;

        private Task<IJobResult> runnableTask;
        private IJobListener listener;
        private IJobResult result;
        private bool isCompleted = false;

        public Job(uint jobId, IJobRunnable runnable)
        {
            if (jobId == default(uint))
            {
                throw new ArgumentException("jobId must be > 0");
            }
            if (runnable == null)
            {
                throw new ArgumentNullException("runnable");
            }
            this.jobId = jobId;
            this.runnable = runnable;
        }

        public async void RunAsync()
        {
            try
            {
                runnableTask = runnable.RunAsync();
                result = await runnableTask;
            }
            catch (Exception ex)
            {
                result = new JobExceptionResult(ex);
            }
            finally
            {
                isCompleted = true;
            }
        }

        public Task<IJobResult> RunnableTask
        {
            get { return runnableTask; }
        }

        public void AttachListener(IJobListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException("listener");
            }
            if (this.listener != null)
            {
                throw new InvalidOperationException("Only one listener can be attached to a job at a time.");
            }
            this.listener = listener;
        }

        public async Task<IJobResult> ListenAsync()
        {
            if (this.listener == null)
            {
                throw new InvalidOperationException("Must attach listener before calling Listen()");
            }
            if (this.runnableTask == null)
            {
                throw new InvalidOperationException("Must call Run() before calling Listen()");
            }

            try
            {
                runnable.JobStatusAvailable += runnable_JobStatusAvailable;
                result = await runnableTask;
            }
            finally
            {
                runnable.JobStatusAvailable -= runnable_JobStatusAvailable;
                isCompleted = true;
            }

            return result;
        }

        public void Cancel()
        {
            runnable.Cancel();
        }

        public bool IsCompleted
        {
            get { return isCompleted; }
        }

        public IJobResult Result
        {
            get { return result; }
        }

        public bool HasStatus
        {
            get { return runnable.HasStatus; }
        }

        /// <summary>
        /// NB: dequeues all the status
        /// </summary>
        public IEnumerable<IJobStatus> RetrieveStatus()
        {
            return runnable.RetrieveStatus();
        }

        private async void runnable_JobStatusAvailable(object sender, JobStatusEventArgs e)
        {
            if (this.listener == null)
            {
                throw new InvalidOperationException("Must attach listener before calling Listen()");
            }
            await listener.ListenStatusAsync(e.JobStatus);
        }
    }
}
