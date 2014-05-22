namespace IronFoundry.Warden.Handlers
{
    using System.Threading.Tasks;
    using Containers;
    using NLog;
    using Protocol;
    using IronFoundry.Warden.Utilities;

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

            return Task.Run<Response>(async () =>
                {
                    var container = GetContainer();                    
                    var reservedPort = await container.ReservePortAsync(port);

                    return new NetInResponse { HostPort = (uint)reservedPort, ContainerPort = (uint)reservedPort };
                });
        }
    }
}
