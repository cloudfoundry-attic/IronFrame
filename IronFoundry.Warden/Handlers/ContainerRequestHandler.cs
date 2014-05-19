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

        protected async Task<InfoResponse> BuildInfoResponseAsync()
        {
            var container = GetContainer();
            if (container == null)
                return new InfoResponse();

            var info = await container.GetInfoAsync();

            var response = new InfoResponse
            {
                HostIp = info.HostIPAddress,
                ContainerIp = info.ContainerIPAddress,
                ContainerPath = info.ContainerPath,
                State = info.State,
                MemoryStatInfo = new InfoResponse.MemoryStat
                {
                    // RSS is defined as memory + swap. This is the equivalent of "private memory" on Windows.
                    TotalRss = (ulong)info.MemoryStat.PrivateBytes,
                },
                CpuStatInfo = new InfoResponse.CpuStat
                {
                    // Convert TimeSpan to nanoseconds
                    Usage = (ulong)info.CpuStat.TotalProcessorTime.Ticks * 100,
                },
            };

            response.Events.AddRange(info.Events);
            
            return response;
        }
    }
}
