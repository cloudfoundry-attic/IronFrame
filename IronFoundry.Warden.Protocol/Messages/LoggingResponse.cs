using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Protocol
{
    public partial class LoggingResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.Logging;  }
        }
    }
}
