using System.Security.Policy;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Handlers;
using IronFoundry.Warden.Protocol;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFoundry.Warden.Protocol
{
    public class LoggingRequestHandlerTest
    {
        [Fact]
        public async void WhenRequestingLoggingProvidesLoggregatorEmitter()
        {
            var containerManager = Substitute.For<IContainerManager>();
            var container = Substitute.For<IContainerClient>();
            containerManager.GetContainer("abc123").Returns(container);

            var request = new LoggingRequest()
            {
                ApplicationId = "1",
                Handle = "abc123",
                InstanceIndex = "999",
                LoggregatorRouter = "127.0.0.1:5555",
                LoggregatorSecret = "RotoSecret",
            };

            request.DrainUris.Add("http://liquiddraino");

            var handler = new LoggingRequestHandler(containerManager, request);

            await handler.HandleAsync();

            container.Received(x => x.EnableLogging(Arg.Is<ILogEmitter>(e => e.GetType() == typeof(ContainerLogEmitter))));
        }
    }
}
