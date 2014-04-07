using System.Web.UI.WebControls;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Containers.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using logmessage;
using Xunit;
using System.Net;


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
                LoggregatorAddress = "127.0.0.1:5555",
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
        
        [Fact]
        public void InitializesWithValidName()
        {
            var logData = new InstanceLoggingInfo()
            {
                ApplicationId = "0",
                InstanceIndex = "0",
                LoggregatorAddress = "localhost:5555",
                LoggregatorSecret = "Secret",
            };

            var emitter = new ContainerLogEmitter(logData);
            Assert.NotNull(emitter);
        }

        [Fact]
        public void ThrowsWithInvalidHostName()
        {
            var logData = new InstanceLoggingInfo()
            {
                ApplicationId = "0",
                InstanceIndex = "0",
                LoggregatorAddress = "SomeInvalid-HostName",
                LoggregatorSecret = "Secret",
            };

            var ex = Record.Exception(() => { var emitter = new ContainerLogEmitter(logData); });
            Assert.IsType<ArgumentException>(ex);
        }
        

        [Fact]
        public void WhenNullDataIsLoggedDoesNotThrow()
        {
            var logData = new InstanceLoggingInfo()
            {
                ApplicationId = "0",
                InstanceIndex = "0",
                LoggregatorAddress = "127.0.0.1:5555",
                LoggregatorSecret = "Secret",
            };
            var emitter = new ContainerLogEmitter(logData);

            emitter.EmitLogMessage(LogMessage.MessageType.OUT, null);
        }

        
    }
}
