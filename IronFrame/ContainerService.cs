using DiskQuotaTypeLibrary;
using IronFrame.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace IronFrame
{
    public interface IContainerService
    {
        IContainer CreateContainer(ContainerSpec containerSpec);
        void DestroyContainer(string handle);
        IContainer GetContainerByHandle(string handle);
        IContainer GetContainerByHandleIncludingDestroyed(string handle);
        IReadOnlyList<IContainer> GetContainers();
    }

    public sealed class ContainerSpec
    {
        public string Handle { get; set; }
        public BindMount[] BindMounts { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, string> Environment { get; set; }

        public ContainerSpec()
        {
            this.BindMounts = new BindMount[]{};
        }
    }

    public sealed class ContainerService : IContainerService
    {
        const string IIS_USRS_GROUP = "IIS_IUSRS";
        const string PropertiesFileName = "properties.json";

        readonly string containerBasePath;
        readonly IFileSystemManager fileSystem;
        readonly ContainerHandleHelper handleHelper;
        readonly IUserManager userManager;
        readonly ILocalTcpPortManager tcpPortManager;
        readonly IProcessRunner processRunner;
        readonly IContainerPropertyService containerPropertiesService;
        readonly IContainerHostService containerHostService;
        readonly IDiskQuotaManager diskQuotaManager;
        readonly List<IContainer> containers = new List<IContainer>();
        private IContainerDirectoryFactory directoryFactory;
        private IContainerFactory containerFactory;
        private HashSet<IContainer> destroyingContainers = new HashSet<IContainer>();

        internal ContainerService(ContainerHandleHelper handleHelper, IUserManager userManager, IFileSystemManager fileSystem, IContainerPropertyService containerPropertiesService, ILocalTcpPortManager tcpPortManager, IProcessRunner processRunner, IContainerHostService containerHostService, IDiskQuotaManager diskQuotaManager, IContainerDirectoryFactory directoryFactory, IContainerFactory containerFactory, string containerBasePath)
        {
            this.handleHelper = handleHelper;
            this.userManager = userManager;
            this.fileSystem = fileSystem;
            this.containerPropertiesService = containerPropertiesService;
            this.tcpPortManager = tcpPortManager;
            this.processRunner = processRunner;
            this.containerHostService = containerHostService;
            this.containerBasePath = containerBasePath;
            this.diskQuotaManager = diskQuotaManager;
            this.directoryFactory = directoryFactory;
            this.containerFactory = containerFactory;
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
                new DiskQuotaManager(),
                new ContainerDirectoryFactory(),
                new ContainerFactory(),
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

                user.CreateProfile();
                undoStack.Push(() => user.DeleteProfile());

                var directory = directoryFactory.Create(fileSystem, containerBasePath, id);
                directory.CreateSubdirectories(user);
                undoStack.Push(directory.Destroy);

                directory.CreateBindMounts(containerSpec.BindMounts, user);

                var jobObject = new JobObject(id);
                undoStack.Push(() => jobObject.Dispose());

                var containerHostClient = containerHostService.StartContainerHost(id, directory, jobObject, user.GetCredential());
                undoStack.Push(() => containerHostClient.Shutdown());


                var constrainedProcessRunner = new ConstrainedProcessRunner(containerHostClient);
                undoStack.Push(() => constrainedProcessRunner.Dispose());

                var processHelper = new ProcessHelper();
                var dependencyHelper = new ContainerHostDependencyHelper();

                var diskQuotaControl = diskQuotaManager.CreateDiskQuotaControl(directory, user.SID);

                container = containerFactory.CreateContainer(
                    id,
                    handle,
                    user,
                    directory,
                    containerPropertiesService,
                    tcpPortManager,
                    jobObject,
                    diskQuotaControl,
                    processRunner,
                    constrainedProcessRunner,
                    processHelper,
                    containerSpec.Environment,
                    dependencyHelper);

                containerPropertiesService.SetProperties(container, containerSpec.Properties);
                lock (containers)
                {
                    containers.Add(container);
                }
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
                    throw new AggregateException(new[] { e, undoException });
                }
            }

            return container;
        }

        public void DestroyContainer(string handle)
        {
            var container = FindContainer(handle, true);
            if (container != null)
            {
                lock (containers)
                {
                    containers.Remove(container);

                    destroyingContainers.Add(container);
                    container.Destroy();
                    destroyingContainers.Remove(container);
                }
            }
        }

        IContainer FindContainer(string handle, bool includeDestroyedContainers)
        {
            lock (containers)
            {
                var containersToSearch = containers;
                if (includeDestroyedContainers)
                    containersToSearch = containersToSearch.Concat(destroyingContainers).ToList();
                return containersToSearch.Find(x => x.Handle.Equals(handle, StringComparison.OrdinalIgnoreCase));
            }
        }

        public IContainer GetContainerByHandleIncludingDestroyed(string handle)
        {
            return FindContainer(handle, true);
        }

        public IContainer GetContainerByHandle(string handle)
        {
            return FindContainer(handle, false);
        }

        public IReadOnlyList<IContainer> GetContainers()
        {
            lock (containers)
            {
                return containers.Concat(destroyingContainers).ToArray();
            }
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

            var containerDiskQuota = diskQuotaManager.CreateDiskQuotaControl(directory, user.SID);

            var dependencyHelper = new ContainerHostDependencyHelper();

            var container = new Container(
                id,
                id, // TODO: Recover the handle from container metadata
                user,
                directory,
                containerPropertiesService,
                tcpPortManager,
                jobObject,
                containerDiskQuota,
                processRunner,
                processRunner,
                processHelper,
                environment,
                dependencyHelper);

            return container;
        }

        internal void RestoreFromContainerBasePath()
        {
            foreach (var containerPath in fileSystem.EnumerateDirectories(containerBasePath))
            {
                var container = RestoreContainerFromPath(containerPath);

                lock (containers)
                {
                    containers.Add(container);
                }
            }
        }

        public static Dictionary<string, string> EnvsFromList(List<string> environmentVariables)
        {
            return environmentVariables
                .Select(x => x.Split(new char[]{'='}, 2))
                .ToDictionary(x => x[0], x => x[1]);
        }

        /// <summary>
        /// NOTE: This method is for use by the Warden. Please do not use otherwise.
        /// More work is required to make this a generally usable method.
        /// </summary>
        /// <param name="containerBasePath"></param>
        /// <param name="userGroupName"></param>
        /// <returns></returns>
        public static ContainerService Warden_RestoreFromContainerBasePath(string containerBasePath, string userGroupName)
        {
            var containerService = new ContainerService(containerBasePath, userGroupName);

            containerService.RestoreFromContainerBasePath();

            return containerService;
        }
    }

    internal class ContainerFactory : IContainerFactory
    {
        public IContainer CreateContainer(string id,
            string handle,
            IContainerUser user,
            IContainerDirectory directory,
            IContainerPropertyService propertyService,
            ILocalTcpPortManager tcpPortManager,
            JobObject jobObject,
            IContainerDiskQuota containerDiskQuota,
            IProcessRunner processRunner,
            IProcessRunner constrainedProcessRunner,
            ProcessHelper processHelper,
            Dictionary<string, string> defaultEnvironment,
            ContainerHostDependencyHelper dependencyHelper)
        {
            return new Container(
                id,
                handle,
                user,
                directory,
                propertyService,
                tcpPortManager,
                jobObject,
                containerDiskQuota,
                processRunner,
                constrainedProcessRunner,
                processHelper,
                defaultEnvironment,
                dependencyHelper
            );
        }
    }

    internal interface IContainerFactory
    {
        IContainer CreateContainer(
            string id,
            string handle,
            IContainerUser user,
            IContainerDirectory directory,
            IContainerPropertyService propertyService,
            ILocalTcpPortManager tcpPortManager,
            JobObject jobObject,
            IContainerDiskQuota containerDiskQuota,
            IProcessRunner processRunner,
            IProcessRunner constrainedProcessRunner,
            ProcessHelper processHelper,
            Dictionary<string, string> defaultEnvironment,
            ContainerHostDependencyHelper dependencyHelper
        );
    }
}
