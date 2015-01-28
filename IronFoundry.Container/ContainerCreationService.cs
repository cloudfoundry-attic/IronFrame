using System;
using System.Collections.Generic;
using IronFoundry.Container.Utilities;

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
        readonly ContainerHandleHelper handleHelper;
        readonly IUserManager userManager;
        readonly ILocalTcpPortManager tcpPortManager;
        readonly IProcessRunner processRunner;
        readonly IContainerHostService containerHostService;

        public ContainerCreationService(
            ContainerHandleHelper handleHelper,
            IUserManager userManager,
            FileSystemManager fileSystem,
            ILocalTcpPortManager tcpPortManager,
            IProcessRunner processRunner,
            IContainerHostService containerHostService,
            string containerBasePath
            )
        {
            this.handleHelper = handleHelper;
            this.userManager = userManager;
            this.fileSystem = fileSystem;
            this.tcpPortManager = tcpPortManager;
            this.processRunner = processRunner;
            this.containerHostService = containerHostService;
            this.containerBasePath = containerBasePath;
        }

        public ContainerCreationService(string containerBasePath, string userGroupName)
            : this(
                new ContainerHandleHelper(),
                new LocalPrincipalManager(userGroupName),
                new FileSystemManager(),
                new LocalTcpPortManager(),
                new ProcessRunner(),
                new ContainerHostService(),
                containerBasePath
            )
        {
        }

        public IContainer CreateContainer(ContainerSpec containerSpec)
        {
            Guard.NotNull(containerSpec, "containerSpec");

            var handle = containerSpec.Handle;
            if (String.IsNullOrEmpty(handle))
                handle = handleHelper.GenerateHandle();

            var id = handleHelper.GenerateId(handle);
            var user = ContainerUser.Create(userManager, id);
            var directory = ContainerDirectory.Create(fileSystem, containerBasePath, id, user);

            var jobObject = new JobObject(id);

            var containerHostClient = containerHostService.StartContainerHost(id, directory, jobObject, user.GetCredential());

            var constrainedProcessRunner = new ConstrainedProcessRunner(containerHostClient);

            return new Container(id, handle, user, directory, tcpPortManager, jobObject, processRunner, constrainedProcessRunner);
        }

        public void Dispose()
        {
        }
    }
}
