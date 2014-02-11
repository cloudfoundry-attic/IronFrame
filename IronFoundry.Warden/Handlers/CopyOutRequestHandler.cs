using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Protocol;

namespace IronFoundry.Warden.Handlers
{
    public class CopyOutRequestHandler : CopyRequestHandler
    {
        public CopyOutRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request, new CopyOutResponse())
        {
        }
    }
}
