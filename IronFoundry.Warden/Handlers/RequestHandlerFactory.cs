namespace IronFoundry.Warden.Handlers
{
    using System;
    using IronFoundry.Container;
    using IronFoundry.Warden.Containers;
    using IronFoundry.Warden.Jobs;
    using IronFoundry.Warden.Protocol;

    public class RequestHandlerFactory
    {
        private readonly IContainerManager containerManager;
        private readonly IJobManager jobManager;
        private readonly Message.Type requestType;
        private readonly Request request;

        public RequestHandlerFactory(IContainerManager containerManager, IJobManager jobManager, Message.Type requestType, Request request)
        {
            if (containerManager == null)
            {
                throw new ArgumentNullException("containerManager");
            }
            if (jobManager == null)
            {
                throw new ArgumentNullException("jobManager");
            }
            if (requestType == default(Message.Type))
            {
                throw new ArgumentNullException("requestType");
            }
            if (request == null)
            {
                throw new ArgumentNullException("message");
            }
            this.containerManager = containerManager;
            this.jobManager = jobManager;
            this.requestType = requestType;
            this.request = request;
        }

        public RequestHandler GetHandler()
        {
            RequestHandler handler = null;

            switch (requestType)
            {
                case Message.Type.CopyIn:
                    handler = new CopyInRequestHandler(containerManager, request);
                    break;
                case Message.Type.CopyOut:
                    handler = new CopyOutRequestHandler(containerManager, request);
                    break;
                case Message.Type.Create:
                    handler = new CreateRequestHandler(containerManager, request);
                    break;
                case Message.Type.Destroy:
                    handler = new DestroyRequestHandler(containerManager, request);
                    break;
                case Message.Type.Echo:
                    handler = new EchoRequestHandler(request);
                    break;
                case Message.Type.Info:
                    handler = new InfoRequestHandler(containerManager, request);
                    break;
                case Message.Type.LimitBandwidth:
                    handler = new LimitBandwidthRequestHandler(request);
                    break;
                case Message.Type.LimitDisk:
                    handler = new LimitDiskRequestHandler(request);
                    break;
                case Message.Type.LimitCpu:
                    handler = new LimitCpuRequestHandler(request);
                    break;
                case Message.Type.LimitMemory:
                    handler = new LimitMemoryRequestHandler(containerManager, request);
                    break;
                case Message.Type.Link:
                    handler = new LinkRequestHandler(containerManager, jobManager, request);
                    break;
                case Message.Type.List:
                    handler = new ListRequestHandler(containerManager, request);
                    break;
                case Message.Type.NetIn:
                    handler = new NetInRequestHandler(containerManager, request);
                    break;
                case Message.Type.NetOut:
                    handler = new NetOutRequestHandler(request);
                    break;
                case Message.Type.Ping:
                    handler = new PingRequestHandler(request);
                    break;
                case Message.Type.Run:
                    handler = new RunRequestHandler(containerManager, request);
                    break;
                case Message.Type.Spawn:
                    handler = new SpawnRequestHandler(containerManager, jobManager, request);
                    break;
                case Message.Type.Stop:
                    handler = new StopRequestHandler(containerManager, request);
                    break;
                case Message.Type.Stream:
                    handler = new StreamRequestHandler(containerManager, jobManager, request);
                    break;
                case Message.Type.Logging:
                    handler = new LoggingRequestHandler(containerManager, request);
                    break;
                default:
                    throw new WardenException("Unknown request type '{0}' passed to handler factory.", requestType);
            }

            return handler;
        }
    }
}
