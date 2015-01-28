using System;
using System.Collections.Generic;
using System.Linq;
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

    public interface IContainerService : IDisposable
    {
        IContainer CreateContainer(ContainerSpec containerSpec);
        void DestroyContainer(string handle);
        IContainer GetContainerByHandle(string handle);
        IReadOnlyList<IContainer> GetContainers();
    }

    public class ContainerService : IContainerService
    {
        readonly string containerBasePath;
        readonly FileSystemManager fileSystem;
        readonly ContainerHandleHelper handleHelper;
        readonly IUserManager userManager;
        readonly ILocalTcpPortManager tcpPortManager;
        readonly IProcessRunner processRunner;
        readonly IContainerHostService containerHostService;
        private List<Container> containers = new List<Container>();

        public ContainerService(
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

        public ContainerService(string containerBasePath, string userGroupName)
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

            UndoStack undoStack = new UndoStack();
            Container container;

            try
            {
                var handle = containerSpec.Handle;
                if (String.IsNullOrEmpty(handle))
                  handle = handleHelper.GenerateHandle();

                var id = handleHelper.GenerateId(handle);

                var user = ContainerUser.Create(userManager, id);
                undoStack.Push(() => user.Delete());

                var directory = ContainerDirectory.Create(fileSystem, containerBasePath, id, user);
                undoStack.Push(() => fileSystem.DeleteDirectory(containerBasePath));

                var jobObjectName = handle;
                var jobObject = new JobObject(jobObjectName);
                undoStack.Push(() => jobObject.Dispose());

                var containerHostClient = containerHostService.StartContainerHost(id, directory, jobObject, user.GetCredential());
                undoStack.Push(() => containerHostClient.Shutdown());

                var constrainedProcessRunner = new ConstrainedProcessRunner(containerHostClient);
                undoStack.Push(() => constrainedProcessRunner.Dispose());

                var processHelper = new ProcessHelper();

                container = new Container(id, handle, user, directory, tcpPortManager, jobObject, processRunner, constrainedProcessRunner, processHelper, containerSpec.Environment);
                containers.Add(container);
            }
            catch (Exception e)
            {
                try
                {
                    undoStack.UndoAll();
                    throw;
                }
                catch (AggregateException undoException)
                {
                    throw new AggregateException(new [] { e, undoException });
                }
            }

            return container;
        }
        
        public void DestroyContainer(string handle)
        {
            var container = FindContainer(handle);
            if (container != null)
            {
                container.Destroy();
                containers.Remove(container);
            }
        }

        public void Dispose()
        {
        }

        Container FindContainer(string handle)
        {
            return containers.Find(x => x.Handle.Equals(handle, StringComparison.OrdinalIgnoreCase));
        }

        public IContainer GetContainerByHandle(string handle)
        {
            return FindContainer(handle);
        }

        public IReadOnlyList<IContainer> GetContainers()
        {
            return containers;
        }
    }
}
