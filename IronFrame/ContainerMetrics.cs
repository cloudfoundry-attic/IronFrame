using System;
using System.Collections.Generic;

namespace IronFrame
{
    public sealed class ContainerMetrics  
    {
        public ContainerMetrics()
        {
            MemoryStat = new ContainerMemoryStat();
            CpuStat = new ContainerCpuStat();
        }

        public ContainerCpuStat CpuStat { get; set; }
        public ContainerMemoryStat MemoryStat { get; set; }
    }

    public sealed class ContainerCpuStat : IEquatable<ContainerCpuStat>
    {
        public TimeSpan TotalProcessorTime { get; set; }

        // BR: This makes me sad. These are only used for unit tests :(
        public bool Equals(ContainerCpuStat other)
        {
            return other != null &&
                this.TotalProcessorTime == other.TotalProcessorTime;
        }
    }

    public sealed class ContainerMemoryStat : IEquatable<ContainerMemoryStat>
    {
        public ulong PrivateBytes { get; set; }

        // BR: This makes me sad. These are only used for unit tests :(
        public bool Equals(ContainerMemoryStat other)
        {
            return other != null &&
                this.PrivateBytes == other.PrivateBytes;
        }
    }
}
