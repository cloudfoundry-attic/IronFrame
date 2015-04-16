namespace IronFoundry.Warden.Handlers
{
    using Containers;
    using NLog;
    using Protocol;
    using System;
    using System.Threading.Tasks;

    // MO: Added to ContainerClient
    public abstract class CopyRequestHandler : ContainerRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ICopyRequest request;
        private readonly Response response;

        protected CopyRequestHandler(IContainerManager containerManager, Request request, Response response)
            : base(containerManager, request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            this.request = (ICopyRequest)request;

            if (response == null)
            {
                throw new ArgumentNullException("response");
            }
            this.response = response;
        }

        public async override Task<Response> HandleAsync()
        {
            log.Trace("SrcPath: '{0}' DstPath: '{1}'", request.SrcPath, request.DstPath);

            IContainerClient container = GetContainer();
            if (container == null)
            {
                return response;
            }

            await container.CopyAsync(request.SrcPath, request.DstPath);

            return response;
        }
    }
}
