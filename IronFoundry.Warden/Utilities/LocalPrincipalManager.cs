namespace IronFoundry.Warden.Utilities
{
    using System;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Web.Security;
    using NLog;

    public class LocalPrincipalManager
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        // IIS_IUSRS_SID = "S-1-5-32-568";
        private const string IIS_IUSRS_NAME = "IIS_IUSRS";

        private readonly string directoryPath = String.Format("WinNT://{0}", Environment.MachineName);

        public string FindUser(string userName)
        {
            string rvUserName = null;

            using (var localDirectory = new DirectoryEntry(directoryPath))
            {
                DirectoryEntries users = localDirectory.Children;
                using (DirectoryEntry user = users.Find(userName))
                {
                    if (user != null)
                    {
                        rvUserName = user.Name;
                    }
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
                    iisUsersGroup.Members.Add(user);
                    iisUsersGroup.Save();

                    rv =  new LocalPrincipalData(rvUserName, rvPassword);
                }
            }

            return rv;
        }

        public void DeleteUser(string userName)
        {
            using (var localDirectory = new DirectoryEntry(directoryPath))
            {
                DirectoryEntries users = localDirectory.Children;
                using (DirectoryEntry user = users.Find(userName))
                {
                    users.Remove(user);
                }
            }
        }
    }
}
