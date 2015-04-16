using System;
using IronFoundry.Container;
using IronFoundry.Container.Messaging;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Handlers;
using IronFoundry.Warden.Jobs;
using IronFoundry.Warden.Protocol;
using NSubstitute;
using Xunit;

namespace IronFoundry.Warden.Test.Handlers
{
    public class LinkRequestHandlerTests
    {
        private IContainerClient containerClient;
        private LinkRequestHandler handler;
        private Jobs.IJobManager jobManager;

        public LinkRequestHandlerTests()
        {
            containerClient = Substitute.For<IContainerClient>();
            containerClient.GetInfoAsync().ReturnsTask(new ContainerInfo());

            var containerManager = Substitute.For<IContainerManager>();
            containerManager.GetContainer("abcd1234").Returns(containerClient);

            jobManager = Substitute.For<Jobs.IJobManager>();

            var request = new LinkRequest()
            {
                Handle = "abcd1234",
                JobId = 1,
            };

            handler = new LinkRequestHandler(containerManager, jobManager, request);
        }

        [Fact]
        public async void MissingJobObjectReturnsNoSuchJobMessage()
        {
            jobManager.GetJob(1).Returns((Job)null);

            var response = (LinkResponse)await handler.HandleAsync();

            Assert.Equal((uint)1, response.ExitStatus);
            Assert.Equal("no such job\n", response.Stderr);
        }

        [Fact]
        public async void WhenJobCompletesReturnsJobResults()
        {
            var jobRunnable = Substitute.For<IJobRunnable>();
            var job = Substitute.For<Jobs.Job>((uint)1, jobRunnable);
            jobManager.GetJob(1).Returns(job);
            var jobResult = Substitute.For<IJobResult>();
            jobResult.ExitCode.Returns(0);
            jobResult.Stdout.Returns("standard output stream");
            jobResult.Stderr.Returns("standard error stream");

            job.RunnableTask.ReturnsTask(jobResult);

            var response = (LinkResponse) await handler.HandleAsync();

            Assert.Equal(0, (int)response.ExitStatus);
            Assert.Equal("standard output stream", response.Stdout);
            Assert.Equal("standard error stream", response.Stderr);
        }

        [Fact]
        public async void ReturnsInfoFromContainerClient()
        {
            var info = new ContainerInfo()
            {
                ContainerPath = "c://some//path",
                ContainerIPAddress = "127.0.0.1",
                State = ContainerState.Active,
            };

            info.MemoryStat.PrivateBytes = 1024 * 1024 * 1024;
            info.CpuStat.TotalProcessorTime = new TimeSpan(1000000);

            containerClient.GetInfoAsync().ReturnsTask(info);

            var response = (LinkResponse)await handler.HandleAsync();

            Assert.Equal(info.State.ToString(), response.Info.State);
            Assert.Equal(info.ContainerIPAddress, response.Info.ContainerIp);
            Assert.Equal(info.ContainerPath, response.Info.ContainerPath);
            Assert.Equal(info.ContainerPath, response.Info.ContainerPath);
            Assert.Equal(info.MemoryStat.PrivateBytes, response.Info.MemoryStatInfo.TotalRss);
            Assert.Equal((ulong)info.CpuStat.TotalProcessorTime.Ticks * 100, response.Info.CpuStatInfo.Usage);
        }

        [Fact]
        public async void ReturnsStoppedInfoRequestWhenContainerThrowsOperationCancelled()
        {
            containerClient.GetInfoAsync().ThrowsTask(new OperationCanceledException());

            var response = (LinkResponse)await handler.HandleAsync();

            Assert.Equal("Stopped", response.Info.State);
        }

        [Fact]
        public async void ReturnsStoppedInfoRequestWhenContainerThrowsMessagingException()
        {
            containerClient.GetInfoAsync().ThrowsTask(new Exception());
            
            var response = (LinkResponse)await handler.HandleAsync();

            Assert.Equal("Stopped", response.Info.State);
        }
    }
}
