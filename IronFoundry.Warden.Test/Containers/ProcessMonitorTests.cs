using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using IronFoundry.Container.Utilities;

namespace IronFoundry.Warden.Test
{

    public class ProcessMonitorTests
    {
        ProcessMonitor monitor;
        IProcess process;

        public ProcessMonitorTests()
        {
            monitor = new ProcessMonitor();
            process = Substitute.For<IProcess>();
        }

        [Fact]
        public void TryAddingProcessSucceeds()
        {
            Assert.True(monitor.TryAdd(process));
        }

        [Fact]
        public void AddedProcessReportsExistence()
        {
            monitor.TryAdd(process);

            Assert.True(monitor.HasProcess(process));
        } 

        [Fact]
        public void AddingSameProcessTwiceFails()
        {
            monitor.TryAdd(process);

            Assert.False(monitor.TryAdd(process));
        }

        [Fact]
        public void RemovesProcessReportsRemoved()
        {
            monitor.TryAdd(process);
            monitor.Remove(process);

            Assert.False(monitor.HasProcess(process));
        }

        [Fact]
        public void RemovesExitedProcesses()
        {
            monitor.TryAdd(process);

            process.Exited += Raise.Event();

            Assert.False(monitor.HasProcess(process));
        }

        [Fact]
        public void MonitorsOutputDataForAddedProcesses()
        {
            ProcessDataReceivedEventArgs capturedArgs = null;

            monitor.TryAdd(process);
            monitor.OutputDataReceived += (o, e) => {
                capturedArgs = e;
            };

            var args = new ProcessDataReceivedEventArgs("TestData");

            process.OutputDataReceived += Raise.EventWith(new object(), args);

            Assert.Same(args, capturedArgs);
        }

        [Fact]
        public void StopsMonitoringOutputDataForRemovedProcesses()
        {
            ProcessDataReceivedEventArgs capturedArgs = null;
            monitor.OutputDataReceived += (o, e) =>
            {
                capturedArgs = e;
            };

            monitor.TryAdd(process);
            monitor.Remove(process);

            var args = new ProcessDataReceivedEventArgs("TestData");

            process.OutputDataReceived += Raise.EventWith(new object(), args);

            Assert.Null(capturedArgs);
        }

        [Fact]
        public void MonitorsErrorDataForAddedProcesses()
        {
            ProcessDataReceivedEventArgs capturedArgs = null;

            monitor.TryAdd(process);
            monitor.ErrorDataReceived += (o, e) =>
            {
                capturedArgs = e;
            };

            var args = new ProcessDataReceivedEventArgs("TestData");

            process.ErrorDataReceived += Raise.EventWith(new object(), args);

            Assert.Same(args, capturedArgs);
        }

        [Fact]
        public void StopsMonitoringErrorDataForRemovedProcesses()
        {
            ProcessDataReceivedEventArgs capturedArgs = null;
            monitor.ErrorDataReceived += (o, e) =>
            {
                capturedArgs = e;
            };

            monitor.TryAdd(process);
            monitor.Remove(process);

            var args = new ProcessDataReceivedEventArgs("TestData");

            process.ErrorDataReceived += Raise.EventWith(new object(), args);

            Assert.Null(capturedArgs);
        }

    }
}
