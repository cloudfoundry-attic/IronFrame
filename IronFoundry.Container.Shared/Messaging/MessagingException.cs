using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Container.Messaging
{
    [Serializable]
    public class MessagingException : Exception
    {
        public MessagingException() { }
        public MessagingException(string message) : base(message) { }
        public MessagingException(string message, Exception inner) : base(message, inner) { }
        protected MessagingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public JsonRpcErrorResponse ErrorResponse { get; set; }
    }
}
