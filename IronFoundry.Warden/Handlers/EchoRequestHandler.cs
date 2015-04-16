namespace IronFoundry.Warden.Handlers
{
    using System.Threading.Tasks;
    using IronFoundry.Warden.Protocol;
    using NLog;

    // MO: Added to ContainerClient
    public class EchoRequestHandler : RequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly EchoRequest request;

        public EchoRequestHandler(Request request)
            : base(request)
        {
            this.request = (EchoRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            log.Trace("Message: '{0}'", request.Message);
            return Task.FromResult<Response>(new EchoResponse { Message = request.Message });
        }
    }
}
