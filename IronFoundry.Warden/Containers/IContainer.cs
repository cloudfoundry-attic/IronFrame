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

        IProcess CreateProcess(CreateProcessStartInfo si, bool impersonate = false);
        void Destroy();
        WindowsImpersonationContext GetExecutionContext(bool shouldImpersonate = false);
        ProcessStats GetProcessStatistics();

        void Initialize(string containerDirectory, string containerHandle, IContainerUser userInfo);
        void Stop();

        void Initialize(); // Deprecating
        int ReservePort(int requestedPort); // Deprecating
    }
}
