namespace IronFoundry.Warden.Handlers
{
    using Containers;
    using Protocol;

    public class CopyInRequestHandler : CopyRequestHandler
    {
        public CopyInRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request, new CopyInResponse())
        {
        }
    }
}
