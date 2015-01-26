using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Net;
using System.Runtime.InteropServices;
using System.Web.Security;
using IronFoundry.Container.Win32;
using NLog;

namespace IronFoundry.Container.Utilities
{
    // BR: Move this to IronFoundry.Container
    public class LocalPrincipalManager : IUserManager
    {
        const int NERR_Success = 0;
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int NetUserDel(string serverName, string userName);

        private const uint COM_EXCEPT_UNKNOWN_DIRECTORY_OBJECT = 0x80005004;
        // TODO: Determine if adding the user to IIS_USRS is really a requirement for the
        // IISHost.  If it is, then pass an array of groups for the user instead of having this hardcoded.
        //private const string IIS_IUSRS_NAME = "IIS_IUSRS";

        private readonly string directoryPath = String.Format("WinNT://{0}", Environment.MachineName);
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IDesktopPermissionManager permissionManager;
        private readonly string wardenUserGroupName;

        public LocalPrincipalManager(IDesktopPermissionManager permissionManager, string userGroupName)
        {
            this.permissionManager = permissionManager;
            this.wardenUserGroupName = userGroupName;
        }

        public void DeleteUser(string userName)
        {
            // Don't need to cleanup desktop permissions as they are managed by the group.

            // Using NetUserDel as the DirectoryService APIs were painfully slow (~20-30 seconds to delete a single user).  
            var result = NetUserDel(null, userName);

            if (result != NERR_Success)
            {
                log.Error("Failed to delete user with the error code: {0}", result);
            }
        }

        public NetworkCredential CreateUser(string userName)
        {
            var data = InnerCreateUser(userName);
            permissionManager.AddDesktopPermission(wardenUserGroupName);
            return new NetworkCredential(data.UserName, data.Password);
        }

        public string FindUser(string userName)
        {
            string rvUserName = null;

            using (var localDirectory = new DirectoryEntry(directoryPath))
            {
                DirectoryEntries users = localDirectory.Children;
                try
                {
                    using (DirectoryEntry user = users.Find(userName))
                    {
                        if (user != null)
                        {
                            rvUserName = user.Name;
                        }
                    }
                }
                catch (COMException ex)
                {
                    // Exception indicates the requested item could not be found, in which case we should return the default value
                    if ((uint) ex.ErrorCode != COM_EXCEPT_UNKNOWN_DIRECTORY_OBJECT)
                    {
                        throw;
                    }
                }
            }

            return rvUserName;
        }

        private LocalPrincipalData InnerCreateUser(string userName)
        {
            string rvUserName = null;
            string rvPassword = null;
            LocalPrincipalData rv = null;

            using (var context = new PrincipalContext(ContextType.Machine))
            {
                bool userSaved = false;
                ushort tries = 0;
                UserPrincipal user = null;

                do
                {
                    try
                    {
                        rvPassword = Membership.GeneratePassword(8, 2).ToLowerInvariant() + Membership.GeneratePassword(8, 2).ToUpperInvariant();
                        user = new UserPrincipal(context, userName, rvPassword, true);
                        user.DisplayName = "Warden User " + userName;
                        user.Save();
                        userSaved = true;
                    }
                    catch (PasswordException ex)
                    {
                        log.DebugException(ex);
                    }

                    ++tries;
                } while (userSaved == false && tries < 5);

                if (userSaved)
                {
                    rvUserName = user.SamAccountName;
                    //AddUserToGroup(context, IIS_IUSRS_NAME, user);
                    AddUserToGroup(context, wardenUserGroupName, user);
                    rv = new LocalPrincipalData(rvUserName, rvPassword);
                }
            }

            return rv;
        }

        private void AddUserToGroup(PrincipalContext context, string groupName, UserPrincipal user)
        {
            var groupQuery = new GroupPrincipal(context, groupName);
            var searcher = new PrincipalSearcher(groupQuery);
            var group = searcher.FindOne() as GroupPrincipal;

            if (group == null)
            {
                throw new ArgumentException(string.Format("The specified group '{0}' does not exist.", groupName), "groupName");
            }

            // The iisUserGroups.Members.Add attempts to resolve all the SID's of the entries while
            // it's enumerating for an item.  This approach works around this issue by dynamically
            // invoking 'Add' with the DN of the user.
            var groupAsDirectoryEntry = group.GetUnderlyingObject() as DirectoryEntry;
            var userAsDirectoryEntry = user.GetUnderlyingObject() as DirectoryEntry;
            groupAsDirectoryEntry.Invoke("Add", new object[] { userAsDirectoryEntry.Path });

            group.Save();
        }
    }
}