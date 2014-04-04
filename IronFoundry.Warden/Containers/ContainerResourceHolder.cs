using IronFoundry.Warden.Configuration;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Containers
{
    public interface IResourceHolder
    {
        ushort? AssignedPort { get; set; }
        IContainerUser User { get; }
        IContainerDirectory Directory { get; }
        ContainerHandle Handle { get; }
        JobObject JobObject { get; }
        ILocalTcpPortManager LocalTcpPortManager { get; }

        void Destroy();
    }

    public class ContainerResourceHolder : IResourceHolder
    {
        private const int TerminateWaitTimeout = 2000; // ms

        public ContainerResourceHolder(ContainerHandle handle, IContainerUser user, IContainerDirectory directory, JobObject jobObject, ILocalTcpPortManager localTcpPortManager)
        {
            Handle = handle;
            User = user;
            Directory = directory;
            JobObject = jobObject;
            LocalTcpPortManager = localTcpPortManager;
        }

        public ushort? AssignedPort { get; set; }
        public IContainerUser User { get; private set; }
        public IContainerDirectory Directory { get; private set; }
        public ContainerHandle Handle { get; private set; }
        public JobObject JobObject { get; private set; }
        public ILocalTcpPortManager LocalTcpPortManager { get; private set; }

        public void Destroy()
        {
            JobObject.TerminateProcessesAndWait(TerminateWaitTimeout);
            JobObject.Dispose();

            Directory.Delete();
            User.Delete();
            if (AssignedPort.HasValue)
            {
                LocalTcpPortManager.ReleaseLocalPort(AssignedPort.Value, User.UserName);
            }
        }

        public static IResourceHolder Create(IWardenConfig config)
        {
            var handle = new ContainerHandle();
            var user = ContainerUser.CreateUser(handle, new LocalPrincipalManager(new DesktopPermissionManager()));
            var directory = new ContainerDirectory(handle, user, true, config);
            var localPortManager = new LocalTcpPortManager(new FirewallManager(), new NetShRunner());
            var resoureHolder = new ContainerResourceHolder(
                handle,
                user,
                directory,
                new JobObject(handle.ToString()),
                localPortManager
                );

            return resoureHolder;
        }

        public static IResourceHolder Create(IWardenConfig config, ContainerHandle handle)
        {
            var user = ContainerUser.CreateUser(handle, new LocalPrincipalManager(new DesktopPermissionManager()));
            var directory = new ContainerDirectory(handle, user, true, config);
            var localPortManager = new LocalTcpPortManager(new FirewallManager(), new NetShRunner());
            var resoureHolder = new ContainerResourceHolder(
                handle,
                user,
                directory,
                new JobObject(handle.ToString()),
                localPortManager
                );

            return resoureHolder;
        }
    }
}