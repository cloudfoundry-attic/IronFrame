using System;
using System.Linq;
using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using IronFoundry.Warden.Utilities;
using Xunit;
using System.Collections;

namespace IronFoundry.Warden.Test
{
    /// <summary>
    /// Test the LocalPrincipal by using the actual local WinNT users and groups entries.
    /// </summary>
    /// <remarks>
    /// These tests must be run with Administrator permission. 
    /// Also, it is expected that the IIS_IUSRS group is present.
    /// </remarks>
    public class LocalPrincipalManagerTests : IDisposable
    {
        private const string testUserName = "IFTestUserName";
        private LocalPrincipalManager manager = new LocalPrincipalManager();

        public LocalPrincipalManagerTests()
        {
            TryRemoveLocalUser(testUserName);
        }

        public void Dispose()
        {
            TryRemoveLocalUser(testUserName);
        }

        [Fact]
        void AddedUserCanBeRelocated()
        {
            manager.CreateUser(testUserName);
            Assert.Equal<string>(testUserName, manager.FindUser(testUserName));
        }

        [Fact]
        void DeletedUserCannotBeLocated()
        {
            manager.CreateUser(testUserName);
            manager.DeleteUser(testUserName);

            Assert.True(string.IsNullOrEmpty(manager.FindUser(testUserName)));
        }

        [Fact]
        void AddedUserAppearsInIISGroup()
        {
            manager.CreateUser(testUserName);
            AssertUserInGroup("IIS_IUSRS", testUserName);
        }

        [Fact]
        void UserAddedMultipleTimesThenThrows()
        {
            manager.CreateUser(testUserName);
            Assert.Throws<System.DirectoryServices.AccountManagement.PrincipalExistsException>(() => { manager.CreateUser(testUserName); });
        }

        [Fact]
        void WhenUserDeletedMultipleTimesDoesNotThrow()
        {
            manager.CreateUser(testUserName);

            manager.DeleteUser(testUserName);
            manager.DeleteUser(testUserName);
        }

        [Fact]
        void CanFindWellKnownUser()
        {
            Assert.Equal("Administrator", manager.FindUser("Administrator"));
        }

        [Fact]
        void FindReturnsNullOnUnlocatableUser()
        {
            Assert.Null(manager.FindUser("ThisUserShouldNeverExist"));
        }

        #region Test Helpers
        private void TryRemoveLocalUser(string userName)
        {
            try
            {
                manager.DeleteUser(userName);
            }
            catch
            {

            }
        }

        private void AssertUserInGroup(string groupName, string userName)
        {
            using (var localDirectory = new DirectoryEntry(String.Format("WinNT://{0}", Environment.MachineName)))
            {
                DirectoryEntries children = localDirectory.Children;
                DirectoryEntry group = children.Find(groupName);

                var groupChildren = group.Invoke("Members", null) as IEnumerable;
                DirectoryEntry user = null;

                foreach(var child in groupChildren)
                {
                    DirectoryEntry de = new DirectoryEntry(child);
                    if (de.Name == userName)
                    {
                        user = de;
                    }
                }

                Assert.NotNull(user);
            }
        }
        #endregion
    }
}
