using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronFrame.Win32;
using Xunit;

namespace IronFrame.Utilities
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
                NativeMethods.IsProcessInJob(p.Handle, jobObject.Handle, out isInJob);
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

        [Fact]
        public void SetActiveProcessLimit_StopsForkBomb()
        {
            using (var jobObject = new JobObject())
            {
                jobObject.SetActiveProcessLimit(2);

                var process = IFTestHelper.ExecuteWithWait("fork-bomb");
                jobObject.AssignProcessToJob(process);
                IFTestHelper.Continue(process);
                process.WaitForExit(1000);
                var hasExited = process.HasExited;
                if(!hasExited) process.Kill();
                if (!hasExited)
                {
                    Console.WriteLine(process.StandardOutput.ReadToEnd());
                    Console.Error.WriteLine(process.StandardError.ReadToEnd());
                }
                Assert.True(hasExited, "Active process limit was not enforced");
            }
        }

        public class JobMemoryLimits : IDisposable
        {
            const ulong DefaultMemoryLimit = 1024 * 1024 * 25; // 25MB

            JobObject jobObject;

            public JobMemoryLimits()
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

        public class JobCpuLimits : IDisposable
        {
            JobObject jobObject;
            JobObject jobObject2;


            public JobCpuLimits()
            {
                jobObject = new JobObject();
                jobObject2 = new JobObject();
            }

            public void Dispose()
            {
                jobObject.Dispose();
                jobObject2.Dispose();
            }

            [Fact]
            public void CanLimitCpu()
            {
                jobObject.SetJobCpuLimit(1 * 100);
                jobObject2.SetJobCpuLimit(10 * 100);

                var thread1 = new Thread(() =>
                {
                    IFTestHelper.ExecuteInJob(jobObject, "consume-cpu", "--duration", "2000").WaitForExit();
                });
                var thread2 = new Thread(() =>
                {
                    IFTestHelper.ExecuteInJob(jobObject2, "consume-cpu", "--duration", "2000").WaitForExit();
                });

                thread1.Start();
                thread2.Start();
                thread1.Join();
                thread2.Join();

                var ratio = (float)jobObject.GetCpuStatistics().TotalUserTime.Ticks / jobObject2.GetCpuStatistics().TotalUserTime.Ticks;
                Assert.InRange(ratio, 0.01, 0.4);
            }

            [Fact(Skip = "Fails intermittently on appveyor")]
            public void CanSetPriority()
            {
                jobObject.SetPriorityClass(ProcessPriorityClass.Idle);
                jobObject2.SetPriorityClass(ProcessPriorityClass.AboveNormal);

                var thread1 = new Thread(() =>
                {
                    IFTestHelper.ExecuteInJob(jobObject, "consume-cpu", "--duration", "4000").WaitForExit();
                });
                var thread2 = new Thread(() =>
                {
                    IFTestHelper.ExecuteInJob(jobObject2, "consume-cpu", "--duration", "4000").WaitForExit();
                });

                thread1.Start();
                thread2.Start();
                thread1.Join();
                thread2.Join();

                var ratio = (float)jobObject.GetCpuStatistics().TotalUserTime.Ticks / jobObject2.GetCpuStatistics().TotalUserTime.Ticks;
                Assert.InRange(ratio, 0.01, 0.5);
            }

            [Fact]
            public void CanGetCpuLimit()
            {
                jobObject.SetJobCpuLimit(3);
                Assert.Equal(3, jobObject.GetJobCpuLimit());
            }

            [Fact]
            public void ThrowsOnInvalidCpuWeights()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => jobObject.SetJobCpuLimit(0));
                Assert.Throws<ArgumentOutOfRangeException>(() => jobObject.SetJobCpuLimit(100 * 100 + 1));
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

            [Fact]
            public void WhenManagedProcessCreatesChildProcess_ReturnsAllProcessIds()
            {
                // Arrange:
                var process = IFTestHelper.ExecuteWithWait("create-child");
                jobObject.AssignProcessToJob(process);
                process.StandardInput.WriteLine();

                var pidLine = process.StandardOutput.ReadLine();
                int pid = Int32.Parse(pidLine);

                // Act:
                var managedProcesses = jobObject.GetProcessIds();

                // Assert:
                Assert.Contains(process.Id, managedProcesses);
                Assert.Contains(pid, managedProcesses);

                process.StandardInput.WriteLine();

                process.WaitForExit();
                Assert.True(IFTestHelper.Succeeded(process));
            }
        }

        public class ClipboardTests : IDisposable
        {
            JobObject jobObject;

            public ClipboardTests()
            {
                jobObject = new JobObject();
            }

            public void Dispose()
            {
                jobObject.Dispose();
                IFTestHelper.Execute("write-clipboard").WaitForExit();
            }

            [Fact(Skip = "Awaiting 'Windows Station'")]
            public void ClipboardIsDisabledForWrites()
            {
                var proc = IFTestHelper.ExecuteInJob(jobObject, "write-clipboard", "Text from JobObject1");
                proc.WaitForExit();
                var output = proc.StandardOutput.ReadToEnd().Trim();
                Assert.Contains("Could not write to clipboard", output);
            }

            [Fact(Skip = "Awaiting 'Windows Station'")]
            public void ClipboardIsDisabledForReads()
            {
                var clipboardText = "Text From Test";
                IFTestHelper.Execute("write-clipboard", clipboardText).WaitForExit();

                var proc = IFTestHelper.Execute("read-clipboard");
                proc.WaitForExit();
                Assert.Equal(clipboardText, proc.StandardOutput.ReadToEnd().Trim());

                proc = IFTestHelper.ExecuteInJob(jobObject, "read-clipboard");
                proc.WaitForExit();
                Assert.DoesNotContain(clipboardText, proc.StandardOutput.ReadToEnd().Trim());
            }
        }
    }
}
