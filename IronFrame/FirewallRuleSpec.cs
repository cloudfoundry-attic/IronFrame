using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFrame
{
    public class FirewallRuleSpec
    {
        public List<IPRange> Networks { get; set; }

        public string RemoteAddresses
        {
            get { return String.Join(",", Networks.Select(x => x.Start + "-" + x.End)); }
        }
    }

    public class IPRange
    {
        public string Start { get; set; }
        public string End { get; set; }
    }
}
