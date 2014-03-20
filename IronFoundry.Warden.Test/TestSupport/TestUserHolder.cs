using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;

namespace IronFoundry.Warden.Test.TestSupport
{
    public class TestUserHolder : IDisposable
    {
        private TestUserHolder(string userName, string password, UserPrincipal principal)
        {
            this.UserName = userName;
            this.Password = password;
            this.Principal = principal;
        }

        public void Dispose()
        {
            Principal.Delete();
        }

        public string UserName { get; private set; }
        public string Password { get; private set; }
        public UserPrincipal Principal { get; private set; }

        private static UserPrincipal RecreateTestUser(string testUserName, string password)
        {
            var context = new PrincipalContext(ContextType.Machine);
            var testUser = UserPrincipal.FindByIdentity(context, testUserName);

            if (testUser != null)
            {
                testUser.Delete();
            }

            testUser = new UserPrincipal(context, testUserName, password, true);
            testUser.Save();

            return testUser;
        }

        private static string GeneratePassword()
        {
            return Membership.GeneratePassword(8, 2).ToLowerInvariant() + Membership.GeneratePassword(8, 2).ToUpperInvariant();
        }

        public static TestUserHolder CreateUser(string user, string password = null)
        {
            if (password == null)
            {
                password = GeneratePassword();
            }
            var principal = RecreateTestUser(user, password);
            return new TestUserHolder(user, password, principal);
        }
    }
}
