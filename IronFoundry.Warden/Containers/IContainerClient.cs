using IronFoundry.Warden.Shared.Data;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Utilities;
using System;
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
        Task<ProcessStats> GetProcessStatisticsAsync();
        void Initialize(IResourceHolder containerResources);
        int ReservePort(int port);
        Task<CommandResult> RunCommandAsync(RemoteCommand command);        
        void Stop();        
    }
}
