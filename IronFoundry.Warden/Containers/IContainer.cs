using IronFoundry.Container;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Container.Messaging;
using IronFoundry.Container.Utilities;
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
        void CreateTarFile(string sourcePath, string tarFilePath, bool compress);
        void Copy(string source, string destination);
        void CopyFileIn(string sourceFilePath, string destinationFilePath);
        void CopyFileOut(string sourceFilePath, string destinationFilePath);
        void ExtractTarFile(string tarFilePath, string destinationPath, bool decompress);

        IProcess CreateProcess(CreateProcessStartInfo si, bool impersonate = false);
        WindowsImpersonationContext GetExecutionContext(bool shouldImpersonate = false);
        ContainerInfo GetInfo();

        void Initialize(IContainerDirectory containerDirectory, ContainerHandle containerHandle, IContainerUser userInfo);
        void Stop(bool kill);

        int ReservePort(int requestedPort); 
    }
}
