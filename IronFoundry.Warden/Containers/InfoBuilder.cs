namespace IronFoundry.Warden.Containers
{
    using System;
    using Protocol;
    using Utilities;
    using System.Threading.Tasks;

    public class InfoBuilder
    {
        private readonly IContainerClient container;

        public InfoBuilder(IContainerClient container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            this.container = container;
        }

        public async Task<InfoResponse> GetInfoResponse()
        {
            var hostIp = IPUtilities.GetLocalIPAddress().ToString();
            var info = new InfoResponse(hostIp, hostIp, container.ContainerDirectoryPath, container.State);

            var stats = await container.GetProcessStatisticsAsync();

            // Convert TimeSpan to nanoseconds
            info.CpuStatInfo.Usage = (ulong)stats.TotalProcessorTime.Ticks * 100;

            // RSS is defined as memory + swap. This is the equivalent of "private memory" on Windows.
            info.MemoryStatInfo.TotalRss = (ulong)stats.PrivateMemory;

            return info;
        }
    }
}
