using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Protocol;

namespace IronFoundry.Warden.Handlers
{
    public class CopyInRequestHandler : CopyRequestHandler
    {
        public CopyInRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request, new CopyInResponse())
        {
        }
    }
}
