namespace IronFoundry.Warden.Test.Utilities
{
    using System.Collections;
    using System.Collections.Generic;
    using FluentAssertions;
    using IronFoundry.Warden.Utilities;
    using Xunit;

    public class EnvironmentBlockTests
    {
        [Fact]
        public void CanBeBuiltFromDictionary()
        {
            var env = EnvironmentBlock.Create(new Dictionary<string, string> {{"FOO", "BAR"}});
            var dict = env.ToDictionary();
            dict.Should().HaveCount(1).And.ContainKey("FOO").And.ContainValue("BAR");
        }

        [Fact]
        public void GeneratesDefaultEnvironment()
        {
            var env = EnvironmentBlock.GenerateDefault();
            var dict = env.ToDictionary();

            dict.Count.Should().BeGreaterOrEqualTo(0);
            // Verify some of the environment variables we expect to be there by default
            dict.Should().ContainKeys("TEMP", "TMP", "SystemRoot", "COMPUTERNAME");
        }

        [Fact]
        public void MergeOverwritesOld()
        {
            var env = EnvironmentBlock.GenerateDefault();
            var newEnv = EnvironmentBlock.Create(new Hashtable {{"COMPUTERNAME", "FOOBAR"}});
            var dict = env.Merge(newEnv).ToDictionary();

            dict.Should().HaveCount(env.ToDictionary().Count);
            dict["COMPUTERNAME"].Should().Be("FOOBAR");
        }
    }
}
