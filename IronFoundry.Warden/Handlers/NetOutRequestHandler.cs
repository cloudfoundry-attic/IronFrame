namespace IronFoundry.Warden.Handlers
{
    using System.Threading.Tasks;
    using IronFoundry.Warden.Protocol;
    using NLog;

    public class NetOutRequestHandler : RequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly NetOutRequest request;

        public NetOutRequestHandler(Request request)
            : base(request)
        {
            this.request = (NetOutRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            // TODO do work!
            log.Trace("Handle: '{0}' Network: '{1}' Port: '{2}'", request.Handle, request.Network, request.Port);
            return Task.FromResult<Response>(new NetOutResponse());
        }
    }
}
