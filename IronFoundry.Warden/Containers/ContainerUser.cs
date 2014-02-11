using System;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Containers
{
    public class ContainerUser : IEquatable<ContainerUser>
    {
        private const string userPrefix = "warden_";
        private static readonly Regex uniqueIdValidator = new Regex(@"^\w{8,}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private readonly string uniqueId;
        private readonly string userName;
        private readonly string password;

        public NetworkCredential GetCredential()
        {
            return new NetworkCredential(userName, password);
        }

        public ContainerUser(string uniqueId, bool shouldCreate = false)
        {
            if (uniqueId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("uniqueId");
            }
            this.uniqueId = uniqueId;

            if (uniqueIdValidator.IsMatch(uniqueId))
            {
                this.userName = CreateUserName(uniqueId);
            }
            else
            {
                throw new ArgumentException("uniqueId must be 8 or more word characters.");
            }

            var principalManager = new LocalPrincipalManager();
            if (shouldCreate)
            {
                /*
                 * TODO: this means that we can't retrieve a user's password if restoring a container.
                 * This should be OK when we move to the "separate process for container" model since the separate
                 * process will be installed as a service and the password will only need to be known at install
                 * time.
                 */
                var userData = principalManager.CreateUser(this.userName);
                if (userData == null)
                {
                    throw new ArgumentException(String.Format("Could not create user '{0}'", this.userName));
                }
                else
                {
                    this.userName = userData.UserName;
                    this.password = userData.Password;
                }
            }
            else
            {
                string foundUser = principalManager.FindUser(this.userName);
                if (foundUser == null)
                {
                    throw new ArgumentException(String.Format("Could not find user '{0}'", this.userName));
                }
            }

            AddDesktopPermission(this.userName);
        }

        public string SID
        {
            get
            {
                var ntAccount = new NTAccount(userName);
                var securityIdentifier = (SecurityIdentifier)ntAccount.Translate(typeof(SecurityIdentifier));
                return securityIdentifier.ToString();
            }
        }

        public static void CleanUp(string uniqueId)
        {
            try
            {
                string userName = CreateUserName(uniqueId);
                DeleteUser(userName);
                RemoveDesktopPermission(userName);
            }
            catch { }
        }

        public void Delete()
        {
            DeleteUser(userName);
        }

        public static implicit operator string(ContainerUser containerUser)
        {
            return containerUser.userName;
        }

        public static bool operator ==(ContainerUser x, ContainerUser y)
        {
            if (Object.ReferenceEquals(x, null))
            {
                return Object.ReferenceEquals(y, null);
            }
            return x.Equals(y);
        }

        public static bool operator !=(ContainerUser x, ContainerUser y)
        {
            return !(x == y);
        }

        public override string ToString()
        {
            return userName;
        }

        public override int GetHashCode()
        {
            return userName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ContainerUser);
        }

        public bool Equals(ContainerUser other)
        {
            if (Object.ReferenceEquals(null, other))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.GetHashCode() == other.GetHashCode();
        }

        private static void AddDesktopPermission(string userName)
        {
            if (Environment.UserInteractive == false)
            {
                var desktopPermissionManager = new DesktopPermissionManager(userName);
                desktopPermissionManager.AddDesktopPermission();
            }
        }

        private static void DeleteUser(string userName)
        {
            var principalManager = new LocalPrincipalManager();
            principalManager.DeleteUser(userName);
        }

        private static void RemoveDesktopPermission(string userName)
        {
            if (Environment.UserInteractive == false)
            {
                var desktopPermissionManager = new DesktopPermissionManager(userName);
                desktopPermissionManager.RemoveDesktopPermission();
            }
        }

        private static string CreateUserName(string uniqueId)
        {
            return String.Concat(userPrefix, uniqueId);
        }
    }
}
