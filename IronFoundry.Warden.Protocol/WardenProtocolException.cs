using System;
using System.Runtime.Serialization;
using System.Text;

namespace IronFoundry.Warden.Protocol
{
    [Serializable]
    public sealed class WardenProtocolException : Exception, ISerializable
    {
        public WardenProtocolException()
            : base()
        {
        }

        public WardenProtocolException(string message)
            : base(message)
        {
        }

        public WardenProtocolException(string message, params object[] args)
            : this(String.Format(message, args))
        {
        }

        public WardenProtocolException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public WardenProtocolException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public string ResponseMessage
        {
            get
            {
                var sb = new StringBuilder(this.Message);
                if (this.InnerException != null)
                {
                    sb.AppendLine().AppendLine(this.InnerException.Message);
                }
                return sb.ToString();
            }
        }
    }
}
