namespace IronFoundry.Warden.Handlers
{
    using Containers;
    using Protocol;

    // MO: Added to ContainerClient
    public class CopyInRequestHandler : CopyRequestHandler
    {
        public CopyInRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request, new CopyInResponse())
        {
        }
    }
}
