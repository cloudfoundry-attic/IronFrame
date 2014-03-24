namespace IronFoundry.Warden.Utilities
{
    using System;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Web.Security;
    using NLog;
    using System.Runtime.InteropServices;

    public class LocalPrincipalManager : IronFoundry.Warden.Utilities.IUserManager
    {
        private const uint COM_EXCEPT_UNKNOWN_DIRECTORY_OBJECT = 0x80005004;

        private readonly Logger log = LogManager.GetCurrentClassLogger();

        // IIS_IUSRS_SID = "S-1-5-32-568";
        private const string IIS_IUSRS_NAME = "IIS_IUSRS";

        private readonly string directoryPath = String.Format("WinNT://{0}", Environment.MachineName);
        private IDesktopPermissionManager permissionManager;

        public LocalPrincipalManager(IDesktopPermissionManager permissionManager)
        {
            this.permissionManager = permissionManager;
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
                    if ((uint)ex.ErrorCode != COM_EXCEPT_UNKNOWN_DIRECTORY_OBJECT) { throw; }
                }
            }

            return rvUserName;
        }

        public LocalPrincipalData CreateUser(string userName)
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
                }
                while (userSaved == false && tries < 5);

                if (userSaved)
                {
                    rvUserName = user.SamAccountName;
                    var groupQuery = new GroupPrincipal(context, IIS_IUSRS_NAME);
                    var searcher = new PrincipalSearcher(groupQuery);
                    var iisUsersGroup = searcher.FindOne() as GroupPrincipal;

                    // The iisUserGroups.Members.Add attempts to resolve all the SID's of the entries while
                    // it's enumerating for an item.  This approach works around this issue by dynamically
                    // invoking 'Add' with the DN of the user.
                    var groupAsDirectoryEntry = iisUsersGroup.GetUnderlyingObject() as DirectoryEntry;
                    var userAsDirectoryEntry = user.GetUnderlyingObject() as DirectoryEntry;
                    groupAsDirectoryEntry.Invoke("Add", new object[] { userAsDirectoryEntry.Path });

                    iisUsersGroup.Save();

                    rv = new LocalPrincipalData(rvUserName, rvPassword);
                }
            }

            return rv;
        }

        public void DeleteUser(string userName)
        {
            using (var localDirectory = new DirectoryEntry(directoryPath))
            {
                DirectoryEntries users = localDirectory.Children;
                // users.Find(userName) throws, rather than be exception based we enumerate this outselves                
                var user = AnyEntry(users, de => de.Name == userName);
                if (user != null)
                {
                    users.Remove(user);
                }
            }
        }

        private DirectoryEntry AnyEntry(DirectoryEntries entries, Func<DirectoryEntry, bool> eval)
        {
            foreach (var entry in entries)
            {
                DirectoryEntry de = entry as DirectoryEntry;

                if (de != null && eval(de))
                {
                    return de;
                }
            }

            return null;
        }

        System.Net.NetworkCredential IUserManager.CreateUser(string userName)
        {
            var data = this.CreateUser(userName);
            this.permissionManager.AddDesktopPermission(userName);
            return new System.Net.NetworkCredential(data.UserName, data.Password);
        }
    }
}
