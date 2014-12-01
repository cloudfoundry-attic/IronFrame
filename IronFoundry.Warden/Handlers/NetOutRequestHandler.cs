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
            log.Trace("Handle: '{0}' Network: '{1}' Port: '{2}' PortRange: '{3}' ProtocolInfo: '{4}' IcmpType: '{5}' IcmpCode: '{6}' Log: '{7}'", 
                request.Handle, 
                request.Network, 
                request.Port, 
                request.PortRange,
                request.ProtocolInfo,
                request.IcmpType,
                request.IcmpCode,
                request.Log
                );
            return Task.FromResult<Response>(new NetOutResponse());
        }
    }
}
