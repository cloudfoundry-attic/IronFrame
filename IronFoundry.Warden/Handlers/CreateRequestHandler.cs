namespace IronFoundry.Warden.Handlers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Security.AccessControl;
    using System.Threading.Tasks;
    using Containers;
    using NLog;
    using Protocol;
    using IronFoundry.Warden.Configuration;

    public class CreateRequestHandler : ContainerRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly CreateRequest request;

        public CreateRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
            this.request = (CreateRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            return Task.Run<Response>(() =>
                {
                    var resources = ContainerResourceHolder.Create(new WardenConfig());

                    var container = new ContainerProxy(new ContainerHostLauncher());
                    container.Initialize(resources);
                    
                    containerManager.AddContainer(container);

                    ProcessBindMounts(request.BindMounts, container.ContainerUserName);

                    return new CreateResponse { Handle = container.Handle };
                });
        }

        /// <summary>
        /// Give read access to bind mount directories.
        /// TODO: move to centralized permission manager.
        /// </summary>
        /// <param name="bindMounts"></param>
        /// <param name="containerUser"></param>
        private void ProcessBindMounts(IEnumerable<CreateRequest.BindMount> bindMounts, string containerUser)
        {
            var inheritanceFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

            if (!bindMounts.IsNullOrEmpty())
            {
                foreach (var bindMount in bindMounts)
                {
                    FileSystemRights rights = FileSystemRights.Read;
                    switch (bindMount.BindMountMode)
                    {
                        case CreateRequest.BindMount.Mode.RO:
                            // TODO: these rights aren't quite enough - rights = FileSystemRights.Read;
                            rights = FileSystemRights.FullControl;
                            break;
                        case CreateRequest.BindMount.Mode.RW:
                            // TODO: these rights aren't quite enough - rights = FileSystemRights.Read | FileSystemRights.Write;
                            rights = FileSystemRights.FullControl;
                            break;
                    }
                    var accessRule = new FileSystemAccessRule(containerUser, rights, inheritanceFlags, PropagationFlags.InheritOnly, AccessControlType.Allow);
                    log.Trace("Adding access rule to SrcPath '{0}', DstPath '{1}'", bindMount.SrcPath, bindMount.DstPath);
                    AddAccessRuleTo(accessRule, bindMount.SrcPath);
                    AddAccessRuleTo(accessRule, bindMount.DstPath);
                }
            }
        }

        private void AddAccessRuleTo(FileSystemAccessRule accessRule, string path)
        {
            var pathInfo = new DirectoryInfo(path); 
            if (pathInfo.Exists)
            {
                log.Trace("Adding access rule to path '{0}'", pathInfo.FullName);
                DirectorySecurity pathSecurity = pathInfo.GetAccessControl();
                pathSecurity.AddAccessRule(accessRule);

                ReplaceAllChildPermissions(pathInfo, pathSecurity);
            }
            else
            {
                DirectoryInfo parentInfo = pathInfo.Parent;
                if (parentInfo.Exists)
                {
                    log.Trace("Adding access rule to path '{0}' via parent '{1}'", pathInfo.FullName, parentInfo.FullName);
                    DirectorySecurity pathSecurity = parentInfo.GetAccessControl();
                    pathSecurity.AddAccessRule(accessRule);

                    Directory.CreateDirectory(pathInfo.FullName, pathSecurity);

                    ReplaceAllChildPermissions(pathInfo, pathSecurity);
                }
            }
        }

        private static void ReplaceAllChildPermissions(DirectoryInfo dirInfo, DirectorySecurity security)
        {
            dirInfo.SetAccessControl(security);

            foreach (var fi in dirInfo.GetFiles())
            {
                var fileSecurity = fi.GetAccessControl();
                fileSecurity.SetAccessRuleProtection(false, false);
                fi.SetAccessControl(fileSecurity);
            }

            foreach (var di in dirInfo.GetDirectories())
            {
                ReplaceAllChildPermissions(di, security);
            }
        }
    }
}
