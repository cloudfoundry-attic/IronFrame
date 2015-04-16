namespace IronFoundry.Warden.Protocol
{
    using System;
    using System.Collections.Generic;

    public partial class InfoResponse : Response
    {
        public InfoResponse(string hostIp, string containerIp, string containerPath, string containerState)
        {
            if (String.IsNullOrWhiteSpace(hostIp))
            {
                throw new ArgumentNullException("hostIp");
            }
            if (String.IsNullOrWhiteSpace(containerIp))
            {
                throw new ArgumentNullException("containerIp");
            }
            if (String.IsNullOrWhiteSpace(containerPath))
            {
                throw new ArgumentNullException("containerPath");
            }
            if (String.IsNullOrWhiteSpace(containerState))
            {
                throw new ArgumentNullException("containerState");
            }

            _events = new List<string>();
            _jobIds = new List<ulong>();

            this.ContainerIp = containerIp;
            this.ContainerPath = containerPath;

            this.BandwidthStatInfo = BandwidthStat.CreateDefault();
            this.CpuStatInfo = new InfoResponse.CpuStat();
            this.DiskStatInfo = DiskStat.CreateDefault();
            this.HostIp = hostIp;
            this.MemoryStatInfo = MemoryStat.CreateDefault();
            this.State = containerState;
        }

        public override Message.Type ResponseType
        {
            get { return Message.Type.Info; }
        }

        public partial class BandwidthStat
        {
            public static BandwidthStat CreateDefault()
            {
                return new BandwidthStat()
                {
                    InBurst = 0,
                    OutBurst = 0,

                    InRate = 0,
                    OutRate = 0,
                };
            }
        }

        public partial class DiskStat
        {
            public static DiskStat CreateDefault()
            {
                return new DiskStat()
                {
                    BytesUsed = 0,
                    InodesUsed = 0,
                };
            }
        }

        public partial class MemoryStat
        {
            public static MemoryStat CreateDefault()
            {
                return new MemoryStat()
                {
                    TotalRss = 0,
                    TotalCache = 0
                };
            }
        }
    }


}
