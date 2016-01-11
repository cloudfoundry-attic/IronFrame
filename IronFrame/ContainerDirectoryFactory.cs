using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFrame.Utilities;

namespace IronFrame
{
    internal interface IContainerDirectoryFactory
    {
        IContainerDirectory Create(IFileSystemManager fileSystem, string containerBasePath, string containerHandle);
    }

    class ContainerDirectoryFactory : IContainerDirectoryFactory
    {
        public IContainerDirectory Create(IFileSystemManager fileSystem, string containerBasePath, string containerHandle)
        {
            // TODO: Sanitize the container handle for use in the filesystem
            var containerPath = Path.Combine(containerBasePath, containerHandle);
            return new ContainerDirectory(fileSystem, containerPath);
        }
    }
}
