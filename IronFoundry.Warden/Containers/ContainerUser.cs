using System;
using System.Net;
using System.Security;
using System.Security.Principal;
using System.Text.RegularExpressions;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Containers
{
    public class ContainerUser : IEquatable<ContainerUser>, IContainerUser
    {
        private const string UserPrefix = "warden_";
        private static readonly Regex uniqueIdValidator = new Regex(@"^\w{8,}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private readonly IUserManager userManager;
        private readonly string userName;
        private string password;

        private ContainerUser(string uniqueId, IUserManager userManager)
        {
            if (uniqueId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("uniqueId");
            }

            this.userManager = userManager;

            if (uniqueIdValidator.IsMatch(uniqueId))
            {
                userName = CreateUserName(uniqueId);
            }
            else
            {
                throw new ArgumentException("uniqueId must be 8 or more word characters.");
            }
        }

        public ContainerUser(string userName, SecureString password)
        {
            this.userName = userName;
            this.password = password.ToUnsecureString();
            userManager = new LocalPrincipalManager(new DesktopPermissionManager());
        }

        public string SID
        {
            get
            {
                var ntAccount = new NTAccount(userName);
                var securityIdentifier = (SecurityIdentifier) ntAccount.Translate(typeof (SecurityIdentifier));
                return securityIdentifier.ToString();
            }
        }

        public NetworkCredential GetCredential()
        {
            return new NetworkCredential(userName, password);
        }

        public string UserName
        {
            get { return userName; }
        }

        public void Delete()
        {
            userManager.DeleteUser(userName);
        }

        public bool Equals(ContainerUser other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return GetHashCode() == other.GetHashCode();
        }

        public static ContainerUser CreateUser(string uniqueId, IUserManager userManager)
        {
            var user = new ContainerUser(uniqueId, userManager);
            var userData = userManager.CreateUser(user.UserName);
            if (userData == null)
            {
                throw new ArgumentException(String.Format("Could not create user '{0}'", user.UserName));
            }
            user.password = userData.Password;
            return user;
        }

        public static implicit operator string(ContainerUser containerUser)
        {
            return containerUser.userName;
        }

        public static bool operator ==(ContainerUser x, ContainerUser y)
        {
            if (ReferenceEquals(x, null))
            {
                return ReferenceEquals(y, null);
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

        public static string CreateUserName(string uniqueId)
        {
            return String.Concat(UserPrefix, uniqueId);
        }
    }
}