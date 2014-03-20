namespace IronFoundry.Warden.Containers
{
    using System;
    using Protocol;
    using Utilities;

    public class InfoBuilder
    {
        private readonly Container container;

        public InfoBuilder(Container container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            this.container = container;
        }

        public InfoResponse GetInfoResponse()
        {
            var hostIp = IPUtilities.GetLocalIPAddress().ToString();
            var info = new InfoResponse(hostIp, hostIp, container.Directory.FullName, container.State);

            var stats = container.GetProcessStatistics();

            // Convert TimeSpan to nanoseconds
            info.CpuStatInfo.Usage = (ulong)stats.TotalProcessorTime.Ticks * 100;

            // RSS is defined as memory + swap
            // For now, report the private memory, which is more representative of how much memory an application is using.
            info.MemoryStatInfo.TotalRss = (ulong)stats.PrivateMemory;

            return info;
        }
    }
}
