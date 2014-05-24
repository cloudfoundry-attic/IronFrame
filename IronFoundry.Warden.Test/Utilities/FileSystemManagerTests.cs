using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFoundry.Warden.Utilities
{
    public class FileSystemManagerTests
    {
        private PlatformFileSystem fileSystem;
        private FileSystemManager manager;

        public FileSystemManagerTests()
        {
            fileSystem = Substitute.For<PlatformFileSystem>();
            manager = new FileSystemManager(fileSystem);
        }

        [Fact]
        public void CopiesOneFileToAnother()
        {
            manager.Copy("source", "destination");

            fileSystem.Received(x => x.Copy("source", "destination", true));
        }

        [Fact]
        public void CopiesOneDirectoryToAnother()
        {
            fileSystem.GetAttributes("source").Returns(System.IO.FileAttributes.Directory);
            fileSystem.GetAttributes("destination").Returns(System.IO.FileAttributes.Directory);

            manager.Copy("source", "destination");

            fileSystem.Received(x => x.CopyDirectory("source", "destination", true));
        }

        [Fact]
        public void CopiesOneFileToDirectory()
        {
            fileSystem.GetFileName("source").Returns("source");
            fileSystem.GetAttributes("destination").Returns(System.IO.FileAttributes.Directory);

            manager.Copy("source", "destination");

            fileSystem.Received(x => x.Copy("source", @"destination\source", true));
        }

        [Fact]
        public void CopyDirectoryToFileThrows()
        {
            fileSystem.GetAttributes("source").Returns(System.IO.FileAttributes.Directory);

            var except = Record.Exception(() => manager.Copy("source", "destination"));
            Assert.IsType<InvalidOperationException>(except);
        }
    }
}
