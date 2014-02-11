namespace IronFoundry.Warden.Containers
{
    using System;
    using Protocol;
    using Utilities;

    public class InfoBuilder
    {
        private readonly Container container;

        public InfoBuilder(Container container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            this.container = container;
        }

        public InfoResponse GetInfoResponse()
        {
            var hostIp = IPUtilities.GetLocalIPAddress().ToString();
            return new InfoResponse(hostIp, hostIp, container.Directory, container.State);
        }
    }
}
