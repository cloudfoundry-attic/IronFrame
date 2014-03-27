namespace IronFoundry.Warden.Handlers
{
    using System;
    using Containers;
    using Protocol;
    using System.Threading.Tasks;

    public abstract class ContainerRequestHandler : RequestHandler
    {
        protected readonly IContainerManager containerManager;
        protected readonly IContainerRequest containerRequest;

        public ContainerRequestHandler(IContainerManager containerManager, Request request)
            : base(request)
        {
            if (containerManager == null)
            {
                throw new ArgumentNullException("containerManager");
            }
            this.containerManager = containerManager;

            this.containerRequest = (IContainerRequest)request;
        }

        protected IContainerClient GetContainer()
        {
            return containerManager.GetContainer(containerRequest.Handle);
        }

        protected async Task<InfoResponse> BuildInfoResponse()
        {
            // TODO complete info
            InfoResponse infoResponse = null;

            IContainerClient container = GetContainer();
            if (container == null)
            {
                infoResponse = new InfoResponse();
            }
            else
            {
                var infoBuilder = new InfoBuilder(container);
                infoResponse = await infoBuilder.GetInfoResponse();
            }

            return infoResponse;
        }
    }
}
