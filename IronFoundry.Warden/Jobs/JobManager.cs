using System.Collections.Generic;
using System.Threading;
using NLog;

namespace IronFoundry.Warden.Jobs
{
    public class JobManager : IJobManager
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private uint jobIds = 0;

        private readonly IDictionary<uint, Job> jobs = new Dictionary<uint, Job>();
        private readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();

        public uint StartJobFor(IJobRunnable runnable)
        {
            try
            {
                rwlock.EnterWriteLock();
                uint jobId = GetNextJobID();
                var job = new Job(jobId, runnable);
                jobs.Add(jobId, job);
                job.RunAsync();
                return jobId;
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }

        public Job GetJob(uint jobId)
        {
            try
            {
                rwlock.EnterUpgradeableReadLock();

                Job job = null;
                if (jobs.TryGetValue(jobId, out job))
                {
                    if (job.IsCompleted)
                    {
                        try
                        {
                            rwlock.EnterWriteLock();
                            jobs.Remove(jobId);
                        }
                        finally
                        {
                            rwlock.ExitWriteLock();
                        }
                    }
                }

                return job;
            }
            finally
            {
                rwlock.ExitUpgradeableReadLock();
            }
        }

        public void RemoveJob(uint jobId)
        {
            try
            {
                rwlock.EnterWriteLock();

                Job j;
                if (jobs.TryGetValue(jobId, out j))
                {
                    if (!j.IsCompleted)
                    {
                        j.Cancel();
                    }
                }
                jobs.Remove(jobId);
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }

        private uint GetNextJobID()
        {
            ++jobIds;
            return jobIds;
        }
    }
}
