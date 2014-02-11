namespace IronFoundry.Warden.Handlers
{
    using System.Threading.Tasks;
    using Containers;
    using NLog;
    using Protocol;

    public class NetInRequestHandler : ContainerRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly NetInRequest request;

        public NetInRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
            this.request = (NetInRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            /*
             * This sets up networking INTO the container. For linux, the following is executed:
             
             warden/warden/root/linux/skeleton/net.sh
              
             iptables -t nat -A ${nat_instance_chain} \
               --protocol tcp \
               --destination-port "${HOST_PORT}" \
               --jump DNAT \
               --to-destination "${network_container_ip}:${CONTAINER_PORT}"
             */
            log.Trace("Handle: '{0}' ContainerPort: '{1}' HostPort: '{2}'", request.Handle, request.ContainerPort, request.HostPort);

            ushort port = 0;
            if (request.ContainerPortSpecified)
            {
                port = (ushort)request.ContainerPort;
            }
            else if (request.HostPortSpecified)
            {
                port = (ushort)request.HostPort;
            }

            return Task.Run<Response>(() =>
                {
                    Container container = GetContainer();
                    ushort reservedPort = container.ReservePort(port);
                    return new NetInResponse { HostPort = reservedPort, ContainerPort = reservedPort };
                });
        }
    }
}
