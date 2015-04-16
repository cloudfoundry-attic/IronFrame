using System;
using System.Collections.Generic;
using System.Linq;

namespace IronFoundry.Container
{
    public sealed class ContainerInfo : IEquatable<ContainerInfo>
    {
        public ContainerInfo()
        {
            CpuStat = new ContainerCpuStat();
            Events = new List<string>();
            MemoryStat = new ContainerMemoryStat();
            Properties = new Dictionary<string,string>();
            State = ContainerState.Born;
            ReservedPorts = new List<int>();
        }

        public ContainerCpuStat CpuStat { get; set; }
        public string ContainerIPAddress { get; set; }
        public string ContainerPath { get; set; }
        public List<string> Events { get; set; }
        public string HostIPAddress { get; set; }
        public ContainerMemoryStat MemoryStat { get; set; }
        //public List<int> ProcessIds { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public List<int> ReservedPorts { get; set; }
        public ContainerState State { get; set; }

        // BR: This makes me sad. These are only used for unit tests :(
        public bool Equals(ContainerInfo other)
        {
            var thisPropertiesKeys = new HashSet<string>(this.Properties.Keys);
            var otherPropertiesKeys = new HashSet<string>(other.Properties.Keys);

            return other != null &&
                this.CpuStat.Equals(other.CpuStat) &&
                String.Equals(ContainerIPAddress, other.ContainerIPAddress) &&
                String.Equals(ContainerPath, other.ContainerPath, StringComparison.OrdinalIgnoreCase) &&
                this.Events.SequenceEqual(other.Events, StringComparer.OrdinalIgnoreCase) &&
                String.Equals(HostIPAddress, other.HostIPAddress) &&
                this.MemoryStat.Equals(other.MemoryStat) &&
                this.ReservedPorts.SequenceEqual(other.ReservedPorts) &&
                //thisPropertiesKeys.Equals(otherPropertiesKeys) &&
                State.Equals(other.State);
        }
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

    public enum ContainerState
    {
        Born,
        Active,
        Stopped,
        Destroyed
    }
}
