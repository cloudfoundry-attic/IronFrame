using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using IronFrame.Win32;

namespace IronFrame
{
    // BR: Investigate if we would need this in the host??  Can't delete a user in the host...
    public interface IContainerUser
    {
        string UserName { get; }
        string SID { get; }
        NetworkCredential GetCredential();
        void Delete();
        void DeleteProfile();
    }

    internal class ContainerUser : IContainerUser
    {
        readonly IUserManager userManager; // TODO: Refactor this out of this class
        readonly NetworkCredential credentials;
        private string _sid = null;

        public ContainerUser(IUserManager userManager, NetworkCredential credentials)
        {
            this.userManager = userManager;
            this.credentials = credentials;
        }

        public string UserName
        {
            get { return credentials.UserName; }
        }

        public string SID
        {
            get
            {
                if (_sid == null)
                {
                    _sid = userManager.GetSID(credentials.UserName);
                }
                return _sid;
            }
        }

        public NetworkCredential GetCredential()
        {
            return credentials;
        }

        static string BuildContainerUserName(string id)
        {
            return "c_" + id;
        }

        public static ContainerUser Create(IUserManager userManager, string containerId)
        {
            var credentials = userManager.CreateUser(BuildContainerUserName(containerId));
            return new ContainerUser(userManager, credentials);
        }

        public void Delete()
        {
            userManager.DeleteUser(UserName);
        }

        public static ContainerUser Restore(IUserManager userManager, string containerId)
        {
            var credentials = new NetworkCredential(BuildContainerUserName(containerId), "");
            return new ContainerUser(userManager, credentials);
        }

       public void CreateProfile()
        {
            userManager.CreateProfile(UserName);
        }

        public void DeleteProfile()
        {
            if (SID != null)
                userManager.DeleteProfile(SID);
        }
    }
}
