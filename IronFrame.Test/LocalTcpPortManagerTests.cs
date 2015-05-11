using System;
using IronFrame.Utilities;
using NSubstitute;
using Xunit;

namespace IronFrame
{
    public class LocalTcpPortManagerTests
    {
        internal INetShRunner NetShRunner;
        internal IFirewallManager FirewallManager;

        public LocalTcpPortManagerTests()
        {
            NetShRunner = Substitute.For<INetShRunner>();
            FirewallManager = Substitute.For<IFirewallManager>();
        }

        public class WhenReservingPort : LocalTcpPortManagerTests
        {
            [Fact]
            public void ThrowsArgumentNullExceptionIfUserNameNull()
            {
                var tcpPortManager = new LocalTcpPortManager(null, null);

                Assert.Throws<ArgumentNullException>(() => tcpPortManager.ReserveLocalPort(100, null));
            }

            [Fact]
            public void CallsNetShRunnerFirewallManagerAndReturnsPort()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.AddRule(Arg.Any<int>(), Arg.Any<string>()).ReturnsForAnyArgs(true);

                var port = tcpPortManager.ReserveLocalPort(8888, "userName");

                NetShRunner.Received().AddRule(Arg.Is(8888), Arg.Is("userName"));
                FirewallManager.Received().OpenPort(Arg.Is(8888), Arg.Is("userName"));
                Assert.Equal(8888, port);
            }

            [Fact]
            public void ZeroPortReturnsNewPort()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.AddRule(Arg.Any<int>(), Arg.Any<string>()).ReturnsForAnyArgs(true);

                var port = tcpPortManager.ReserveLocalPort(0, "userName");

                Assert.True(port > 0);
            }

            [Fact]
            public void ThrowsWardenExceptionIfNetShFails()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.AddRule(Arg.Any<int>(), Arg.Any<string>()).ReturnsForAnyArgs(false);

                var exception = Record.Exception(() =>  tcpPortManager.ReserveLocalPort(8888, "userName"));

                Assert.IsType<Exception>(exception);
            }

            [Fact]
            public void ThrowWardenExecptionIfFirewallManagerFails()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.AddRule(Arg.Any<int>(), Arg.Any<string>()).ReturnsForAnyArgs(true);
                FirewallManager.When(x => x.OpenPort(Arg.Any<int>(), Arg.Any<string>())).Do(x => { throw new Exception(); });

                var exception = Record.Exception(() => tcpPortManager.ReserveLocalPort(8888, "userName"));

                Assert.IsType<Exception>(exception);
            }
        }

        public class WhenReleasingPort : LocalTcpPortManagerTests
        {
            [Fact]
            public void ThrowsArgumentNullExceptionIfUserNameIsNull()
            {
                var tcpPortManager = new LocalTcpPortManager(null, null);

                Assert.Throws<ArgumentNullException>(() => tcpPortManager.ReleaseLocalPort(100, null));
            }

            [Fact]
            public void CallsNetShRunnerFirewallManagerToReleasePort()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.DeleteRule(Arg.Any<int>()).ReturnsForAnyArgs(true);

                tcpPortManager.ReleaseLocalPort(8888, "userName");

                NetShRunner.Received().DeleteRule(Arg.Is(8888));
                FirewallManager.Received().ClosePort(Arg.Is("userName"));
            }

            [Fact]
            public void ThrowsWardenExceptionIfNetShFails()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.DeleteRule(Arg.Any<int>()).ReturnsForAnyArgs(false);

                var exception = Record.Exception(() => tcpPortManager.ReleaseLocalPort(8888, "userName"));

                Assert.IsType<Exception>(exception);
            }

            [Fact]
            public void RemovesFirewallRulesEvenWithNoPort()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);

                tcpPortManager.ReleaseLocalPort(null, "userName");

                NetShRunner.DidNotReceive().DeleteRule(Arg.Any<int>());
                FirewallManager.Received().ClosePort(Arg.Is("userName"));
            }

            [Fact]
            public void ThrowWardenExecptionIfFirewallManagerFails()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.DeleteRule(Arg.Any<int>()).ReturnsForAnyArgs(true);
                FirewallManager.When(x => x.ClosePort(Arg.Any<string>())).Do(x => { throw new Exception(); });

                var exception = Record.Exception(() => tcpPortManager.ReserveLocalPort(8888, "userName"));

                Assert.IsType<Exception>(exception);
            }
        }

        public class CreateOutboundFirewallRule : LocalTcpPortManagerTests
        {
            [Fact]
            public void DelegatesToFirewallManager()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                var firewallRuleSpec = new FirewallRuleSpec();
                tcpPortManager.CreateOutboundFirewallRule("fred", firewallRuleSpec);
                FirewallManager.Received(1).CreateOutboundFirewallRule("fred", firewallRuleSpec);
            }
        }

        public class RemoveFirewallRules : LocalTcpPortManagerTests
        {
            [Fact]
            public void DelegatesToFirewallManager()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                tcpPortManager.RemoveFirewallRules("fred");
                FirewallManager.Received(1).RemoveAllFirewallRules("fred");
            }
        }
    }
}