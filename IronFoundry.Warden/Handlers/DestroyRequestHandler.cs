namespace IronFoundry.Warden.Handlers
{
    using System;
    using System.Threading.Tasks;
    using Containers;
    using NLog;
    using Protocol;
    using Utilities;

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
            if (request.Handle.IsNullOrWhiteSpace()) throw new WardenException("Container handle is required.");

            log.Trace("Destroying container with handle: '{0}'", request.Handle);
            
            return Task.Run<Response>(async () =>
                {
                    var container = GetContainer();
                    if (container != null)
                    {
                        var containerInfo = await container.GetInfoAsync();
                        if (containerInfo.State != Containers.Messages.ContainerState.Stopped)
                        {
                            try
                            {
                                await container.StopAsync(true);
                            }
                            catch (Exception ex)
                            {
                                log.WarnException(ex);
                            }
                        }
                        await containerManager.DestroyContainerAsync(container);
                    }
                    return new DestroyResponse();
                });

        }
    }
}
