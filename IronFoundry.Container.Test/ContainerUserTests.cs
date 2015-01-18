using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Utilities;
using Xunit;
using NSubstitute;

namespace IronFoundry.Container
{
    public class ContainerUserTests
    {
        private const string UserName = "username";
        private const string Password = "password";
        IUserManager UserManager { get; set; }
        NetworkCredential Credential { get; set; }

        public ContainerUserTests()
        {
            UserManager = Substitute.For<IUserManager>();
            Credential = new NetworkCredential(UserName, Password);
        }

        public class Delete : ContainerUserTests
        {
            [Fact]
            public void CallsDeleteOnTheUserManager()
            {
                var user = new ContainerUser(UserManager, Credential);

                user.Delete();

                UserManager.Received(1).DeleteUser(UserName);
            }
        }
    }
}
