using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using IronFrame.Utilities;
using NSubstitute;
using Xunit;

namespace IronFrame
{
    public class ContainerDirectoryTests
    {
        private ContainerDirectoryFactory factory;
        IContainerDirectory directory { get; set; }
        IFileSystemManager fileSystem { get; set; }

        public ContainerDirectoryTests()
        {
            fileSystem = Substitute.For<IFileSystemManager>();
            factory = new ContainerDirectoryFactory();
            directory = factory.Create(fileSystem, @"C:\Containers", "handle");
        }

        public class CreateSubdirectories : ContainerDirectoryTests
        {
            IContainerUser ContainerUser { get; set; }

            public CreateSubdirectories()
            {
                ContainerUser = Substitute.For<IContainerUser>();
                ContainerUser.UserName.Returns("username");
            }

            [Fact]
            public void CreatesContainerDirectoryWithUserReadOnlyPermissions()
            {
                IEnumerable<UserAccess> userAccess = null;
                fileSystem.CreateDirectory(
                    @"C:\Containers\handle",
                    Arg.Do<IEnumerable<UserAccess>>(x => userAccess = x)
                );

                directory.CreateSubdirectories(ContainerUser);

                Assert.NotNull(directory);
                Assert.Collection(userAccess,
                    x =>
                    {
                        Assert.Equal(@"BUILTIN\Administrators", x.UserName);
                        Assert.Equal(FileAccess.ReadWrite, x.Access);
                    },
                    x =>
                    {
                        Assert.NotEmpty(x.UserName);
                        Assert.Equal(WindowsIdentity.GetCurrent().Name, x.UserName);
                        Assert.Equal(FileAccess.ReadWrite, x.Access);
                    },
                    x =>
                    {
                        Assert.Equal("username", x.UserName);
                        Assert.Equal(FileAccess.Read, x.Access);
                    });
            }

            [Fact]
            public void CreatesContainerBinDirectoryWithUserReadOnlyPermissions()
            {
                IEnumerable<UserAccess> userAccess = null;
                fileSystem.CreateDirectory(
                    @"C:\Containers\handle\bin",
                    Arg.Do<IEnumerable<UserAccess>>(x => userAccess = x)
                );

                directory.CreateSubdirectories(ContainerUser);

                Assert.NotNull(directory);
                Assert.Collection(userAccess,
                    x =>
                    {
                        Assert.Equal(@"BUILTIN\Administrators", x.UserName);
                        Assert.Equal(FileAccess.ReadWrite, x.Access);
                    },
                    x =>
                    {
                        Assert.NotEmpty(x.UserName);
                        Assert.Equal(WindowsIdentity.GetCurrent().Name, x.UserName);
                        Assert.Equal(FileAccess.ReadWrite, x.Access);
                    },
                    x =>
                    {
                        Assert.Equal("username", x.UserName);
                        Assert.Equal(FileAccess.Read, x.Access);
                    });
            }

            [Fact]
            public void CreatesContainerUserDirectoryWithUserReadWritePermissions()
            {
                IEnumerable<UserAccess> userAccess = null;
                fileSystem.CreateDirectory(
                    @"C:\Containers\handle\user",
                    Arg.Do<IEnumerable<UserAccess>>(x => userAccess = x)
                );

                directory.CreateSubdirectories(ContainerUser);

                Assert.NotNull(directory);
                Assert.Collection(userAccess,
                    x =>
                    {
                        Assert.Equal(@"BUILTIN\Administrators", x.UserName);
                        Assert.Equal(FileAccess.ReadWrite, x.Access);
                    },
                    x => {
                        Assert.NotEmpty(x.UserName);
                        Assert.Equal(WindowsIdentity.GetCurrent().Name, x.UserName);
                        Assert.Equal(FileAccess.ReadWrite, x.Access);
                    },
                    x =>
                    {
                        Assert.Equal("username", x.UserName);
                        Assert.Equal(FileAccess.ReadWrite, x.Access);
                    });
            }
        }

        public class Destroy : ContainerDirectoryTests
        {
            private IContainerUser ContainerUser { get; set; }

            public Destroy()
            {
                ContainerUser = Substitute.For<IContainerUser>();
                ContainerUser.UserName.Returns("username");
            }

            [Fact]
            public void DeletesContainerDirectory()
            {
                directory.Destroy();
                fileSystem.Received(1).DeleteDirectory(@"C:\Containers\handle");
            }
        }

        public class Volume : ContainerDirectoryTests
        {
            [Fact]
            public void FindsRootVolume()
            {
                Assert.Equal(@"C:\", directory.Volume);
            }
        }

        public class MapBinPath : ContainerDirectoryTests
        {
            [InlineData("/", @"C:\Containers\handle\bin\")]
            [InlineData("/path/to/app", @"C:\Containers\handle\bin\path\to\app")]
            [Theory]
            public void MapsRootedPathRelativeToContainerBinPath(string containerPath, string expectedMappedPath)
            {
                var mappedPath = directory.MapBinPath(containerPath);

                Assert.Equal(expectedMappedPath, mappedPath);
            }

            [Fact]
            public void ConvertsForwardSlashesToBackSlashes()
            {
                var mappedPath = directory.MapBinPath("/path/to/app");

                Assert.Equal(@"C:\Containers\handle\bin\path\to\app", mappedPath);
            }

            [Fact]
            public void CanonicalizesPath()
            {
                var mappedPath = directory.MapBinPath("/path/to/../../app");

                Assert.Equal(@"C:\Containers\handle\bin\app", mappedPath);
            }

            [InlineData("/app/../..")]
            [InlineData("/../../..")]
            [InlineData("../")]
            [InlineData("..")]
            [InlineData("/..")]
            [InlineData("/../")]
            [Theory]
            public void WhenPathIsOutsideOfContainerBinPath_Throws(string containerPath)
            {
                var ex = Record.Exception(() => directory.MapBinPath(containerPath));

                Assert.IsAssignableFrom<ArgumentException>(ex);
            }
        }

        public class MapPrivatePath : ContainerDirectoryTests
        {
            [InlineData("/", @"C:\Containers\handle\private\")]
            [InlineData("/path/to/data", @"C:\Containers\handle\private\path\to\data")]
            [Theory]
            public void MapsRootedPathRelativeToContainerRootPath(string containerPath, string expectedMappedPath)
            {
                var mappedPath = directory.MapPrivatePath(containerPath);

                Assert.Equal(expectedMappedPath, mappedPath);
            }

            [Fact]
            public void ConvertsForwardSlashesToBackSlashes()
            {
                var mappedPath = directory.MapPrivatePath("/path/to/data");

                Assert.Equal(@"C:\Containers\handle\private\path\to\data", mappedPath);
            }

            [Fact]
            public void CanonicalizesPath()
            {
                var mappedPath = directory.MapPrivatePath("/path/to/../../data");

                Assert.Equal(@"C:\Containers\handle\private\data", mappedPath);
            }

            [InlineData("/data/../..")]
            [InlineData("/../../..")]
            [InlineData("../")]
            [InlineData("..")]
            [InlineData("/..")]
            [InlineData("/../")]
            [Theory]
            public void WhenPathIsOutsideOfContainerBinPath_Throws(string containerPath)
            {
                var ex = Record.Exception(() => directory.MapPrivatePath(containerPath));

                Assert.IsAssignableFrom<ArgumentException>(ex);
            }
        }

        public class MapUserPath : ContainerDirectoryTests
        {
            [InlineData("", @"C:\Containers\handle\user\")]
            [InlineData(" ", @"C:\Containers\handle\user\")]
            [InlineData("\\", @"C:\Containers\handle\user\")]
            [InlineData("/", @"C:\Containers\handle\user\")]
            [InlineData("/path/to/app", @"C:\Containers\handle\user\path\to\app")]
            [InlineData(@"\path\to\app", @"C:\Containers\handle\user\path\to\app")]
            [Theory]
            public void MapsRootedPathRelativeToContainerUserPath(string containerPath, string expectedMappedPath)
            {
                var mappedPath = directory.MapUserPath(containerPath);

                Assert.Equal(expectedMappedPath, mappedPath);
            }

            [Fact]
            public void ConvertsForwardSlashesToBackSlashes()
            {
                var mappedPath = directory.MapUserPath("/path/to/app");

                Assert.Equal(@"C:\Containers\handle\user\path\to\app", mappedPath);
            }

            [Fact]
            public void CanonicalizesPath()
            {
                var mappedPath = directory.MapUserPath("/path/to/../../app");

                Assert.Equal(@"C:\Containers\handle\user\app", mappedPath);
            }

            [InlineData("/app/../..")]
            [InlineData("/../../..")]
            [InlineData("../")]
            [InlineData("..")]
            [InlineData("/..")]
            [InlineData("/../")]
            [Theory]
            public void WhenPathIsOutsideOfContainerUserPath_Throws(string containerPath)
            {
                var ex = Record.Exception(() => directory.MapUserPath(containerPath));

                Assert.IsAssignableFrom<ArgumentException>(ex);
            }

            [Fact]
            public void WhenPathHasDriveLetterSkipMapping()
            {
                var mappedPath = directory.MapUserPath("C:\\Windows\\System32\\cmd.exe");

                Assert.Equal("C:\\Windows\\System32\\cmd.exe", mappedPath);
            }
        }

        public class CreateBindMountTests : ContainerDirectoryTests
        {
            [Fact]
            public void ItCreatesBindMounts()
            {
                var bindMounts = new []
                {
                    new BindMount()
                    {
                        SourcePath = "source",
                        DestinationPath = "parent1\\destination"
                    },
                    new BindMount()
                    {
                        SourcePath = "source2",
                        DestinationPath = "destination2"
                    }
                };
                var user = Substitute.For<IContainerUser>();
                var userAccess = Substitute.For<UserAccess>();
                userAccess.UserName = user.UserName;

                directory.CreateBindMounts(bindMounts, user);

                fileSystem.Received().CreateDirectory(directory.MapUserPath("parent1"), Arg.Is<ICollection<UserAccess>>(x => x.Any(u => u.UserName == user.UserName)));
                fileSystem.Received().Symlink(directory.MapUserPath(bindMounts[0].DestinationPath), "source");
                fileSystem.Received().Symlink(directory.MapUserPath(bindMounts[1].DestinationPath), "source2");
            }
        }
    }
}
