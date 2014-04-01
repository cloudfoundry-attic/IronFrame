using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Containers.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace IronFoundry.Warden.Test
{
    public class ContainerLogEmitterTest
    {
        [Fact]
        public void InitializesWithValidAddress()
        {
            var logData = new InstanceLoggingInfo()
            {
                ApplicationId = "0",
                InstanceIndex = "0",
                LoggregatorAddress = "192.168.1.1:5555",
                LoggregatorSecret = "Secret",
            };

            var emitter = new ContainerLogEmitter(logData);
            Assert.NotNull(emitter);
        }

        [Fact]
        public void ThrowsWithInvalidAddress()
        {
            var logData = new InstanceLoggingInfo()
                {
                    ApplicationId = "0",
                    InstanceIndex = "0",
                    LoggregatorAddress = "999.999.999.999:5555",
                    LoggregatorSecret = "Secret",
                };

            var ex = Record.Exception(() => { var emitter = new ContainerLogEmitter(logData); });
            Assert.IsType<ArgumentException>(ex);
        }
    }
}
