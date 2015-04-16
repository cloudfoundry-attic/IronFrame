using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IronFoundry.Container.Utilities;

namespace IronFoundry.Container
{
    public interface IContainerService : IDisposable
    {
        IContainer CreateContainer(ContainerSpec containerSpec);
        void DestroyContainer(string handle);
        IContainer GetContainerByHandle(string handle);
        IReadOnlyList<IContainer> GetContainers();
    }

    public sealed class ContainerSpec
    {
        public string Handle { get; set; }
        public BindMount[] BindMounts { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, string> Environment { get; set; }
    }

    public sealed class ContainerService : IContainerService
    {
        const string IIS_USRS_GROUP = "IIS_IUSRS";
        const string PropertiesFileName = "properties.json";

        readonly string containerBasePath;
        readonly FileSystemManager fileSystem;
        readonly ContainerHandleHelper handleHelper;
        readonly IUserManager userManager;
        readonly ILocalTcpPortManager tcpPortManager;
        readonly IProcessRunner processRunner;
        readonly IContainerPropertyService containerPropertiesService;
        readonly IContainerHostService containerHostService;
        readonly List<IContainer> containers = new List<IContainer>();

        internal ContainerService(
            ContainerHandleHelper handleHelper,
            IUserManager userManager,
            FileSystemManager fileSystem,
            IContainerPropertyService containerPropertiesService,
            ILocalTcpPortManager tcpPortManager,
            IProcessRunner processRunner,
            IContainerHostService containerHostService,
            string containerBasePath
            )
        {
            this.handleHelper = handleHelper;
            this.userManager = userManager;
            this.fileSystem = fileSystem;
            this.containerPropertiesService = containerPropertiesService;
            this.tcpPortManager = tcpPortManager;
            this.processRunner = processRunner;
            this.containerHostService = containerHostService;
            this.containerBasePath = containerBasePath;
        }

        public ContainerService(string containerBasePath, string userGroupName)
            : this(
                new ContainerHandleHelper(),
                new LocalPrincipalManager(userGroupName, IIS_USRS_GROUP),
                new FileSystemManager(),
                new LocalFilePropertyService(new FileSystemManager(), PropertiesFileName),
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
            IContainer container;

            try
            {
                var handle = containerSpec.Handle;
                if (String.IsNullOrEmpty(handle))
                  handle = handleHelper.GenerateHandle();

                var id = handleHelper.GenerateId(handle);

                var user = ContainerUser.Create(userManager, id);
                undoStack.Push(() => user.Delete());

                var directory = ContainerDirectory.Create(fileSystem, containerBasePath, id, user);
                undoStack.Push(() => fileSystem.DeleteDirectory(directory.RootPath));

                var jobObject = new JobObject(id);
                undoStack.Push(() => jobObject.Dispose());

                var containerHostClient = containerHostService.StartContainerHost(id, directory, jobObject, user.GetCredential());
                undoStack.Push(() => containerHostClient.Shutdown());

                var constrainedProcessRunner = new ConstrainedProcessRunner(containerHostClient);
                undoStack.Push(() => constrainedProcessRunner.Dispose());

                var processHelper = new ProcessHelper();

                container = new Container(
                    id, 
                    handle, 
                    user, 
                    directory, 
                    containerPropertiesService, 
                    tcpPortManager, 
                    jobObject, 
                    processRunner, 
                    constrainedProcessRunner, 
                    processHelper, 
                    containerSpec.Environment);

                containers.Add(container);

                containerPropertiesService.SetProperties(container, containerSpec.Properties);
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

        IContainer FindContainer(string handle)
        {
            return containers.Find(x => x.Handle.Equals(handle, StringComparison.OrdinalIgnoreCase));
        }

        public IContainer GetContainerByHandle(string handle)
        {
            return FindContainer(handle);
        }

        public IReadOnlyList<IContainer> GetContainers()
        {
            return containers.ToArray();
        }

        public IReadOnlyList<string> GetContainerHandles()
        {
            return containers.Select(x => x.Handle).ToList();
        }

        IContainer RestoreContainerFromPath(string containerPath)
        {
            var id = Path.GetFileName(containerPath);

            var user = ContainerUser.Restore(userManager, id);
            var directory = ContainerDirectory.Restore(fileSystem, containerPath);

            var jobObjectName = id;
            var jobObject = new JobObject(jobObjectName);

            var environment = new Dictionary<string, string>();
            var processHelper = new ProcessHelper();

            var container = new Container(
                id,
                id, // TODO: Recover the handle from container metadata
                user,
                directory,
                containerPropertiesService,
                tcpPortManager,
                jobObject,
                processRunner,
                processRunner,
                processHelper,
                environment);

            return container;
        }

        internal void RestoreFromContainerBasePath()
        {
            foreach (var containerPath in fileSystem.EnumerateDirectories(containerBasePath))
            {
                var container = RestoreContainerFromPath(containerPath);

                containers.Add(container);
            }
        }

        internal static ContainerService RestoreFromContainerBasePath(string containerBasePath, string userGroupName)
        {
            var containerService = new ContainerService(containerBasePath, userGroupName);

            containerService.RestoreFromContainerBasePath();

            return containerService;
        }
    }
}
