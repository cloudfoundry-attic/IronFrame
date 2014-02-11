namespace IronFoundry.Warden.Protocol
{
    using System;
    using System.Collections.Generic;

    public partial class InfoResponse : Response
    {
        public InfoResponse(string hostIp, string containerIp, string containerPath, string containerState)
        {
            if (hostIp.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("hostIp");
            }
            if (containerIp.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("containerIp");
            }
            if (containerPath.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("containerPath");
            }
            if (containerState.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("containerState");
            }

            _events = new List<string>();
            _jobIds = new List<ulong>();

            this.ContainerIp = containerIp;
            this.ContainerPath = containerPath;

            this.BandwidthStatInfo = new InfoResponse.BandwidthStat();
            this.CpuStatInfo = new InfoResponse.CpuStat();
            this.DiskStatInfo = new InfoResponse.DiskStat();
            this.HostIp = hostIp;
            this.MemoryStatInfo = new InfoResponse.MemoryStat();
            this.State = containerState;
        }

        public override Message.Type ResponseType
        {
            get { return Message.Type.Info; }
        }
    }
}
