using System.Net;
using IronFrame.Utilities;
using Xunit;
using NSubstitute;

namespace IronFrame
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
