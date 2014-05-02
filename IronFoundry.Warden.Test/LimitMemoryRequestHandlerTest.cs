using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Protocol;
using NSubstitute;
using Xunit;

namespace IronFoundry.Warden.Handlers
{
    public class LimitMemoryRequestHandlerTest
    {
        const ulong GB = 1024 * 1024 * 1024;

        public class WhenRequestIsValid : LimitMemoryRequestHandlerTest
        {
            LimitMemoryRequest request;

            public WhenRequestIsValid()
            {
                request = new LimitMemoryRequest
                {
                    Handle = "handle",
                    LimitInBytes = GB,
                    LimitInBytesSpecified = true,
                };
            }

            [Fact]
            public async void InvokesContainer()
            {
                var container = Substitute.For<IContainerClient>();
                var containerManager = Substitute.For<IContainerManager>();
                containerManager.GetContainer(null).ReturnsForAnyArgs(container);

                var handler = new LimitMemoryRequestHandler(containerManager, request);

                await handler.HandleAsync();

                container.Received(1, x => x.LimitMemoryAsync(GB));
            }

            [Fact]
            public async void ReturnsValidResponse()
            {
                var container = Substitute.For<IContainerClient>();
                var containerManager = Substitute.For<IContainerManager>();
                containerManager.GetContainer(null).ReturnsForAnyArgs(container);

                var handler = new LimitMemoryRequestHandler(containerManager, request);

                var response = await handler.HandleAsync();

                Assert.NotNull(response);
                var limitMemoryResponse = Assert.IsType<LimitMemoryResponse>(response);
                Assert.Equal(GB, limitMemoryResponse.LimitInBytes);
            }
        }
    }
}
