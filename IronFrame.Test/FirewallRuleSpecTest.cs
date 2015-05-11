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

        [Fact]
        public void TestRemotePorts()
        {
            var firewallRuleSpec = new FirewallRuleSpec()
            {
                Ports = new List<PortRange>
                {
                    new PortRange() {Start = 8080, End = 8090},
                    new PortRange() {Start = 9090, End = 9099},

                }
            };
            Assert.Equal("8080-8090,9090-9099", firewallRuleSpec.RemotePorts);
        }

        [Fact]
        public void ReturnStarIfAddressesIsEmpty()
        {
            var firewallRuleSpec = new FirewallRuleSpec();
            Assert.Equal("*", firewallRuleSpec.RemoteAddresses);
        }

        [Fact]
        public void ReturnStarIfPortIsEmpty()
        {
            var firewallRuleSpec = new FirewallRuleSpec();
            Assert.Equal("*", firewallRuleSpec.RemotePorts);
        }

        [Fact]
        public void InitializeProtocolToAll()
        {
            var firewallRuleSpec = new FirewallRuleSpec();
            Assert.Equal(Protocol.All, firewallRuleSpec.Protocol);
        }
    }
}