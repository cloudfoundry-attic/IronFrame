using System;
using System.Diagnostics;
using System.Threading;
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
            using(var jobObject = new JobObject("TestJobObjectName"))
            using(var otherJobObject = new JobObject("TestJobObjectName"))
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
        public void CanTerminateObjectsUnderJobObject()
        {
            JobObject jobObject = new JobObject();

            Process p = null;

            try
            {
                p = Process.Start("cmd.exe");

                jobObject.AssignProcessToJob(p);

                jobObject.TerminateProcesses();

                p.WaitForExit(1000);

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
                p = Process.Start("cmd.exe");

                jobObject.AssignProcessToJob(p);

                jobObject.TerminateProcessesAndWait(1000);

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

        public class WhenManagingNoProcesses : IDisposable
        {
            JobObject jobObject;

            public WhenManagingNoProcesses()
            {
                jobObject = new JobObject();
            }

            public void Dispose()
            {
                jobObject.Dispose();
            }

            [Fact]
            public void ReturnsDefaultCpuStatistics()
            {
                var stats = jobObject.GetCpuStatistics();

                Assert.Equal(TimeSpan.Zero, stats.TotalKernelTime);
                Assert.Equal(TimeSpan.Zero, stats.TotalUserTime);
            }

            [Fact]
            public void ReturnsEmptyListOfProcesses()
            {
                var processIds = jobObject.GetProcessIds();

                Assert.Empty(processIds);
            }
        }

        public class WhenManagingOneProcess : IDisposable
        {
            JobObject jobObject;
            Process process;

            public WhenManagingOneProcess()
            {
                jobObject = new JobObject();

                var batch = @"for /L %i in (1,1,10000000) do @echo %i";
                process = Process.Start("cmd.exe", "/K " + batch);

                jobObject.AssignProcessToJob(process);
            }

            public void Dispose()
            {
                process.Kill();
                jobObject.Dispose();
            }

            [Fact(Skip = "Success is inconsistent on this test, review.")]
            public void ReturnsCpuStatistics()
            {
                // Give the process some time to execute
                Thread.Sleep(500);

                var stats = jobObject.GetCpuStatistics();

                Assert.NotEqual(TimeSpan.Zero, stats.TotalKernelTime + stats.TotalUserTime);
            }

            [Fact]
            public void ReturnsProcess()
            {
                var processIds = jobObject.GetProcessIds();

                Assert.Collection(processIds,
                    x => Assert.Equal(process.Id, x)
                );
            }
        }

        public class WhenManagingMultipleProcesses : IDisposable
        {
            JobObject jobObject;
            Process[] processes;

            public WhenManagingMultipleProcesses()
            {
                jobObject = new JobObject();

                var batch = @"for /L %i in (1,1,100) do @echo %i";

                processes = new[]
                {
                    Process.Start("cmd.exe", "/K " + batch),
                    Process.Start("cmd.exe", "/K " + batch),
                    Process.Start("cmd.exe", "/K " + batch),
                    Process.Start("cmd.exe", "/K " + batch),
                    Process.Start("cmd.exe", "/K " + batch),
                    Process.Start("cmd.exe", "/K " + batch),
                };

                foreach (var process in processes)
                    jobObject.AssignProcessToJob(process);
            }

            public void Dispose()
            {
                foreach (var process in processes)
                    process.Kill();

                jobObject.Dispose();
            }

            [Fact(Skip = "Success is inconsistent on this test, review.")]
            public void ReturnsCpuStatistics()
            {
                // Give the processes some time to execute
                Thread.Sleep(250);

                var stats = jobObject.GetCpuStatistics();

                Assert.NotEqual(TimeSpan.Zero, stats.TotalKernelTime + stats.TotalUserTime);
            }

            [Fact]
            public void ReturnsProcesses()
            {
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
