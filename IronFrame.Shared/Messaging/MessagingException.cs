using System;

namespace IronFrame.Messaging
{
    internal class MessagingException : Exception
    {
        public MessagingException() { }
        public MessagingException(string message) : base(message) { }
        public MessagingException(string message, Exception inner) : base(message, inner) { }

        public JsonRpcErrorResponse ErrorResponse { get; set; }
    }
}
