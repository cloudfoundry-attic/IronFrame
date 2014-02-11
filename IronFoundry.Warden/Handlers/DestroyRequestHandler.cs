using System;
using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Protocol;
using IronFoundry.Warden.Utilities;
using NLog;

namespace IronFoundry.Warden.Handlers
{
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
            if (request.Handle.IsNullOrWhiteSpace())
            {
                throw new WardenException("Container handle is required.");
            }
            else
            {
                log.Trace("Destroying container with handle: '{0}'", request.Handle);

                return Task.Run<Response>(() =>
                    {
                        Container container = GetContainer();
                        if (container != null)
                        {
                            if (container.State != ContainerState.Stopped)
                            {
                                try
                                {
                                    var stopRequest = new StopRequest { Handle = request.Handle };
                                    var stopRequestHandler = new StopRequestHandler(containerManager, stopRequest);
                                    var stopTask = stopRequestHandler.HandleAsync();
                                    Response stopResponse = stopTask.Result;
                                }
                                catch (Exception ex)
                                {
                                    log.WarnException(ex);
                                }
                            }
                            containerManager.DestroyContainer(container);
                        }
                        return new DestroyResponse();
                    });
            }
        }
    }
}
