namespace IronFoundry.Warden.Handlers
{
    using System.Threading.Tasks;
    using IronFoundry.Warden.Containers;
    using IronFoundry.Warden.Protocol;
    using NLog;

    public class LimitMemoryRequestHandler : ContainerRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly LimitMemoryRequest request;

        public LimitMemoryRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
            this.request = (LimitMemoryRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            log.Trace("Handle: '{0}' LimitInBytes: '{1}'", request.Handle, request.LimitInBytes);

            return Task.Run<Response>(async () =>
            {
                var container = GetContainer();
                
                await container.LimitMemoryAsync(request.LimitInBytes);

                return new LimitMemoryResponse
                {
                    LimitInBytes = request.LimitInBytes,
                };
            });
        }
    }
}
