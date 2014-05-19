using System;
using System.Collections.Generic;
using System.Linq;
using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class ContainerMemoryStat : IEquatable<ContainerMemoryStat>
    {
        public ulong PrivateBytes { get; set; }

        public bool Equals(ContainerMemoryStat other)
        {
            return other != null &&
                this.PrivateBytes == other.PrivateBytes;
        }
    }

    public class ContainerCpuStat : IEquatable<ContainerCpuStat>
    {
        public TimeSpan TotalProcessorTime { get; set; }

        public bool Equals(ContainerCpuStat other)
        {
            return other != null &&
                this.TotalProcessorTime == other.TotalProcessorTime;
        }
    }

    public class ContainerInfo : IEquatable<ContainerInfo>
    {
        public ContainerInfo()
        {
            CpuStat = new ContainerCpuStat();
            Events = new List<string>();
            MemoryStat = new ContainerMemoryStat();
            State = ContainerState.Born.ToString();
        }

        public ContainerCpuStat CpuStat { get; set; }
        public string ContainerIPAddress { get; set; }
        public string ContainerPath { get; set; }
        public List<string> Events { get; set; }
        public string HostIPAddress { get; set; }
        public ContainerMemoryStat MemoryStat { get; set; }
        //public List<int> ProcessIds { get; set; }
        public string State { get; set; }

        public bool Equals(ContainerInfo other)
        {
            return other != null &&
                this.CpuStat.Equals(other.CpuStat) &&
                String.Equals(ContainerIPAddress, other.ContainerIPAddress) &&
                String.Equals(ContainerPath, other.ContainerPath, StringComparison.OrdinalIgnoreCase) &&
                this.Events.SequenceEqual(other.Events, StringComparer.OrdinalIgnoreCase) &&
                String.Equals(HostIPAddress, other.HostIPAddress) &&
                this.MemoryStat.Equals(other.MemoryStat) &&
                String.Equals(State, other.State);
        }
    }

    public class ContainerInfoRequest : JsonRpcRequest
    {
        public const string MethodName = "Container.Info";

        public ContainerInfoRequest() : base(MethodName)
        {
        }
    }

    public class ContainerInfoResponse : JsonRpcResponse<ContainerInfo>
    {
        public ContainerInfoResponse(JToken id, ContainerInfo result) : base(id, result)
        {
        }
    }
}
