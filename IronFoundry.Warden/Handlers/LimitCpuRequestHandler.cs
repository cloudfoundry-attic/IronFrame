using System.Threading.Tasks;
using IronFoundry.Warden.Protocol;
using NLog;

namespace IronFoundry.Warden.Handlers
{
    public class LimitCpuRequestHandler : RequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly LimitCpuRequest request;

        public LimitCpuRequestHandler(Request request)
            : base(request)
        {
            this.request = (LimitCpuRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            // TODO do work!
            log.Trace("Handle: '{0}' LimitInShares: '{1}' LimitInSharesSpecified: '{2}''", request.Handle, request.LimitInShares, request.LimitInSharesSpecified);
            return Task.FromResult<Response>(new LimitCpuResponse());
        }
    }
}