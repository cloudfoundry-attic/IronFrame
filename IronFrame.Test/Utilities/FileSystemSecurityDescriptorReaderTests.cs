using System;
using System.IO;
using System.Security.AccessControl;
using Xunit;

namespace IronFrame.Utilities
{
    public class FileSystemSecurityDescriptorReaderTests : IDisposable
    {
        public FileSystemSecurityDescriptorReaderTests()
        {
            TempFilePath = Path.GetTempFileName();
            TempDirPath = CreateTempDirectory();
        }

        private string TempFilePath { get; set; }
        private string TempDirPath { get; set; }

        public void Dispose()
        {
            File.Delete(TempFilePath);
            Directory.Delete(TempDirPath, true);
        }

        private string CreateTempDirectory()
        {
            var tempPath = Path.GetTempFileName();
            File.Delete(tempPath);
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        public class GetSecurityDescriptor : FileSystemSecurityDescriptorReaderTests
        {
            internal ISecurityDescriptorReader Reader { get; set; }

            [Fact]
            public void WhenPathDoesNotExist_Throws()
            {
                Reader = new FileSystemSecurityDescriptorReader(@"c:\IMAGINARY\NONEXISTENT\PATH");
                Action compute = () => Reader.GetSecurityDescriptor();
                Assert.Throws<ArgumentException>(compute);
            }

            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            [Theory]
            public void WhenPathNullOrEmpty_Throws(string path)
            {
                Reader = new FileSystemSecurityDescriptorReader(path);
                Action compute = () => Reader.GetSecurityDescriptor();
                Assert.Throws<ArgumentException>(compute);
            }

            [Fact]
            public void WithDirectoryPath()
            {
                var descriptor = GetPathDescriptor(TempDirPath);
                Assert.NotNull(descriptor);
            }

            [Fact]
            public void WithFilePath()
            {
                var descriptor = GetPathDescriptor(TempFilePath);
                Assert.NotNull(descriptor);
            }

            private RawSecurityDescriptor GetPathDescriptor(string path)
            {
                Reader = new FileSystemSecurityDescriptorReader(path);
                return Reader.GetSecurityDescriptor();
            }
        }
    }
}