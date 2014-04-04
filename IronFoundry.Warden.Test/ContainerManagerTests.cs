using IronFoundry.Warden.Containers;
using NSubstitute;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class ContainerManagerTests
    {
        public class WhenDestoryingContainer : ContainerManagerTests
        {
            [Fact]
            public async void CallsContainerDestroyAsync()
            {
                var client = Substitute.For<IContainerClient>();
                var containerManager = new ContainerManager();
                client.Handle.Returns(new ContainerHandle("asdfghjkl"));
                containerManager.AddContainer(client);

                await containerManager.DestroyContainerAsync(client);

                client.Received(x => x.DestroyAsync());
            }

            [Fact]
            public async void ContainerShouldBeRemoved()
            {
                var client = Substitute.For<IContainerClient>();
                var containerManager = new ContainerManager();
                client.Handle.Returns(new ContainerHandle("asdfghjkl"));
                containerManager.AddContainer(client);
                await containerManager.DestroyContainerAsync(client);

                var destroyedClient = containerManager.GetContainer("asdfghjkl");

                Assert.Null(destroyedClient);
            }
        }
    }
}