using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Security;
using IronFrame.Utilities;
using IronFrame.Win32;
using NLog;
using System.Security.Principal;

namespace IronFrame
{
    internal interface IUserManager
    {
        NetworkCredential CreateUser(string userName);
        void DeleteUser(string userName);
        string GetSID(string userName);
    }

    // Public because it is used by the acceptance tests to create/delete users.
    public sealed class LocalPrincipalManager : IUserManager
    {
        const int NERR_Success = 0;
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int NetUserDel(string serverName, string userName);

        private const uint COM_EXCEPT_UNKNOWN_DIRECTORY_OBJECT = 0x80005004;
        private const uint COM_EXCEPT_ERROR_NONE_MAPPED = 0x80070534;

        // TODO: Determine if adding the user to IIS_USRS is really a requirement for the
        // IISHost.  If it is, then pass an array of groups for the user instead of having this hardcoded.
        //private const string IIS_IUSRS_NAME = "IIS_IUSRS";

        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IDesktopPermissionManager permissionManager;
        private readonly IEnumerable<string> wardenUserGroups;

        internal LocalPrincipalManager(IDesktopPermissionManager permissionManager, params string[] userGroupNames)
        {
            this.permissionManager = permissionManager;
            this.wardenUserGroups = userGroupNames ?? new String[0];
        }

        public LocalPrincipalManager(params string[] userGroupNames)
            : this(new DesktopPermissionManager(), userGroupNames)
        {
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

            foreach (string userGroupName in wardenUserGroups)
            {
                permissionManager.AddDesktopPermission(userGroupName);
            }

            return new NetworkCredential(data.UserName, data.Password);
        }

        public string GetSID(string userName)
        {
            try
            {
                var account = new NTAccount(null, userName);
                var securityIdentifier = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
                return securityIdentifier.ToString();
            }
            catch (IdentityNotMappedException)
            {
            }
            return null;
        }

        public string FindUser(string userName)
        {
            string rvUserName = null;

            using (var localDirectory = new DirectoryEntry("WinNT://.,Computer"))
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
                    if ((uint)ex.ErrorCode != COM_EXCEPT_UNKNOWN_DIRECTORY_OBJECT)
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

                try
                {
                    do
                    {
                        try
                        {
                            if (user != null)
                            {
                                user.Dispose();
                            }

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

                        foreach (string userGroupName in this.wardenUserGroups)
                        {
                            AddUserToGroup(rvUserName, userGroupName);
                        }

                        rv = new LocalPrincipalData(rvUserName, rvPassword);
                    }
                }
                finally
                {
                    if (user != null)
                        user.Dispose();
                }
            }

            return rv;
        }

        private void AddUserToGroup(string userName, string groupName)
        {
            using (DirectoryEntry groupEntry = new DirectoryEntry("WinNT://./" + groupName + ",Group"))
            {
                groupEntry.Invoke("Add", new object[] { "WinNT://" + userName + ",User" });
                groupEntry.CommitChanges();
            }
        }
    }
}