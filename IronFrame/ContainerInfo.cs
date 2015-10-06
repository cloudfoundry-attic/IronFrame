using System;
using System.Collections.Generic;
using System.Linq;

namespace IronFrame
{
    public sealed class ContainerInfo : IEquatable<ContainerInfo>
    {
        public ContainerInfo()
        {
            Events = new List<string>();
            Properties = new Dictionary<string,string>();
            State = ContainerState.Born;
            ReservedPorts = new List<int>();
        }

        public string ContainerIPAddress { get; set; }
        public string ContainerPath { get; set; }
        public List<string> Events { get; set; }
        public string HostIPAddress { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public List<int> ReservedPorts { get; set; }
        public ContainerState State { get; set; }

        // BR: This makes me sad. These are only used for unit tests :(
        public bool Equals(ContainerInfo other)
        {
            var thisPropertiesKeys = new HashSet<string>(this.Properties.Keys);
            var otherPropertiesKeys = new HashSet<string>(other.Properties.Keys);

            return other != null &&
                String.Equals(ContainerIPAddress, other.ContainerIPAddress) &&
                String.Equals(ContainerPath, other.ContainerPath, StringComparison.OrdinalIgnoreCase) &&
                this.Events.SequenceEqual(other.Events, StringComparer.OrdinalIgnoreCase) &&
                String.Equals(HostIPAddress, other.HostIPAddress) &&
                this.ReservedPorts.SequenceEqual(other.ReservedPorts) &&
                //thisPropertiesKeys.Equals(otherPropertiesKeys) &&
                State.Equals(other.State);
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
