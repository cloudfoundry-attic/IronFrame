namespace IronFoundry.Warden.Handlers
{
    using System.Threading.Tasks;
    using IronFoundry.Warden.Protocol;
    using NLog;

    public class LimitMemoryRequestHandler : RequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly LimitMemoryRequest request;

        public LimitMemoryRequestHandler(Request request)
            : base(request)
        {
            this.request = (LimitMemoryRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            // TODO do work!
            log.Trace("Handle: '{0}' LimitInBytes: '{1}'", request.Handle, request.LimitInBytes);
            return Task.FromResult<Response>(new LimitMemoryResponse { LimitInBytes = 134217728 }); // TODO 128 MB
        }
    }
}
