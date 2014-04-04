using System;
using System.Net;
using System.Security;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class ContainerUserTests
    {
        public class WhenCreating
        {
            [Fact]
            public void DelegatsToUserManager()
            {
                var userManager = Substitute.For<IUserManager>();
                userManager.CreateUser(Arg.Any<string>()).ReturnsForAnyArgs(new NetworkCredential("warden_asdfghjk", "foo"));

                ContainerUser.CreateUser("asdfghjk", userManager);

                userManager.ReceivedWithAnyArgs().CreateUser(Arg.Any<string>());
            }

            [Fact]
            public void ReturnsValidUser()
            {
                var userManager = Substitute.For<IUserManager>();
                userManager.CreateUser(Arg.Any<string>()).ReturnsForAnyArgs(new NetworkCredential("warden_asdfghjk", "foo"));

                ContainerUser user = ContainerUser.CreateUser("asdfghjk", userManager);

                Assert.Equal("warden_asdfghjk", user.UserName);
            }

            [Fact]
            public void ReturnsValidNetworkCredential()
            {
                var userManager = Substitute.For<IUserManager>();
                userManager.CreateUser(Arg.Any<string>()).ReturnsForAnyArgs(new NetworkCredential("warden_asdfghjk", "foo"));
                ContainerUser user = ContainerUser.CreateUser("asdfghjk", userManager);

                NetworkCredential credential = user.GetCredential();

                Assert.Equal("warden_asdfghjk", credential.UserName);
                Assert.Equal("foo", credential.Password);
            }

            [Fact]
            public void ThrowsArgumentExceptionIfIdIsLessEightWordCharacters()
            {
                var userManager = Substitute.For<IUserManager>();

                var exception = Record.Exception(()=> ContainerUser.CreateUser("asdfg", userManager));

                Assert.IsType<ArgumentException>(exception);
                
            }

            [Fact]
            public void ThrowsArgumentExceptionIfIdIsNotWordCharacters()
            {
                var userManager = Substitute.For<IUserManager>();

                var exception = Record.Exception(() => ContainerUser.CreateUser("aflk22&&", userManager));

                Assert.IsType<ArgumentException>(exception);
            }

            [Fact]
            public void ThrowsArgumentNullExceptionWhenIdIsNull()
            {
                var userManager = Substitute.For<IUserManager>();

                var exception = Record.Exception(() => ContainerUser.CreateUser(null, userManager));

                Assert.IsType<ArgumentNullException>(exception);
            }
        }

        public class WhenConverting
        {
            [Fact]
            public void ConvertsContanerUserToUserNameString()
            {
                var userManager = Substitute.For<IUserManager>();
                userManager.CreateUser(Arg.Any<string>()).ReturnsForAnyArgs(new NetworkCredential("warden_asdfghjk", "foo"));
                ContainerUser user = ContainerUser.CreateUser("asdfghjk", userManager);

                string name = user;

                Assert.Equal("warden_asdfghjk", name);
            }
        }

        public class WhenComparing
        {
            [Fact]
            public void TwoContainerUserObjectsWithTheSameNameShouldBeEqual()
            {
                var userManager = Substitute.For<IUserManager>();
                userManager.CreateUser(Arg.Any<string>()).ReturnsForAnyArgs(new NetworkCredential("warden_asdfghjk", "foo"));
                ContainerUser user = ContainerUser.CreateUser("asdfghjk", userManager);
                userManager.CreateUser(Arg.Any<string>()).ReturnsForAnyArgs(new NetworkCredential("warden_asdfghjk", "foo"));
                ContainerUser user2 = ContainerUser.CreateUser("asdfghjk", userManager);

                Assert.Equal(user, user2);
                Assert.True(user == user2);
            }

            [Fact]
            public void TwoContainerUserObjectsWithDifferentNamesShouldNotBeEqual()
            {
                var userManager = Substitute.For<IUserManager>();
                userManager.CreateUser(Arg.Any<string>()).ReturnsForAnyArgs(new NetworkCredential("warden_asdfghjk", "foo"));
                ContainerUser user = ContainerUser.CreateUser("asdfghjk", userManager);
                userManager.CreateUser(Arg.Any<string>()).ReturnsForAnyArgs(new NetworkCredential("warden_asdfghjw", "foo"));
                ContainerUser user2 = ContainerUser.CreateUser("asdfghjw", userManager);

                Assert.True(user != user2);
            }
        }
    }
}