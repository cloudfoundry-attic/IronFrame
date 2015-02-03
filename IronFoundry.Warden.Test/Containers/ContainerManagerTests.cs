using IronFoundry.Warden.Configuration;
using IronFoundry.Warden.Containers;
using NSubstitute;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class ContainerManagerTests
    {
        public class WhenDestoryingContainer : ContainerManagerTests
        {
            private readonly IContainerManager containerManager;
            private readonly IContainerJanitor janitor;
            private readonly IWardenConfig config;

            public WhenDestoryingContainer()
            {
                janitor = Substitute.For<IContainerJanitor>();
                config = Substitute.For<IWardenConfig>();
                config.ContainerBasePath.Returns("c:\\temp");
                config.DeleteContainerDirectories.Returns(false);
                config.TcpPort.Returns((ushort)5555);

                containerManager = new ContainerManager(janitor, config);
            }

            [Fact]
            public async void ContainerShouldBeRemoved()
            {
                var client = Substitute.For<IContainerClient>();
                client.Handle.Returns(new ContainerHandle("asdfghjkl"));
                containerManager.AddContainer(client);

                await containerManager.DestroyContainerAsync(client);
                var destroyedClient = containerManager.GetContainer("asdfghjkl");

                Assert.Null(destroyedClient);
            }
        }
    }
}