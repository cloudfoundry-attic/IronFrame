namespace IronFoundry.Warden.Containers
{
    using System;
    using System.Collections.Generic;

    public interface IContainerManager : IDisposable
    {
        void DestroyContainer(ContainerHandle handle);
        void DestroyContainer(Container container);
        void AddContainer(Container container);
        void RestoreContainers(string containerRoot);
        IEnumerable<ContainerHandle> Handles { get; }
        Container GetContainer(string handle);
    }
}
