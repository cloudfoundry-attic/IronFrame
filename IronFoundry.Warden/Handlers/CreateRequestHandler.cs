using IronFoundry.Container;
using IronFoundry.Container.Utilities;

namespace IronFoundry.Warden.Handlers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Containers;
    using IronFoundry.Warden.Configuration;
    using IronFoundry.Warden.Containers.Messages;
    using NLog;
    using Protocol;

    // MO: Added to ContainerClient
    public class CreateRequestHandler : ContainerRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly CreateRequest request;

        public CreateRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
            this.request = (CreateRequest)request;
        }

        static IEnumerable<IronFoundry.Container.BindMount> GetBindMounts(CreateRequest request)
        {
            return request.BindMounts.Select(
                x => new IronFoundry.Container.BindMount
                {
                    SourcePath = x.SrcPath,
                    DestinationPath = x.DstPath,
                    Access = x.BindMountMode == CreateRequest.BindMount.Mode.RW ? FileAccess.ReadWrite : FileAccess.Read,
                })
                .ToList();
        }

        public override Task<Response> HandleAsync()
        {
            return Task.Run<Response>(async () =>
                {
                    var config = new WardenConfig();
                    var handle = new ContainerHandle();

                    var containerService = new ContainerService(
                        config.ContainerBasePath,
                        config.WardenUsersGroup);

                    var containerSpec = new ContainerSpec
                    {
                        Handle = handle,
                        BindMounts = GetBindMounts(request).ToArray(),
                    };

                    var container = containerService.CreateContainer(containerSpec);
                    var containerClient = new ContainerClient(containerService, container, new FileSystemManager());
                    await containerClient.InitializeAsync(null, null, null);

                    containerManager.AddContainer(containerClient);

                    return new CreateResponse { Handle = handle };
                });
        }
    }
}
