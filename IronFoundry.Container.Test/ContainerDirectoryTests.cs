using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using IronFoundry.Warden.Containers;
using IronFoundry.Container.Utilities;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace IronFoundry.Container
{
    public class ContainerDirectoryTests
    {
        ContainerDirectory Directory { get; set; }

        public ContainerDirectoryTests()
        {
            Directory = new ContainerDirectory(@"C:\Containers\handle");
        }

        public class Create
        {
            FileSystemManager FileSystem { get; set; }
            IContainerUser ContainerUser { get; set; }

            public Create()
            {
                FileSystem = Substitute.For<FileSystemManager>();

                ContainerUser = Substitute.For<IContainerUser>();
                ContainerUser.UserName.Returns("username");
            }

            [Fact]
            public void CreatesContainerDirectoryWithAdminPermissions()
            {
                IEnumerable<UserAccess> userAccess = null;
                FileSystem.CreateDirectory(
                    @"C:\Containers\handle",
                    Arg.Do<IEnumerable<UserAccess>>(x => userAccess = x)
                );

                var directory = ContainerDirectory.Create(FileSystem, @"C:\Containers", "handle", ContainerUser);

                Assert.NotNull(directory);
                Assert.Collection(userAccess,
                    x => {
                        Assert.Equal(@"BUILTIN\Administrators", x.UserName);
                        Assert.Equal(FileAccess.ReadWrite, x.Access);
                    },
                    x => {
                        Assert.NotEmpty(x.UserName);
                        Assert.Equal(WindowsIdentity.GetCurrent().Name, x.UserName);
                        Assert.Equal(FileAccess.ReadWrite, x.Access);
                    });
            }

            [Fact]
            public void CreatesContainerBinDirectoryWithUserReadOnlyPermissions()
            {
                IEnumerable<UserAccess> userAccess = null;
                FileSystem.CreateDirectory(
                    @"C:\Containers\handle\bin",
                    Arg.Do<IEnumerable<UserAccess>>(x => userAccess = x)
                );

                var directory = ContainerDirectory.Create(FileSystem, @"C:\Containers", "handle", ContainerUser);

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
                FileSystem.CreateDirectory(
                    @"C:\Containers\handle\user",
                    Arg.Do<IEnumerable<UserAccess>>(x => userAccess = x)
                );

                var directory = ContainerDirectory.Create(FileSystem, @"C:\Containers", "handle", ContainerUser);

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

        public class MapBinPath : ContainerDirectoryTests
        {
            [InlineData("/", @"C:\Containers\handle\bin\")]
            [InlineData("/path/to/app", @"C:\Containers\handle\bin\path\to\app")]
            [Theory]
            public void MapsRootedPathRelativeToContainerBinPath(string containerPath, string expectedMappedPath)
            {
                var mappedPath = Directory.MapBinPath(containerPath);

                Assert.Equal(expectedMappedPath, mappedPath);
            }

            [Fact]
            public void ConvertsForwardSlashesToBackSlashes()
            {
                var mappedPath = Directory.MapBinPath("/path/to/app");

                Assert.Equal(@"C:\Containers\handle\bin\path\to\app", mappedPath);
            }

            [Fact]
            public void CanonicalizesPath()
            {
                var mappedPath = Directory.MapBinPath("/path/to/../../app");

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
                var ex = Record.Exception(() => Directory.MapBinPath(containerPath));

                Assert.IsAssignableFrom<ArgumentException>(ex);
            }
        }

        public class MapUserPath : ContainerDirectoryTests
        {
            [InlineData("/", @"C:\Containers\handle\user\")]
            [InlineData("/path/to/app", @"C:\Containers\handle\user\path\to\app")]
            [Theory]
            public void MapsRootedPathRelativeToContainerUserPath(string containerPath, string expectedMappedPath)
            {
                var mappedPath = Directory.MapUserPath(containerPath);

                Assert.Equal(expectedMappedPath, mappedPath);
            }

            [Fact]
            public void ConvertsForwardSlashesToBackSlashes()
            {
                var mappedPath = Directory.MapUserPath("/path/to/app");

                Assert.Equal(@"C:\Containers\handle\user\path\to\app", mappedPath);
            }

            [Fact]
            public void CanonicalizesPath()
            {
                var mappedPath = Directory.MapUserPath("/path/to/../../app");

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
                var ex = Record.Exception(() => Directory.MapUserPath(containerPath));

                Assert.IsAssignableFrom<ArgumentException>(ex);
            }
        }
    }
}
