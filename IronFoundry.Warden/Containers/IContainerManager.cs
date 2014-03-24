namespace IronFoundry.Warden.Containers
{
    using System;
    using System.Collections.Generic;

    public interface IContainerManager : IDisposable
    {
        void DestroyContainer(ContainerHandle handle);
        void DestroyContainer(IContainerClient container);
        void AddContainer(IContainerClient container);
        void RestoreContainers(string containerRoot);
        IEnumerable<ContainerHandle> Handles { get; }
        IContainerClient GetContainer(string handle);
    }
}
