using IronFoundry.Warden.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Containers
{
    public interface IResourceHolder
    {
        IContainerUser User { get; }
        IContainerDirectory Directory { get; }
        ContainerHandle Handle { get; }
        JobObject JobObject { get; }

        void Destroy();
    }

    public class ContainerResourceHolder : IResourceHolder
    {
        private static int TerminateWaitTimeout = 2000; // ms

        public ContainerResourceHolder(ContainerHandle handle, IContainerUser user, IContainerDirectory directory, JobObject jobObject)
        {
            this.Handle = handle;
            this.User = user;
            this.Directory = directory;
            this.JobObject = jobObject;
        }

        public static IResourceHolder Create(IWardenConfig config)
        {
            var handle = new ContainerHandle();
            var user = new ContainerUser(handle, true);
            var directory = new ContainerDirectory(handle, user, true, config);

            var resoureHolder = new ContainerResourceHolder(
                handle, 
                user, 
                directory, 
                new JobObject(handle.ToString())
            );

            return resoureHolder;
        }

        public IContainerUser User { get; private set; }
        public IContainerDirectory Directory { get; private set; }
        public ContainerHandle Handle { get; private set; }
        public JobObject JobObject { get; private set; }

        public void Destroy()
        {
            JobObject.TerminateProcessesAndWait(TerminateWaitTimeout);
            JobObject.Dispose();
            
            Directory.Delete();
            User.Delete();
        }
    }
}
