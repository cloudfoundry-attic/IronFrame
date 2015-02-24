using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Utilities;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using IronFoundry.Container;

namespace IronFoundry.Warden.Containers
{
    public interface IContainerClient
    {
        string ContainerDirectoryPath { get; }
        ContainerHandle Handle { get; }
        int? AssignedPort { get; }

        Task CopyAsync(string source, string destination);
        IEnumerable<string> DrainEvents();
        void EnableLogging(ILogEmitter logEmitter);
        Task<ContainerInfo> GetInfoAsync();        
        Task InitializeAsync(string baseDirectory, string handle, string userGroup);
        Task LimitMemoryAsync(ulong bytes);
        Task<int> ReservePortAsync(int port);
        Task<CommandResult> RunCommandAsync(RemoteCommandArgs commandArgs);
        Task StopAsync(bool kill);
        Task Destroy();
    }
}
