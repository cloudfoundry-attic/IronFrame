namespace IronFoundry.Warden.Containers
{
    using System;
    using System.Collections.Generic;

    public interface IContainerManager : IDisposable
    {
        void DestroyContainer(ContainerHandle handle);
        void DestroyContainer(IContainer container);
        void AddContainer(IContainer container);
        void RestoreContainers(string containerRoot);
        IEnumerable<ContainerHandle> Handles { get; }
        IContainer GetContainer(string handle);
    }
}
