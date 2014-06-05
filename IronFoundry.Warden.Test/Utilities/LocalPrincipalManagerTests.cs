using System;
using System.Linq;
using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using IronFoundry.Warden.Utilities;
using Xunit;
using System.Collections;
using NSubstitute;

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
        private const string testGroupName = "TestWardenUserGroup";

        private LocalPrincipalManager manager;
        private IDesktopPermissionManager permissionManager;

        public LocalPrincipalManagerTests()
        {
            permissionManager = Substitute.For<IDesktopPermissionManager>();
            RecreateLocalGroup(testGroupName);
            manager = new LocalPrincipalManager(permissionManager, testGroupName);

            TryRemoveLocalUser(testUserName);
        }

        public void Dispose()
        {            
            TryRemoveLocalUser(testUserName);
            TryDeleteLocalGroup(testGroupName);
        }

        [FactAdminRequired]
        void AddedUserCanBeRelocated()
        {
            manager.CreateUser(testUserName);
            Assert.Equal<string>(testUserName, manager.FindUser(testUserName));
        }

        [FactAdminRequired]
        void DeletedUserCannotBeLocated()
        {
            manager.CreateUser(testUserName);
            manager.DeleteUser(testUserName);

            Assert.True(string.IsNullOrEmpty(manager.FindUser(testUserName)));
        }

        [FactAdminRequired]
        void AddedUserAppearsInIISGroup()
        {
            manager.CreateUser(testUserName);
            AssertUserInGroup("IIS_IUSRS", testUserName);
        }

        [FactAdminRequired]
        void AddedUserAppearsInWardenGroup()
        {
            manager.CreateUser(testUserName);
            AssertUserInGroup(testGroupName, testUserName);
        }

        [FactAdminRequired]
        void UserAddedMultipleTimesThenThrows()
        {
            manager.CreateUser(testUserName);
            Assert.Throws<System.DirectoryServices.AccountManagement.PrincipalExistsException>(() => { manager.CreateUser(testUserName); });
        }

        [FactAdminRequired]
        void WhenUserDeletedMultipleTimesDoesNotThrow()
        {
            manager.CreateUser(testUserName);

            manager.DeleteUser(testUserName);
            manager.DeleteUser(testUserName);
        }

        [FactAdminRequired]
        void CanFindWellKnownUser()
        {
            Assert.Equal("Administrator", manager.FindUser("Administrator"));
        }

        [FactAdminRequired]
        void FindReturnsNullOnUnlocatableUser()
        {
            Assert.Null(manager.FindUser("ThisUserShouldNeverExist"));
        }

        [FactAdminRequired]
        void WhenUserCreatedAddsDesktopPermissions()
        {
            manager.CreateUser(testUserName);

            permissionManager.Received().AddDesktopPermission(testGroupName);
        }

        [FactAdminRequired]
        public void WhenUserDeletedRemovesDesktopPermissions()
        {
            manager.CreateUser(testUserName);
            manager.DeleteUser(testUserName);

            permissionManager.DidNotReceive().RemoveDesktopPermission(testGroupName);
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

        private void RecreateLocalGroup(string groupName)
        {
            TryDeleteLocalGroup(groupName);
            CreateLocalGroup(groupName);
        }

        private void CreateLocalGroup(string groupName)
        {
            using (var localDirectory = new DirectoryEntry(String.Format("WinNT://{0}", Environment.MachineName)))
            {
                DirectoryEntries children = localDirectory.Children;

                try
                {
                    DirectoryEntry group = children.Find(groupName);
                    if (group != null) return;
                }
                catch (COMException)
                {
                    // Couldn't find group.
                }

                var newGroup = children.Add(groupName, "group");
                newGroup.CommitChanges();
            }
        }

        private void TryDeleteLocalGroup(string groupName)
        {
            using (var localDirectory = new DirectoryEntry(String.Format("WinNT://{0}", Environment.MachineName)))
            {
                DirectoryEntries children = localDirectory.Children;

                try
                {
                    DirectoryEntry group = children.Find(groupName);
                    children.Remove(group);
                }
                catch
                {
                }
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
