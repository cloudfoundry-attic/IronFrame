using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Handlers;
using IronFoundry.Warden.Protocol;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class LoggingRequestHandlerTest
    {

        [Fact]
        public async void WhenHandlingReturnsValidRequest()
        {
            var container = Substitute.For<IContainerClient>();
            var containerManager = Substitute.For<IContainerManager>();
            containerManager.GetContainer(null).ReturnsForAnyArgs(container);

            var handler = new LoggingRequestHandler(containerManager, new LoggingRequest());

            var response = await handler.HandleAsync();

            Assert.NotNull(response);
        }

        [Fact]
        public async void WhenHandlingValidRequestInvokesContainer()
        {
            var container = Substitute.For<IContainerClient>();
            var containerManager = Substitute.For<IContainerManager>();
            containerManager.GetContainer(null).ReturnsForAnyArgs(container);

            var handler = new LoggingRequestHandler(containerManager, new LoggingRequest());

            var response = await handler.HandleAsync();

            container.Received(x => x.EnableLoggingAsync(Arg.Any<InstanceLoggingInfo>()));
        }

        [Fact]
        public async void WhenHandlingValidRequestTranslatesApplicationData()
        {
            var container = Substitute.For<IContainerClient>();
            var containerManager = Substitute.For<IContainerManager>();
            containerManager.GetContainer(null).ReturnsForAnyArgs(container);

            var requestData = new LoggingRequest()
            {
                Handle = "ContainerHandle",
                ApplicationId = "ApplicationId",
                InstanceIndex = "11",
                LoggregatorRouter = "LoggregatorRouter",
                LoggregatorSecret = "Secret"
            };
            requestData.DrainUris.Add("firstdrainurl");

            var handler = new LoggingRequestHandler(containerManager, requestData);

            var response = await handler.HandleAsync();

            container.Received(x => x.EnableLoggingAsync(Arg.Is<InstanceLoggingInfo>(info => info.InstanceIndex == requestData.InstanceIndex)));
            container.Received(x => x.EnableLoggingAsync(Arg.Is<InstanceLoggingInfo>(info => info.ApplicationId == requestData.ApplicationId)));
            container.Received(x => x.EnableLoggingAsync(Arg.Is<InstanceLoggingInfo>(info => info.LoggregatorAddress == requestData.LoggregatorRouter)));
            container.Received(x => x.EnableLoggingAsync(Arg.Is<InstanceLoggingInfo>(info => info.LoggregatorSecret == requestData.LoggregatorSecret)));

        }
        
        [Fact]
        public async void WhenHandlingValidRequestTranslatesDrainUrls()
        {
            var container = Substitute.For<IContainerClient>();
            var containerManager = Substitute.For<IContainerManager>();
            containerManager.GetContainer(null).ReturnsForAnyArgs(container);

            var requestData = new LoggingRequest()
            {
                Handle = "ContainerHandle",
                ApplicationId = "ApplicationId",
                InstanceIndex = "11",
                LoggregatorRouter = "LoggregatorRouter",
                LoggregatorSecret = "Secret"
            };
            requestData.DrainUris.Add("firstdrainurl");
            requestData.DrainUris.Add("seconddrainurl");

            var handler = new LoggingRequestHandler(containerManager, requestData);

            var response = await handler.HandleAsync();

            container.Received(x => x.EnableLoggingAsync(Arg.Is<InstanceLoggingInfo>(info => info.DrainUris[0] == requestData.DrainUris[0])));
            container.Received(x => x.EnableLoggingAsync(Arg.Is<InstanceLoggingInfo>(info => info.DrainUris[1] == requestData.DrainUris[1])));
        }
    }
}
