using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Handlers
{
    public class LoggingRequestHandler : ContainerRequestHandler
    {
        private LoggingRequest request;

        public LoggingRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
            this.request = (LoggingRequest)request;
        }

        public async override Task<Response> HandleAsync()
        {
            var container = GetContainer();

            var loggingData = new InstanceLoggingInfo()
            {
                ApplicationId = request.ApplicationId,
                InstanceIndex = request.InstanceIndex,
                LoggregatorAddress = request.LoggregatorRouter,
                LoggregatorSecret = request.LoggregatorSecret,
            };

            loggingData.DrainUris.AddRange(request.DrainUris);

            await container.EnableLoggingAsync(loggingData);

            return new LoggingResponse();
        }
    }
}
