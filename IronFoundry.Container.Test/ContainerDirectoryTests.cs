using System;
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
