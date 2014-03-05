using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Utilities;
using Xunit;
using NSubstitute;

namespace IronFoundry.Warden.Test
{
    public class ProcessManagerTests
    {

        [Fact]
        public void WhenManagingNoProcess_ReturnsDefaultInitializedStats()
        {
            var manager = new ProcessManager("TestUser");
            var stats = manager.GetStats();

            Assert.Equal(new TimeSpan(0), stats.TotalProcessorTime);
        }

        [Fact]
        public void WhenManagingOneProcess_ReturnsTotalProcessorTime()
        {
            var manager = new ProcessManager("TestUser");
            var expectedTimepan = new TimeSpan(1500);
            
            var mockProcess = Substitute.For<IProcess>();            
            mockProcess.TotalProcessorTime.Returns(expectedTimepan);

            manager.AddProcess(mockProcess);

            var stats = manager.GetStats();

            Assert.Equal(expectedTimepan, stats.TotalProcessorTime);
        }

        [Fact]
        public void WhenManagingMultipleProcesses_ReturnsAggregateTotalProcessorTime()
        {
            var manager = new ProcessManager("TestUser");
            var expectedTimepan = new TimeSpan(1500);

            var firstProcess = Substitute.For<IProcess>();
            firstProcess.TotalProcessorTime.Returns(expectedTimepan);
            firstProcess.Id.Returns(0);

            var secondProcess = Substitute.For<IProcess>();
            secondProcess.TotalProcessorTime.Returns(expectedTimepan);
            secondProcess.Id.Returns(1);

            manager.AddProcess(firstProcess);
            manager.AddProcess(secondProcess);

            var stats = manager.GetStats();

            Assert.Equal(firstProcess.TotalProcessorTime + secondProcess.TotalProcessorTime, stats.TotalProcessorTime);
        }

        [Fact]
        public void WhenManagingOneProcess_ReturnsTotalUserProcessorTime()
        {
            var manager = new ProcessManager("TestUser");
            var expectedTimepan = new TimeSpan(1500);

            var mockProcess = Substitute.For<IProcess>();
            mockProcess.TotalUserProcessorTime.Returns(expectedTimepan);

            manager.AddProcess(mockProcess);

            var stats = manager.GetStats();

            Assert.Equal(expectedTimepan, stats.TotalUserProcessorTime);
        }

        [Fact]
        public void WhenManagingMultipleProcesses_ReturnsAggregateTotalUserProcessorTime()
        {
            var manager = new ProcessManager("TestUser");
            var expectedTimepan = new TimeSpan(1500);

            var firstProcess = Substitute.For<IProcess>();
            firstProcess.TotalUserProcessorTime.Returns(expectedTimepan);
            firstProcess.Id.Returns(0);

            var secondProcess = Substitute.For<IProcess>();
            secondProcess.TotalUserProcessorTime.Returns(expectedTimepan);
            secondProcess.Id.Returns(1);

            manager.AddProcess(firstProcess);
            manager.AddProcess(secondProcess);

            var stats = manager.GetStats();

            Assert.Equal(firstProcess.TotalUserProcessorTime + secondProcess.TotalUserProcessorTime, stats.TotalUserProcessorTime);
        }
    }
}
