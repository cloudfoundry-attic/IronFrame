using System;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Jobs;
using IronFoundry.Warden.Protocol;

namespace IronFoundry.Warden.Handlers
{
    public abstract class JobRequestHandler : ContainerRequestHandler
    {
        protected readonly IJobManager jobManager;

        public JobRequestHandler(IContainerManager containerManager, IJobManager jobManager, Request request)
            : base(containerManager, request)
        {
            if (jobManager == null)
            {
                throw new ArgumentNullException("jobManager");
            }
            this.jobManager = jobManager;
        }
    }
}
