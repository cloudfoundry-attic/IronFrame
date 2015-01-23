using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Container
{
    public class ContainerSpec
    {
        public string Handle { get; set; }
        public BindMount[] BindMounts { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, string> Environment { get; set; }
    }

    public interface IContainerCreationService : IDisposable
    {
        IContainer CreateContainer(ContainerSpec containerSpec);
    }

    public class ContainerCreationService : IContainerCreationService
    {
        readonly string containerBasePath;
        readonly FileSystemManager fileSystem;
        readonly IUserManager userManager;
        readonly ILocalTcpPortManager tcpPortManager;
        readonly IProcessRunner processRunner;
        readonly IContainerHostService containerHostService;

        public ContainerCreationService(
            IUserManager userManager,
            FileSystemManager fileSystem,
            ILocalTcpPortManager tcpPortManager,
            IProcessRunner processRunner,
            IContainerHostService containerHostService,
            string containerBasePath
            )
        {
            this.userManager = userManager;
            this.fileSystem = fileSystem;
            this.tcpPortManager = tcpPortManager;
            this.processRunner = processRunner;
            this.containerHostService = containerHostService;
            this.containerBasePath = containerBasePath;
        }

        public ContainerCreationService(string containerBasePath, string userGroupName)
        {
            var permissionManager = new DesktopPermissionManager();
            this.userManager = new LocalPrincipalManager(permissionManager, userGroupName);

            this.fileSystem = new FileSystemManager();
            this.tcpPortManager = new LocalTcpPortManager();
            this.processRunner = new ProcessRunner();
            this.containerHostService = new ContainerHostService();
            this.containerBasePath = containerBasePath;
        }

        public IContainer CreateContainer(ContainerSpec containerSpec)
        {
            Guard.NotNull(containerSpec, "containerSpec");

            var handle = containerSpec.Handle;
            if (String.IsNullOrEmpty(handle))
                handle = ContainerHandleGenerator.Generate();

            var user = ContainerUser.Create(userManager, handle);
            var directory = ContainerDirectory.Create(fileSystem, containerBasePath, handle, user);

            var jobObjectName = handle;
            var jobObject = new JobObject(jobObjectName);

            var containerHostClient = containerHostService.StartContainerHost(jobObject, user.GetCredential());

            var constrainedProcessRunner = new ConstrainedProcessRunner(containerHostClient);

            return new Container(handle, user, directory, tcpPortManager, jobObject, processRunner, constrainedProcessRunner);
        }

        public void Dispose()
        {
        }
    }
}
