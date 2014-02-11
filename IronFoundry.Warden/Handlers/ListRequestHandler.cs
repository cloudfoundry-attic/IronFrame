using System;
using System.Linq;
using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Protocol;

namespace IronFoundry.Warden.Handlers
{
    public class ListRequestHandler : RequestHandler
    {
        private readonly IContainerManager containerManager;
        private readonly ListRequest request;

        public ListRequestHandler(IContainerManager containerManager, Request request)
            : base(request)
        {
            if (containerManager == null)
            {
                throw new ArgumentNullException("containerManager");
            }
            this.containerManager = containerManager;
            this.request = (ListRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            var response =  new ListResponse();
            response.Handles.AddRange(containerManager.Handles.Select(h => (string)h));
            return Task.FromResult<Response>(response);
        }
    }
}
