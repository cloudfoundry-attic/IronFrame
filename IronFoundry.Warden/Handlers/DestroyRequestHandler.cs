namespace IronFoundry.Warden.Handlers
{
    using System;
    using System.Threading.Tasks;
    using Containers;
    using IronFoundry.Container;
    using NLog;
    using Protocol;
    using Utilities;

    // MO: Added to ContainerClient
    public class DestroyRequestHandler : ContainerRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly DestroyRequest request;

        public DestroyRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
            this.request = (DestroyRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            if (String.IsNullOrWhiteSpace(request.Handle)) throw new WardenException("Container handle is required.");

            log.Trace("Destroying container with handle: '{0}'", request.Handle);


            return Task.Run<Response>(async () =>
                {
                    await containerManager.DestroyContainerAsync(new ContainerHandle(request.Handle));

                    return new DestroyResponse();
                });

        }
    }
}
