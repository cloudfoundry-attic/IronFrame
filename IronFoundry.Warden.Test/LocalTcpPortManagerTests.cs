using System;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class LocalTcpPortManagerTests
    {
        protected INetShRunner NetShRunner;
        protected IFirewallManager FirewallManager;

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
                NetShRunner.AddRule(Arg.Any<ushort>(), Arg.Any<string>()).ReturnsForAnyArgs(true);

                ushort port = tcpPortManager.ReserveLocalPort(8888, "userName");

                NetShRunner.Received().AddRule(Arg.Is((ushort)8888), Arg.Is("userName"));
                FirewallManager.Received().OpenPort(Arg.Is((ushort)8888), Arg.Is("userName-8888"));
                Assert.Equal(8888, port);
            }

            [Fact]
            public void ZeroPortReturnsNewPort()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.AddRule(Arg.Any<ushort>(), Arg.Any<string>()).ReturnsForAnyArgs(true);

                ushort port = tcpPortManager.ReserveLocalPort(0, "userName");

                Assert.True(port > 0);
            }

            [Fact]
            public void ThrowsWardenExceptionIfNetShFails()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.AddRule(Arg.Any<ushort>(), Arg.Any<string>()).ReturnsForAnyArgs(false);

                var exception = Record.Exception(() =>  tcpPortManager.ReserveLocalPort(8888, "userName"));

                Assert.IsType<WardenException>(exception);
            }

            [Fact]
            public void ThrowWardenExecptionIfFirewallManagerFails()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.AddRule(Arg.Any<ushort>(), Arg.Any<string>()).ReturnsForAnyArgs(true);
                FirewallManager.When(x => x.OpenPort(Arg.Any<ushort>(), Arg.Any<string>())).Do(x => { throw new Exception(); });

                var exception = Record.Exception(() => tcpPortManager.ReserveLocalPort(8888, "userName"));

                Assert.IsType<WardenException>(exception);
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
                NetShRunner.DeleteRule(Arg.Any<ushort>()).ReturnsForAnyArgs(true);

                tcpPortManager.ReleaseLocalPort(8888, "userName");

                NetShRunner.Received().DeleteRule(Arg.Is((ushort) 8888));
                FirewallManager.Received().ClosePort(Arg.Is("userName-8888"));
            }

            [Fact]
            public void ThrowsWardenExceptionIfNetShFails()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.DeleteRule(Arg.Any<ushort>()).ReturnsForAnyArgs(false);

                var exception = Record.Exception(() => tcpPortManager.ReleaseLocalPort(8888, "userName"));

                Assert.IsType<WardenException>(exception);
            }

            [Fact]
            public void ThrowWardenExecptionIfFirewallManagerFails()
            {
                var tcpPortManager = new LocalTcpPortManager(FirewallManager, NetShRunner);
                NetShRunner.DeleteRule(Arg.Any<ushort>()).ReturnsForAnyArgs(true);
                FirewallManager.When(x => x.ClosePort(Arg.Any<string>())).Do(x => { throw new Exception(); });

                var exception = Record.Exception(() => tcpPortManager.ReserveLocalPort(8888, "userName"));

                Assert.IsType<WardenException>(exception);
            }
        }
    }
}