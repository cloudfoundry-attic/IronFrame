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

    public class CreateRequestHandler : ContainerRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly CreateRequest request;

        public CreateRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
            this.request = (CreateRequest)request;
        }

        static IEnumerable<BindMount> GetBindMounts(CreateRequest request)
        {
            return request.BindMounts.Select(
                x => new BindMount
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
                    var resources = ContainerResourceHolder.Create(new WardenConfig());

                    var container = new ContainerProxy(new ContainerHostLauncher());
                    await container.InitializeAsync(resources);
                    
                    containerManager.AddContainer(container);

                    await container.BindMountsAsync(GetBindMounts(request));
                    
                    return new CreateResponse { Handle = container.Handle };
                });
        }
    }
}
