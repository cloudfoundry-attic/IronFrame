namespace IronFoundry.Warden.Handlers
{
    using System;
    using Containers;
    using Jobs;
    using Protocol;

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
