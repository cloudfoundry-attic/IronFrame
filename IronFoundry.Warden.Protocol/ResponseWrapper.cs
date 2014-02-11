using System;
using System.IO;
using ProtoBuf;

namespace IronFoundry.Warden.Protocol
{
    public class ResponseWrapper
    {
        private readonly Response response;

        public ResponseWrapper(Response response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }
            this.response = response;
        }

        public Message GetMessage()
        {
            byte[] msgPayload = null;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, this.response);
                msgPayload = ms.ToArray();
            }
            return new Message { MessageType = response.ResponseType, Payload = msgPayload };
        }
    }
}
