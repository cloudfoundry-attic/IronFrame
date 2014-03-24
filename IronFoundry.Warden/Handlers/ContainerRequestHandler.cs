namespace IronFoundry.Warden.Handlers
{
    using System;
    using Containers;
    using Protocol;

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

        protected InfoResponse BuildInfoResponse()
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
                infoResponse = infoBuilder.GetInfoResponse();
            }

            return infoResponse;
        }
    }
}
