using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Shared.Data;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Utilities;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
namespace IronFoundry.Warden.Containers
{
    public interface IContainerClient
    {
        string ContainerDirectoryPath { get; }
        ContainerHandle Handle { get; }
        ContainerState State { get; }

        Task DestroyAsync();
        IEnumerable<string> DrainEvents();
        Task EnableLoggingAsync(InstanceLoggingInfo loggingInfo);
        Task<ProcessStats> GetProcessStatisticsAsync();
        void Initialize(IResourceHolder containerResources);
        Task LimitMemoryAsync(ulong bytes);
        int ReservePort(int port);
        Task<CommandResult> RunCommandAsync(RemoteCommand command);
        Task StopAsync();
    }
}
