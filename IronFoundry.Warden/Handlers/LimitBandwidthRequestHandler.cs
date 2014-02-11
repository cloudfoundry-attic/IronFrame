using System.Threading.Tasks;
using IronFoundry.Warden.Protocol;
using NLog;

namespace IronFoundry.Warden.Handlers
{
    public class LimitBandwidthRequestHandler : RequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly LimitBandwidthRequest request;

        public LimitBandwidthRequestHandler(Request request)
            : base(request)
        {
            this.request = (LimitBandwidthRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            // TODO do work!
            log.Trace("Handle: '{0}' Burst: '{1}' Rate: '{2}'", request.Handle, request.Burst, request.Rate);
            return Task.FromResult<Response>(new LimitBandwidthResponse { Burst = 0, Rate = 0 });
        }
    }
}
