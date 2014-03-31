using IronFoundry.Warden.Logging;
using logmessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Containers
{
    public interface ILogEmitter
    {
        void EmitLogMessage(LogMessage.MessageType type, string message);
    }


    public class ContainerLogEmitter : ILogEmitter
    {
        public ContainerLogEmitter()
        {
        }

        public void EmitLogMessage(LogMessage.MessageType type, string message)
        {
            //    
        }
    }
}
