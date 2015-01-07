
namespace IronFoundry.Warden.Test.Tasks
{
    using System.IO;
    using FluentAssertions;
    using IronFoundry.Warden.Tasks;
    using Xunit;
    using Newtonsoft.Json;

    public class TaskCommandDTODeserializationTests
    {
        /// <summary>
        /// Verify that the old message containing only cmd and args continues to be deserialized correctly.
        /// </summary>
        [Fact]
        public void WhenOldMessageDeserialized()
        {
            string json = @"{ cmd: ""foo"", args: [ ""arg1"", ""arg2""] }";
            var task = DeserializeJson(json);

            task.Command.Should().Be("foo");
            task.Args.Should().BeEquivalentTo(new[] {"arg1", "arg2"});
            task.Environment.Should().BeNull();
        }

        /// <summary>
        /// Verify that the new messages containing the environment that should be used with the invoke
        /// can be deserialized correctly.
        /// </summary>
        [Fact]
        public void WhenMessageWithEnvironmentDeserialized()
        {
            string json = @"{ cmd: ""foo"", args: [ ""arg1"", ""arg2""], env: { env1: ""val1"", env2: ""val2"" } }";
            var task = DeserializeJson(json);

            task.Command.Should().Be("foo");
            task.Args.Should().BeEquivalentTo(new[] { "arg1", "arg2" });
            task.Environment.Should().HaveCount(2);
            task.Environment.Should().HaveCount(2).And.Contain("env1", "val1").And.Contain("env2", "val2");
        }

        TaskCommandDTO DeserializeJson(string json)
        {
            JsonSerializer serializer = new JsonSerializer();
            var textReader = new JsonTextReader(new StringReader(json));
            var task = serializer.Deserialize<TaskCommandDTO>(textReader);
            return task;
        }

    }
}
