using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace IronFoundry.Container.Utilities
{
    public class FileSystemEffectiveAccessComputerTests : IDisposable
    {
        TempFile TestFile { get; set; }
        FileSystemEffectiveAccessComputer EffectiveAccess { get; set; }
        IdentityReference CurrentIdentity { get; set; }

        public FileSystemEffectiveAccessComputerTests()
        {
            TestFile = new TempFile(Path.GetTempPath());
            EffectiveAccess = new FileSystemEffectiveAccessComputer();
            CurrentIdentity = WindowsIdentity.GetCurrent().User;
        }

        public void Dispose()
        {
            TestFile.Dispose();
        }

        public class ComputeAccess : FileSystemEffectiveAccessComputerTests
        {
            [Fact(Skip = "Fails on domain joined machines")]
            public void FullControlOfOwnedFile()
            {
                var rights = EffectiveAccess.ComputeAccess(TestFile.FullName, CurrentIdentity);

                Assert.True(rights.HasFlag(FileSystemRights.FullControl));
            }

            [InlineData(FileSystemRights.Read)]
            [InlineData(FileSystemRights.Write)]
            [InlineData(FileSystemRights.Read | FileSystemRights.Write)]
            [InlineData(FileSystemRights.Delete)]
            [Theory(Skip = "Fails on domain joined machines")]
            public void DeniedRight(FileSystemRights deniedRight)
            {
                AddFileDenyACE(TestFile.FullName, CurrentIdentity, deniedRight);

                var rights = EffectiveAccess.ComputeAccess(TestFile.FullName, CurrentIdentity);

                Assert.False(rights.HasFlag(deniedRight));
            }
        }

        public void AddFileDenyACE(string filePath, IdentityReference identity, FileSystemRights right)
        {
            const AccessControlSections accessSections = AccessControlSections.Owner | AccessControlSections.Group |
                                                         AccessControlSections.Access;

            var security = File.GetAccessControl(filePath, accessSections);
            var rule = new FileSystemAccessRule(identity, right, AccessControlType.Deny);
            security.AddAccessRule(rule);
            File.SetAccessControl(filePath, security);
        }
    }
}