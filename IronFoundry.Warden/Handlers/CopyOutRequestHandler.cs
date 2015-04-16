namespace IronFoundry.Warden.Handlers
{
    using Containers;
    using Protocol;

    // MO: Added to ContainerClient
    public class CopyOutRequestHandler : CopyRequestHandler
    {
        public CopyOutRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request, new CopyOutResponse())
        {
        }
    }
}
