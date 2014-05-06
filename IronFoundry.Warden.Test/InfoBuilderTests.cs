using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Shared.Data;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class InfoBuilderTests
    {
        private IContainerClient container;
        private InfoBuilder builder;

        public InfoBuilderTests()
        {
            container = Substitute.For<IContainerClient>();
            container.ContainerDirectoryPath.Returns("ContainerPath");
            container.State.Returns(ContainerState.Active);

            ProcessStats stats = new ProcessStats();
            stats.TotalProcessorTime = new TimeSpan(1000000);
            stats.PrivateMemory = 1024 * 1024;

            container.GetProcessStatisticsAsync().Returns(Task.FromResult(stats));

            builder = new InfoBuilder(container);
        }

        [Fact]
        public async void ResponseIncludesEvents()
        {
            var events = new[] { "One", "Two", "Three" };
            container.DrainEvents().Returns(events);

            var response = await builder.GetInfoResponseAsync();

            Assert.Equal(events, response.Events);
        }

        [Fact]
        public async void ResponseIncludesContainerPath()
        {
            var response = await builder.GetInfoResponseAsync();

            Assert.Equal("ContainerPath", response.ContainerPath);
        }

        [Fact]
        public async void ResponseIncludesIPInfo()
        {
            var response = await builder.GetInfoResponseAsync();

            var localIp = GetLocalIPAddress();

            Assert.Equal(localIp.ToString(), response.HostIp);
            Assert.Equal(localIp.ToString(), response.ContainerIp);
        }

        [Fact]
        public async void ResponseIncludesCPUUsageStats()
        {
            var response = await builder.GetInfoResponseAsync();

            var processorTimeInNanoseconds = 1000000 * 100;
            Assert.Equal((ulong)processorTimeInNanoseconds, response.CpuStatInfo.Usage);
        }

        [Fact]
        public async void ResponseIncludesMemoryTotalRssStats()
        {
            var response = await builder.GetInfoResponseAsync();

            var expectedMemory = 1024 * 1024;
            Assert.Equal((ulong)expectedMemory, response.MemoryStatInfo.TotalRss);
        }

        private IPAddress GetLocalIPAddress()
        {
            var address = Dns.GetHostAddresses(Dns.GetHostName());
            return address.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        }
    }
}
