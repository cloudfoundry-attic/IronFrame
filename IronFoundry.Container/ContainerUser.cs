using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Container
{
    public class ContainerUser : IContainerUser
    {
        readonly IUserManager userManager; // TODO: Refactor this out of this class
        readonly NetworkCredential credentials;

        public ContainerUser(IUserManager userManager, NetworkCredential credentials)
        {
            this.userManager = userManager;
            this.credentials = credentials;
        }

        public string UserName
        {
            get { return credentials.UserName; }
        }

        public NetworkCredential GetCredential()
        {
            return credentials;
        }

        static string BuildContainerUserName(string handle)
        {
            return "container_" + handle;
        }

        public static ContainerUser Create(IUserManager userManager, string containerHandle)
        {
            var credentials = userManager.CreateUser(BuildContainerUserName(containerHandle));
            return new ContainerUser(userManager, credentials);
        }

        public void Delete()
        {
            userManager.DeleteUser(UserName);
        }
    }
}
