namespace IronFoundry.Warden.Protocol
{
    using System;
    using System.IO;
    using IronFoundry.Container;
    using ProtoBuf;

    public class MessageUnwrapper
    {
        private readonly Message message;

        public MessageUnwrapper(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            this.message = message;
        }

        public Request GetRequest()
        {
            Request request = null;

            switch (message.MessageType)
            {
                case Message.Type.CopyIn:
                    request = Deserialize<CopyInRequest>(message.Payload);
                    break;
                case Message.Type.CopyOut:
                    request = Deserialize<CopyOutRequest>(message.Payload);
                    break;
                case Message.Type.Create:
                    var createRequest = Deserialize<CreateRequest>(message.Payload);
                    createRequest.Rootfs = "Console"; // ironfoundry TODO TODO TODO
                    request = createRequest;
                    break;
                case Message.Type.Destroy:
                    request = Deserialize<DestroyRequest>(message.Payload);
                    break;
                case Message.Type.Echo:
                    request = Deserialize<EchoRequest>(message.Payload);
                    break;
                case Message.Type.Info:
                    request = Deserialize<InfoRequest>(message.Payload);
                    break;
                case Message.Type.LimitBandwidth:
                    request = Deserialize<LimitBandwidthRequest>(message.Payload);
                    break;
                case Message.Type.LimitDisk:
                    request = Deserialize<LimitDiskRequest>(message.Payload);
                    break;
                case Message.Type.LimitCpu:
                    request = Deserialize<LimitCpuRequest>(message.Payload);
                    break;
                case Message.Type.LimitMemory:
                    request = Deserialize<LimitMemoryRequest>(message.Payload);
                    break;
                case Message.Type.Link:
                    request = Deserialize<LinkRequest>(message.Payload);
                    break;
                case Message.Type.List:
                    request = new ListRequest();
                    break;
                case Message.Type.NetIn:
                    request = Deserialize<NetInRequest>(message.Payload);
                    break;
                case Message.Type.NetOut:
                    request = Deserialize<NetOutRequest>(message.Payload);
                    break;
                case Message.Type.Ping:
                    request = new PingRequest();
                    break;
                case Message.Type.Run:
                    request = Deserialize<RunRequest>(message.Payload);
                    break;
                case Message.Type.Spawn:
                    request = Deserialize<SpawnRequest>(message.Payload);
                    break;
                case Message.Type.Stop:
                    request = Deserialize<StopRequest>(message.Payload);
                    break;
                case Message.Type.Stream:
                    request = Deserialize<StreamRequest>(message.Payload);
                    break;
                case Message.Type.Logging:
                    request = Deserialize<LoggingRequest>(message.Payload);
                    break;
                default:
                    throw new WardenException("Can't unwrap message type '{0}'", message.MessageType);
            }

            return request;
        }

        private static T Deserialize<T>(byte[] payload)
        {
            using (var ms = new MemoryStream(payload))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }
    }
}
