namespace IronFoundry.Warden.Utilities
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using ProtoBuf;
    using Protocol;

    public class MessageWriter
    {
        private readonly NetworkStream destination;

        public MessageWriter(NetworkStream destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            else if (!destination.CanWrite)
            {
                throw new ArgumentException("destination stream is unwritable");
            }
            this.destination = destination;
        }

        public Task WriteAsync(Response response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            var wrapper = new ResponseWrapper(response);
            Message message = wrapper.GetMessage();

            byte[] responsePayload = null;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, message);
                responsePayload = ms.ToArray();
            }

            int payloadLen = responsePayload.Length;
            var payloadLenBytes = Encoding.ASCII.GetBytes(payloadLen.ToString());

            byte[] responseBytes = null;
            using (var ms = new MemoryStream())
            {
                ms.Write(payloadLenBytes, 0, payloadLenBytes.Length);
                ms.WriteByte(Constants.CR);
                ms.WriteByte(Constants.LF);
                ms.Write(responsePayload, 0, responsePayload.Length);
                ms.WriteByte(Constants.CR);
                ms.WriteByte(Constants.LF);
                responseBytes = ms.ToArray();
            }
            return destination.WriteAsync(responseBytes, 0, responseBytes.Length);
        }
    }
}
