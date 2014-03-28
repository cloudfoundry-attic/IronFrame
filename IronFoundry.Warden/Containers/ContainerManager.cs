namespace IronFoundry.Warden.Containers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NLog;
    using Utilities;

    public class ContainerManager : IContainerManager
    {
        private readonly ConcurrentDictionary<ContainerHandle, IContainerClient> containers =
            new ConcurrentDictionary<ContainerHandle, IContainerClient>();

        private readonly Logger log = LogManager.GetCurrentClassLogger();

        public IEnumerable<ContainerHandle> Handles
        {
            get { return containers.Keys; }
        }

        public void AddContainer(IContainerClient container)
        {
            if (!containers.TryAdd(container.Handle, container))
            {
                throw new WardenException("Could not add container '{0}' to collection!", container);
            }
        }

        public IContainerClient GetContainer(string handle)
        {
            var cHandle = new ContainerHandle(handle);
            IContainerClient retrieved;
            if (!containers.TryGetValue(cHandle, out retrieved))
            {
                // TODO: throw exception with message that matches ruby warden
                log.Warn("Expected to find container with handle '{0}'", handle);
            }
            return retrieved;
        }

        public void RestoreContainers(string containerRoot)
        {
            if (Directory.Exists(containerRoot))
            {
                Task.Run(() =>
                    {
                        // TODO: use snapshot rather than directories
                        // when using separate container.exe check for that process' existence
                        foreach (var dirPath in Directory.GetDirectories(containerRoot))
                        {
                            var handle = Path.GetFileName(dirPath);
                            try
                            {
                                var container = ContainerProxy.Restore(handle, ContainerState.Active);
                                containers.TryAdd(container.Handle, container);
                            }
                            catch (Exception ex)
                            {
                                ContainerProxy.CleanUp(handle);
                                log.ErrorException(ex);
                            }
                        }

                        try
                        {
                            RemoveEmptyContainers(); // TODO is this really what we should do on startup?
                        }
                        catch (Exception ex)
                        {
                            log.ErrorException(ex);
                        }
                    });
            }
        }

        private void RemoveEmptyContainers()
        {
            containers.Values.Foreach(log, (c) => c.State == ContainerState.Destroyed,
                (c) =>
                {
                    log.Info("Destroying stale container '{0}'", c.Handle);
                    DestroyContainer(c);
                });
        }

        public void DestroyContainer(IContainerClient container)
        {
            DestroyContainer(container.Handle);
        }

        public void DestroyContainer(ContainerHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException("handle");
            }

            IContainerClient removed;
            if (containers.TryRemove(handle, out removed))
            {
                try
                {
                    removed.DestroyAsync();
                }
                catch
                {
                    log.Error("Error destroying container! Re-adding to collection.");
                    containers[handle] = removed;
                    throw;
                }
            }
            else
            {
                throw new WardenException("Could not remove container '{0}' from collection!", handle);
            }
        }

        public void Dispose()
        {
            // TODO - serialize, clear collection
        }
    }
}
