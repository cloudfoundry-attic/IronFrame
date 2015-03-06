using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Containers
{
    public interface IContainerManager : IDisposable
    {
        IEnumerable<ContainerHandle> Handles { get; }
        Task DestroyContainerAsync(ContainerHandle handle);
        Task DestroyContainerAsync(IContainerClient container);
        void AddContainer(IContainerClient container);
        void RestoreContainers(string containerRoot, string wardenUsersGroup);
        IContainerClient GetContainer(string handle);
    }
}