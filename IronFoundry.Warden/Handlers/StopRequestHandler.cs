using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Protocol;
using NLog;

namespace IronFoundry.Warden.Handlers
{
    public class StopRequestHandler : ContainerRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly StopRequest request;

        public StopRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
            this.request = (StopRequest) request;
        }

        public override Task<Response> HandleAsync()
        {
            return Task.Run<Response>(async () =>
                    {
                        // before
                        log.Trace("Handle: '{0}' Background: '{1}' Kill: '{2}'", request.Handle, request.Background, request.Kill);
                        var c = GetContainer();

                        // do
                        if (c != null)
                        {
                            await c.StopAsync(false);
                        }
                        else
                        {
                            log.Info("Handle: '{0}' could not be found.", request.Handle);
                        }

                        return new StopResponse();
                    });
        }
    }
}