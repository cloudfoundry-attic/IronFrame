using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Shared.Data;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Containers
{
    public interface IContainer
    {
        string ContainerDirectoryPath { get; }
        string ContainerUserName { get; }
        ContainerHandle Handle { get; }
        ContainerState State { get; }

        void BindMounts(IEnumerable<BindMount> mounts);
        IProcess CreateProcess(CreateProcessStartInfo si, bool impersonate = false);
        void Destroy();
        WindowsImpersonationContext GetExecutionContext(bool shouldImpersonate = false);
        ContainerInfo GetInfo();

        void Initialize(IContainerDirectory containerDirectory, ContainerHandle containerHandle, IContainerUser userInfo);
        void Stop(bool kill);

        int ReservePort(int requestedPort); 
    }
}
