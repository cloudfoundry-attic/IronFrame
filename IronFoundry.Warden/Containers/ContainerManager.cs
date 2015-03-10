using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IronFoundry.Container;
using IronFoundry.Warden.Utilities;
using NLog;

namespace IronFoundry.Warden.Containers
{
    public class ContainerManager : IContainerManager
    {
        private readonly ConcurrentDictionary<ContainerHandle, IContainerClient> containerClients =
            new ConcurrentDictionary<ContainerHandle, IContainerClient>();

        private readonly Logger log = LogManager.GetCurrentClassLogger();

        public ContainerManager()
        {
        }

        public IEnumerable<ContainerHandle> Handles
        {
            get { return containerClients.Keys; }
        }

        public void AddContainer(IContainerClient container)
        {
            if (!containerClients.TryAdd(container.Handle, container))
            {
                throw new WardenException("Could not add container '{0}' to collection!", container);
            }
        }

        public IContainerClient GetContainer(string handle)
        {
            var cHandle = new ContainerHandle(handle);
            IContainerClient retrieved;
            if (!containerClients.TryGetValue(cHandle, out retrieved))
            {
                // TODO: throw exception with message that matches ruby warden
                log.Warn("Expected to find container with handle '{0}'", handle);
            }
            return retrieved;
        }

        public void RestoreContainers(string containerRoot, string wardenUsersGroup)
        {
            if (Directory.Exists(containerRoot))
            {
                var fileSystem = new FileSystemManager();
                var containerService = ContainerService.RestoreFromContainerBasePath(containerRoot, wardenUsersGroup);

                var containers = containerService.GetContainers();

                // Recover containers primarily for deletion
                foreach (var container in containers)
                {
                    try
                    {
                        var containerClient = new ContainerClient(containerService, container, fileSystem);
                        containerClients.TryAdd(containerClient.Handle, containerClient);
                    }
                    catch (Exception ex)
                    {
                        log.ErrorException(ex);
                    }
                }

                Task.Run(async () => 
                {
                    try
                    {
                        await RemoveAllContainersAsync();
                    }
                    catch (Exception ex)
                    {
                        log.ErrorException(ex);
                    }
                });
            }
        }

        public async Task DestroyContainerAsync(IContainerClient container)
        {
            await DestroyContainerAsync(container.Handle);
        }

        public async Task DestroyContainerAsync(ContainerHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException("handle");
            }

            IContainerClient removed;
            if (containerClients.TryRemove(handle, out removed))
            {
                await removed.Destroy();
            }
        }

        public void Dispose()
        {
            // TODO - serialize, clear collection
        }

        private async Task RemoveAllContainersAsync()
        {
            foreach (IContainerClient client in containerClients.Values)
            {
                log.Info("Destroying stale container '{0}'", client.Handle);
                await DestroyContainerAsync(client);
            }
        }
    }
}