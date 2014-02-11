using System.Threading.Tasks;
using IronFoundry.Warden.Protocol;
using NLog;

namespace IronFoundry.Warden.Handlers
{
    public class LimitDiskRequestHandler : RequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly LimitDiskRequest request;

        public LimitDiskRequestHandler(Request request)
            : base(request)
        {
            this.request = (LimitDiskRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            // TODO do work!
            log.Trace("Handle: '{0}' Block: '{1}' Byte: '{2}' Inode: '{3}'", request.Handle, request.Block, request.Byte, request.Inode);
            return Task.FromResult<Response>(new LimitDiskResponse());
        }
    }
}
