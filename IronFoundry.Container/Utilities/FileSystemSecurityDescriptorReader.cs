using System;
using System.IO;
using System.Security.AccessControl;

namespace IronFoundry.Container.Utilities
{
    internal interface ISecurityDescriptorReader
    {
        RawSecurityDescriptor GetSecurityDescriptor();
    }

    internal class FileSystemSecurityDescriptorReader : ISecurityDescriptorReader
    {
        private const AccessControlSections AccessSectionsNeeded =
            AccessControlSections.Access | AccessControlSections.Group | AccessControlSections.Owner;

        private readonly string path;

        public FileSystemSecurityDescriptorReader(string path)
        {
            this.path = path;
        }

        public RawSecurityDescriptor GetSecurityDescriptor()
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", "path");
            }

            FileSystemSecurity security;
            if (!TryGetFileSecurity(path, AccessSectionsNeeded, out security))
            {
                if (!TryGetDirectorySecurity(path, AccessSectionsNeeded, out security))
                {
                    throw new ArgumentException("The path must be an existing file or directory.", path);
                }
            }

            var descriptorBinaryForm = security.GetSecurityDescriptorBinaryForm();
            var descriptor = new RawSecurityDescriptor(descriptorBinaryForm, 0);

            return descriptor;
        }

        private static bool TryGetFileSecurity(string path, AccessControlSections sectionsNeeded,
            out FileSystemSecurity security)
        {
            var exists = false;
            security = null;

            if (File.Exists(path))
            {
                exists = true;
                security = File.GetAccessControl(path, sectionsNeeded);
            }

            return exists;
        }

        private bool TryGetDirectorySecurity(string path, AccessControlSections sectionsNeeded,
            out FileSystemSecurity security)
        {
            var exists = false;
            security = null;

            if (Directory.Exists(path))
            {
                exists = true;
                security = Directory.GetAccessControl(path, sectionsNeeded);
            }

            return exists;
        }
    }
}