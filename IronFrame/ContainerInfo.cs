using System;
using System.Collections.Generic;
using System.Linq;

namespace IronFrame
{
    public sealed class ContainerInfo : IEquatable<ContainerInfo>
    {
        public ContainerInfo()
        {
            Properties = new Dictionary<string,string>();
            State = ContainerState.Born;
            ReservedPorts = new List<int>();
        }

        public Dictionary<string, string> Properties { get; set; }
        public List<int> ReservedPorts { get; set; }
        public ContainerState State { get; set; }

        // BR: This makes me sad. These are only used for unit tests :(
        public bool Equals(ContainerInfo other)
        {
            var thisPropertiesKeys = new HashSet<string>(this.Properties.Keys);
            var otherPropertiesKeys = new HashSet<string>(other.Properties.Keys);

            return other != null &&
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
