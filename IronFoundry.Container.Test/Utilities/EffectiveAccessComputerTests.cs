using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Container.Win32;
using Xunit;

namespace IronFoundry.Container.Utilities
{
    public class EffectiveAccessComputerTests
    {
        private IEffectiveAccessComputer EffectiveAccess { get; set; }
        private IdentityReference CurrentIdentity { get; set; }
        private IdentityReference Group { get; set; }

        public EffectiveAccessComputerTests()
        {
            EffectiveAccess = new EffectiveAccessComputer();
            CurrentIdentity = WindowsIdentity.GetCurrent().User;
            Group = WindowsIdentity.GetCurrent().Groups.First();
        }

        public class ComputeAccess : EffectiveAccessComputerTests
        {
            [Fact(Skip = "Fails on domain joined machines")]
            public void WhenUserHasAllStandardRights()
            {
                var rights = new IdentityRights { Identity = CurrentIdentity, Rights = FileSystemRights.FullControl };
                var descriptor = CreateSecurityDescriptor(new[] { rights });
                var access = EffectiveAccess.ComputeAccess(descriptor, CurrentIdentity);

                Assert.True(access.HasFlag(ACCESS_MASK.STANDARD_RIGHTS_ALL));
            }

            [Fact(Skip = "Fails on domain joined machines")]
            public void WhenGroupHasAllStandardRights()
            {
                var rights = new IdentityRights { Identity = Group, Rights = FileSystemRights.FullControl };
                var descriptor = CreateSecurityDescriptor(new[] { rights });
                var access = EffectiveAccess.ComputeAccess(descriptor, CurrentIdentity);

                Assert.True(access.HasFlag(ACCESS_MASK.STANDARD_RIGHTS_ALL));
            }

            [Fact(Skip = "Fails on domain joined machines")]
            public void WhenGroupIsDeniedWrite()
            {
                IdentityRights rights = new IdentityRights {Identity = Group, Rights = FileSystemRights.FullControl};
                var descriptor = CreateSecurityDescriptor(null, denyRights: new[] { rights });
                var access = EffectiveAccess.ComputeAccess(descriptor, CurrentIdentity);

                Assert.False(access.HasFlag(ACCESS_MASK.STANDARD_RIGHTS_ALL));
            }

            private RawSecurityDescriptor CreateSecurityDescriptor(IEnumerable<IdentityRights> allowRights,
                IEnumerable<IdentityRights> denyRights = null)
            {
                var security = new DirectorySecurity();
                security.SetOwner(CurrentIdentity);
                security.SetGroup(Group);

                if (allowRights == null)
                    allowRights = Enumerable.Empty<IdentityRights>();

                if (denyRights == null)
                    denyRights = Enumerable.Empty<IdentityRights>();

                foreach (var right in allowRights)
                {
                    security.AddAccessRule(new FileSystemAccessRule(right.Identity, right.Rights,
                        AccessControlType.Allow));
                }

                foreach (var right in denyRights)
                {
                    security.AddAccessRule(new FileSystemAccessRule(right.Identity, right.Rights, AccessControlType.Deny));
                }

                var binaryDescriptor = security.GetSecurityDescriptorBinaryForm();
                return new RawSecurityDescriptor(binaryDescriptor, 0);
            }

            public class IdentityRights
            {
                public IdentityReference Identity { get; set; }
                public FileSystemRights Rights { get; set; }
            }
        }
    }
}
