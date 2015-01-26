namespace IronFoundry.Container
{
    using System;
    using System.Runtime.Serialization;
    using System.Text;

    [Serializable]
    public class WardenException : Exception, ISerializable
    {
        public WardenException() : base()
        {
        }

        public WardenException(string message) : base(message)
        {
        }

        public WardenException(string message, params object[] args) : this(String.Format(message, args))
        {
        }

        public WardenException(string message, Exception inner) : base(message, inner)
        {
        }

        public WardenException(SerializationInfo info, StreamingContext context) : base(info, context)
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
