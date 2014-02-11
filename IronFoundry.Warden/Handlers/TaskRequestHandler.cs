using System;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Jobs;
using IronFoundry.Warden.Protocol;
using IronFoundry.Warden.Tasks;

namespace IronFoundry.Warden.Handlers
{
    public abstract class TaskRequestHandler : ContainerRequestHandler
    {
        public TaskRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
        }

        protected IJobRunnable GetRunnableFor(ITaskRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            Container container = GetContainer();
            return new TaskRunner(container, request);
        }
    }
}
