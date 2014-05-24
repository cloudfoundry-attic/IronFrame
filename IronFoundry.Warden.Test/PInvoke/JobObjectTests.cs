using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IronFoundry.Warden.Containers
{
    public class JobObjectTests
    {
        [Fact]
        public void CreatingJobObjectGetsValidHandle()
        {
            var jobObject = new JobObject();
            Assert.False(jobObject.Handle.IsInvalid);
        }

        [Fact]
        public void CreatingJobObjectBySameNameGivesEquivalentJobObject()
        {
            using (var jobObject = new JobObject("TestJobObjectName"))
            using (var otherJobObject = new JobObject("TestJobObjectName"))
            using (Process p = Process.Start("cmd.exe"))
            {
                jobObject.AssignProcessToJob(p);

                // Since there is no way to compare two JobObjects directly, we
                // assume equivalency by their content.
                var processIds = jobObject.GetProcessIds();
                var otherProcessids = otherJobObject.GetProcessIds();

                p.Kill();

                Assert.Equal(processIds, otherProcessids);
            }
        }

        [Fact]
        public void OpeningNonExistingJobObjectThrows()
        {
            var ex = Record.Exception(() => { var otherJobObject = new JobObject("TestJobObjectName", true); });
            Assert.NotNull(ex);
        }

        [Fact]
        public void DisposingJobObjectCleansUpHandle()
        {
            JobObject jobObject = null;
            jobObject = new JobObject();

            jobObject.Dispose();

            Assert.Null(jobObject.Handle);
        }

        [Fact]
        public void CanAssignProcessToJobObject()
        {
            JobObject jobObject = new JobObject();

            Process p = null;
            try
            {
                p = Process.Start("cmd.exe");

                jobObject.AssignProcessToJob(p);

                bool isInJob;
                IronFoundry.Warden.PInvoke.NativeMethods.IsProcessInJob(p.Handle, jobObject.Handle, out isInJob);
                Assert.True(isInJob);
            }
            finally
            {
                p.Kill();
            }
        }

        [Fact]
        public void ClosingLastHandleKillsProcess()
        {
            JobObject jobObject = new JobObject();

            Process p = null;
            try
            {
                p = Process.Start("cmd.exe");
                jobObject.AssignProcessToJob(p);

                jobObject.Dispose();

                p.WaitForExit(2000);
                Assert.True(p.HasExited);
            }
            finally
            {
                if (!p.HasExited)
                    p.Kill();
            }
        }

        [Fact]
        public void CanTerminateObjectsUnderJobObject()
        {
            JobObject jobObject = new JobObject();

            Process p = null;

            try
            {
                p = IFTestHelper.ExecuteWithWait("nop");
                jobObject.AssignProcessToJob(p);

                jobObject.TerminateProcesses();

                IFTestHelper.ContinueAndWait(p, timeout: 1000);
                Assert.True(p.HasExited);
            }
            finally
            {
                if (!p.HasExited)
                    p.Kill();
            }
        }

        [Fact]
        public void CanTerminateAndWaitForObjectsUnderJobObject()
        {
            JobObject jobObject = new JobObject();

            Process p = null;
            try
            {
                p = IFTestHelper.ExecuteWithWait("nop");
                jobObject.AssignProcessToJob(p);

                jobObject.TerminateProcessesAndWait(1000);

                IFTestHelper.ContinueAndWait(p, timeout: 1000);
                Assert.True(p.HasExited);
            }
            finally
            {
                if (!p.HasExited)
                    p.Kill();
            }
        }

        [Fact]
        public void TerminateThrowsIfJobObjectIsDisposed()
        {
            JobObject jobObject = new JobObject();
            jobObject.Dispose();

            Assert.Throws<ObjectDisposedException>(() => jobObject.TerminateProcesses());
        }

        public class JobLimits : IDisposable
        {
            const ulong DefaultMemoryLimit = 1024 * 1024 * 25; // 25MB

            JobObject jobObject;

            public JobLimits()
            {
                jobObject = new JobObject();
            }

            public void Dispose()
            {
                jobObject.Dispose();
            }

            [Fact]
            public void CanLimitMemory()
            {
                ulong limitInBytes = DefaultMemoryLimit;
                ulong allocateBytes = limitInBytes * 2;

                jobObject.SetJobMemoryLimit(limitInBytes);

                var process = IFTestHelper.ExecuteInJob(jobObject, "allocate-memory", "--bytes", allocateBytes);

                Assert.True(IFTestHelper.Failed(process));
            }

            [Fact]
            public void CanGetMemoryLimit()
            {
                jobObject.SetJobMemoryLimit(DefaultMemoryLimit);

                Assert.Equal(DefaultMemoryLimit, jobObject.GetJobMemoryLimit());
            }

            [Fact]
            public void WhenMemoryIsNotLimited_GetMemoryLimitReturnsZero()
            {
                Assert.Equal(0UL, jobObject.GetJobMemoryLimit());
            }

            [Fact]
            public void CanGetMemoryPeak()
            {
                jobObject.SetJobMemoryLimit(DefaultMemoryLimit);

                ulong allocateBytes = DefaultMemoryLimit * 2;
                var process = IFTestHelper.ExecuteInJob(jobObject, "allocate-memory", "--bytes", allocateBytes);

                if (IFTestHelper.Succeeded(process))
                    Assert.True(DefaultMemoryLimit <= jobObject.GetPeakJobMemoryUsed());
                else
                    Assert.NotEqual(0UL, jobObject.GetPeakJobMemoryUsed());
            }

            [Fact]
            public void WhenMemoryIsNotLimited_GetMemoryPeakReturnsZero()
            {
                ulong allocateBytes = DefaultMemoryLimit * 2;
                IFTestHelper.ExecuteInJob(jobObject, "allocate-memory", "--bytes", allocateBytes);

                Assert.Equal(0UL, jobObject.GetPeakJobMemoryUsed());
            }
        }

        public class CpuStatistics : IDisposable
        {
            JobObject jobObject;

            public CpuStatistics()
            {
                jobObject = new JobObject();
            }

            public void Dispose()
            {
                jobObject.Dispose();
            }

            [Fact]
            public void WhenNotManagingProcesses_ReturnsDefaultCpuStatistics()
            {
                var stats = jobObject.GetCpuStatistics();

                Assert.Equal(TimeSpan.Zero, stats.TotalKernelTime);
                Assert.Equal(TimeSpan.Zero, stats.TotalUserTime);
            }

            [Fact]
            public void WhenManagingOneProcess_ReturnsCpuStatistics()
            {
                IFTestHelper.ExecuteInJob(jobObject, "consume-cpu", "--duration 250");

                var stats = jobObject.GetCpuStatistics();

                Assert.NotEqual(TimeSpan.Zero, stats.TotalKernelTime + stats.TotalUserTime);
            }
        }

        public class ProcessIds : IDisposable
        {
            JobObject jobObject;
            Process[] processes;

            public ProcessIds()
            {
                jobObject = new JobObject();
            }

            public void Dispose()
            {
                if (processes != null)
                {
                    foreach (var process in processes)
                    {
                        IFTestHelper.ContinueAndWait(process);
                    }
                }

                jobObject.Dispose();
            }

            [Fact]
            public void WhenNotManagingProcesses_ReturnsEmptyListOfProcessIds()
            {
                var processIds = jobObject.GetProcessIds();

                Assert.Empty(processIds);
            }

            [Fact]
            public void WhenManagingOneProcess_ReturnsSingleProcessId()
            {
                processes = new[]
                {
                    IFTestHelper.ExecuteWithWait("nop"),
                };

                jobObject.AssignProcessToJob(processes[0]);

                var processIds = jobObject.GetProcessIds();

                Assert.Collection(processIds,
                    x => Assert.Equal(processes[0].Id, x)
                );
            }

            [Fact]
            public void WhenManagingManyProcesses_ReturnsAllProcessIds()
            {
                processes = new[]
                {
                    IFTestHelper.ExecuteWithWait("nop"),
                    IFTestHelper.ExecuteWithWait("nop"),
                    IFTestHelper.ExecuteWithWait("nop"),
                    IFTestHelper.ExecuteWithWait("nop"),
                    IFTestHelper.ExecuteWithWait("nop"),
                    IFTestHelper.ExecuteWithWait("nop"),
                };

                foreach (var process in processes)
                    jobObject.AssignProcessToJob(process);

                var processIds = jobObject.GetProcessIds();

                Assert.Collection(processIds,
                    x => Assert.Equal(processes[0].Id, x),
                    x => Assert.Equal(processes[1].Id, x),
                    x => Assert.Equal(processes[2].Id, x),
                    x => Assert.Equal(processes[3].Id, x),
                    x => Assert.Equal(processes[4].Id, x),
                    x => Assert.Equal(processes[5].Id, x)
                );
            }
        }
    }
}
