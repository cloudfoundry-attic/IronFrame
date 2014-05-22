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

            [Fact]
            public async void DelegatesDestroyToContainerJanitor()
            {
                await containerManager.DestroyContainerAsync(new ContainerHandle("containerHandle"));
                
                janitor.Received(1, x => x.DestroyContainerAsync("containerHandle",config.ContainerBasePath, config.TcpPort.ToString(), config.DeleteContainerDirectories, null));
            }

            [Fact]
            public async void IncludesPortContainerIfAvailable()
            {
                var client = Substitute.For<IContainerClient>();
                client.AssignedPort.Returns(100);
                client.Handle.Returns(new ContainerHandle("asdfghjkl"));
                containerManager.AddContainer(client);

                await containerManager.DestroyContainerAsync(client);

                janitor.Received(1, x => x.DestroyContainerAsync("asdfghjkl", config.ContainerBasePath, config.TcpPort.ToString(), config.DeleteContainerDirectories, 100));
            }
        }
    }
}