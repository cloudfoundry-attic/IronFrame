using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFrame
{
    public class FirewallRuleSpec
    {
        public Protocol Protocol { get; set; }

        public List<IPRange> Networks { get; set; }

        public List<PortRange> Ports { get; set; } 

        public string RemoteAddresses
        {
            get
            {
                if (Networks == null)
                {
                    return "*";
                }

                return String.Join(",", Networks.Select(x => x.Start + "-" + x.End));
            }
        }

        public string RemotePorts
        {
            get
            {
                if (Ports == null)
                {
                    return "*";
                }

                return String.Join(",", Ports.Select(x => x.Start + "-" + x.End));
            }
        }
    }

    public enum Protocol
    {
        All, Tcp, Udp
    }

    public class IPRange
    {
        public string Start { get; set; }
        public string End { get; set; }
    }

    public class PortRange
    {
        public UInt16 Start { get; set; }
        public UInt16 End { get; set; }
    }

}
