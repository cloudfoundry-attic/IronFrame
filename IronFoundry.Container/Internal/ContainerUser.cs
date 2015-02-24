using System.Net;
using IronFoundry.Container.Utilities;

namespace IronFoundry.Container.Internal
{
    // BR: Investigate if we would need this in the host??  Can't delete a user in the host...
    internal interface IContainerUser
    {
        string UserName { get; }
        NetworkCredential GetCredential();
        void Delete();
    }

    internal sealed class ContainerUser : IContainerUser
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
            return "c_" + handle;
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
