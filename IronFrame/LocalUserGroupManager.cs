﻿using System;
using System.DirectoryServices;
using System.Runtime.InteropServices;

namespace IronFrame
{
    // Kept public because it is used by the acceptance tests to create/delete user groups.

    // TODO: Either change LocalPrincipalManager to use the LocalUserGroupManager for its group operations
    // or have the container use both to create the user and then add it to the appropriate groups
    public sealed class LocalUserGroupManager
    {
        public void CreateLocalGroup(string groupName)
        {
            using (var localDirectory = new DirectoryEntry("WinNT://.,Computer"))
            {
                DirectoryEntries children = localDirectory.Children;

                try
                {
                    using (DirectoryEntry group = children.Find(groupName, "group"))
                    {
                        if (group != null)
                            return;
                    }
                }
                catch (COMException)
                {
                    // Couldn't find group.
                }

                var newGroup = children.Add(groupName, "group");
                newGroup.CommitChanges();
            }
        }

        public void DeleteLocalGroup(string groupName)
        {
            using (var localDirectory = new DirectoryEntry("WinNT://.,Computer"))
            {
                DirectoryEntries children = localDirectory.Children;

                using (DirectoryEntry group = children.Find(groupName, "group"))
                {
                    children.Remove(group);
                }
            }
        }
    }
}
