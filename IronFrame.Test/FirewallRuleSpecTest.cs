using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFrame
{
    public class FirewallRuleSpecTest
    {
        [Fact]
        public void TestRemoteAddresses()
        {
            var firewallRuleSpec = new FirewallRuleSpec()
            {
                Networks = new List<IPRange>
                {
                    new IPRange {Start = "10.1.1.1", End = "10.1.1.10"},
                    new IPRange {Start = "10.3.1.1", End = "10.3.1.10"}

                }
            };
            Assert.Equal("10.1.1.1-10.1.1.10,10.3.1.1-10.3.1.10", firewallRuleSpec.RemoteAddresses);
        }
    }
}