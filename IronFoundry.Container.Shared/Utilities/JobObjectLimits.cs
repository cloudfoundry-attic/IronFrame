using System;
using System.Threading;

namespace IronFoundry.Container.Utilities
{
    // BR: Move this to IronFoundry.Container.Shared
    public class JobObjectLimits : IDisposable
    {
        static readonly TimeSpan DefaultPollPeriod = TimeSpan.FromSeconds(1);

        readonly JobObject jobObject;
        ulong? lastMemoryPeak;
        ulong? lastMemoryLimit;
        readonly object limitLock = new object();
        Timer limitTimer;
        EventHandler memoryLimitReachedEvent;
        readonly TimeSpan pollPeriod;

        public JobObjectLimits(JobObject jobObject, TimeSpan? pollPeriod = null)
        {
            this.jobObject = jobObject;
            this.pollPeriod = pollPeriod ?? DefaultPollPeriod;
        }

        public virtual event EventHandler MemoryLimitReached
        {
            add { memoryLimitReachedEvent += value; }
            remove { memoryLimitReachedEvent -= value; }
        }

        void CheckLimits(object state)
        {
            lock (limitLock)
            {
                var currentMemoryPeak = jobObject.GetPeakJobMemoryUsed();
                var currentMemoryLimit = jobObject.GetJobMemoryLimit();

                bool valueHasChanged = !lastMemoryPeak.HasValue || currentMemoryPeak != lastMemoryPeak.Value;
                bool limitHasChanged = !lastMemoryLimit.HasValue || currentMemoryLimit != lastMemoryLimit.Value;

                try
                {
                    if (valueHasChanged || limitHasChanged)
                    {
                        if (currentMemoryPeak >= currentMemoryLimit)
                            OnMemoryLimitReached();
                    }
                }
                finally
                {
                    lastMemoryPeak = currentMemoryPeak;
                    lastMemoryLimit = currentMemoryLimit;
                }
            }
        }

        public void Dispose()
        {
            if (limitTimer != null)
            {
                limitTimer.Dispose();
                limitTimer = null;
            }
        }

        void EnsureMonitorLimits()
        {
            if (limitTimer == null)
                limitTimer = new Timer(CheckLimits, null, pollPeriod, pollPeriod);
        }

        public virtual void LimitMemory(ulong jobMemoryLimitInBytes)
        {
            jobObject.SetJobMemoryLimit(jobMemoryLimitInBytes);
            EnsureMonitorLimits();
        }

        void OnMemoryLimitReached()
        {
            var handler = memoryLimitReachedEvent;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
