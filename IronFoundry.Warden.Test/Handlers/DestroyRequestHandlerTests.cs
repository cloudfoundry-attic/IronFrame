using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Protocol;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFoundry.Warden.Handlers
{
    public class DestroyRequestHandlerTests
    {
        private readonly IContainerManager manager;
        private readonly IContainerClient container;

        public DestroyRequestHandlerTests()
        {
            manager = Substitute.For<IContainerManager>();
            container = Substitute.For<IContainerClient>();

            manager.GetContainer("containerHandle").Returns(container);
        }

        [Fact]
        public async void RequestsDestroyContainerFromManager()
        {
            container.GetInfoAsync().ReturnsTask(new Containers.Messages.ContainerInfo() { State = IronFoundry.Warden.Containers.Messages.ContainerState.Active });

            var request = new DestroyRequest()
            {
                Handle = "containerHandle",
            };

            var handler = new DestroyRequestHandler(manager, request);

            var response = await handler.HandleAsync();
            manager.Received(1, x => x.DestroyContainerAsync(Arg.Is<ContainerHandle>(h => h.ToString() == "containerHandle")));
        }
    }
}
