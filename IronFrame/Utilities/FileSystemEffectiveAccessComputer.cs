using System.Security.AccessControl;
using System.Security.Principal;

namespace IronFrame.Utilities
{
    internal class FileSystemEffectiveAccessComputer
    {
        public FileSystemRights ComputeAccess(string path, IdentityReference identity)
        {
            var reader = new FileSystemSecurityDescriptorReader(path);
            var descriptor = reader.GetSecurityDescriptor();
            var effectiveAccess = new EffectiveAccessComputer();
            var access = effectiveAccess.ComputeAccess(descriptor, identity);
            var rights = (FileSystemRights) access;

            return rights;
        }
    }
}