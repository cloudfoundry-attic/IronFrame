using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using ICSharpCode.SharpZipLib.GZip;

namespace IronFoundry.Warden.Utilities
{
    public class FileSystemManagerTests
    {
        public class FileSystemManagerTestContext
        {
            protected PlatformFileSystem fileSystem;
            protected FileSystemManager manager;

            public FileSystemManagerTestContext()
            {
                fileSystem = Substitute.For<PlatformFileSystem>();
                manager = new FileSystemManager(fileSystem);
            }
        }

        public class Copy : FileSystemManagerTestContext
        {
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

        public class CopyFile : FileSystemManagerTestContext
        {
            [Fact]
            public void CopiesFile()
            {
                manager.CopyFile("source", "destination");

                fileSystem.Received(x => x.Copy("source", "destination", true));
            }

            [Fact]
            public void CreatesDestinationDirectoriesIfNecessary()
            {
                manager.CopyFile("source", @"path\to\destination");

                fileSystem.Received(x => x.CreateDirectory(@"path\to"));
            }

            [Fact]
            public void WhenSourcePathExists_ThrowsIfSourceIsADirectory()
            {
                fileSystem.Exists("source").Returns(true);
                fileSystem.GetAttributes("source").Returns(FileAttributes.Directory);

                var ex = Record.Exception(() => manager.CopyFile("source", "destination"));
                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public void WhenDestinationPathExists_ThrowsIfDestinationIsADirectory()
            {
                fileSystem.Exists("destination").Returns(true);
                fileSystem.GetAttributes("destination").Returns(FileAttributes.Directory);

                var ex = Record.Exception(() => manager.CopyFile("source", "destination"));
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        public class ExtractTarFile : FileSystemManagerTestContext
        {
            [Fact]
            public void ExtractsTarFile()
            {
                Stream tarStream = new MemoryStream();
                fileSystem.OpenRead("source.tar").Returns(tarStream);

                manager.ExtractTarFile("source.tar", "destination", false);

                fileSystem.Received(x => x.ExtractTarArchive(tarStream, "destination"));
            }

            [Fact]
            public void DecomrpessesTarFile()
            {
                Stream tarStream = new MemoryStream();
                fileSystem.OpenRead("source.tar").Returns(tarStream);

                manager.ExtractTarFile("source.tar", "destination", true);

                fileSystem.Received(x => x.ExtractTarArchive(
                    Arg.Any<GZipInputStream>(), 
                    "destination"));
            }

            [Fact]
            public void CreatesDestinationDirectoriesIfNecessary()
            {
                Stream tarStream = new MemoryStream();
                fileSystem.OpenRead("source.tar").Returns(tarStream);

                manager.ExtractTarFile("source.tar", @"path\to\destination", false);

                fileSystem.Received(x => x.CreateDirectory(@"path\to\destination"));
                fileSystem.Received(x => x.ExtractTarArchive(tarStream, @"path\to\destination"));
            }

            [Fact]
            public void WhenDestinationPathExists_ThrowsIfDestinationIsAFile()
            {
                fileSystem.Exists("destination").Returns(true);
                fileSystem.GetAttributes("destination").Returns(FileAttributes.Normal);

                var ex = Record.Exception(() => manager.ExtractTarFile("source.tar", "destination", false));
                Assert.IsType<InvalidOperationException>(ex);
            }
        }
    }
}
