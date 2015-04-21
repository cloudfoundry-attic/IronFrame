using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace IronFrame.Utilities
{
    public class EnvironmentBlockTests
    {
        [Fact]
        public void CanBeBuiltFromDictionary()
        {
            var env = EnvironmentBlock.Create(new Dictionary<string, string> {{"FOO", "BAR"}});
            var dict = env.ToDictionary();
            Assert.Equal(1, dict.Count);
            Assert.Equal("BAR", dict["FOO"]);
        }

        [Fact]
        public void GeneratesDefaultEnvironment()
        {
            var env = EnvironmentBlock.CreateSystemDefault();
            var dict = env.ToDictionary();

            Assert.True(dict.Count > 0);
            // Verify some of the environment variables we expect to be there by default
            Assert.Contains("TEMP", dict.Keys);
            Assert.Contains("TMP", dict.Keys);
            Assert.Contains("SystemRoot", dict.Keys);
            Assert.Contains("COMPUTERNAME", dict.Keys);
        }

        [Fact]
        public void MergeOverwritesOld()
        {
            var env = EnvironmentBlock.CreateSystemDefault();
            var newEnv = EnvironmentBlock.Create(new Hashtable {{"COMPUTERNAME", "FOOBAR"}});
            var dict = env.Merge(newEnv).ToDictionary();

            Assert.Equal(env.ToDictionary().Count, dict.Count);
            Assert.Equal("FOOBAR", dict["COMPUTERNAME"]);
        }

        [Fact]
        public void GeneratesEnvironmentForUserToken()
        {
            var identity = WindowsIdentity.GetCurrent();
            var userToken = identity.Token;
            
            var env = EnvironmentBlock.CreateForUser(userToken);
            var dict = env.ToDictionary();

            Assert.Equal(GetUserName(identity), dict["USERNAME"]);
        }

        static string GetUserName(WindowsIdentity identity)
        {
            // SYSTEM is a pseudo-user, the process is really running under the machine account 
            // and the username is MACHINENAME$
            var username = identity.GetUserName();
            if (username.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase))
                return Environment.MachineName + "$";

            return username;
        }
    }
}
