using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IronFoundry.Container.Utilities;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace IronFoundry.Container
{
    public class LocalFilePropertyServiceTests
    {
        Clock Clock { get; set; }
        IContainer Container { get; set; }
        IContainerDirectory Directory { get; set; }
        FileSystemManager FileSystem { get; set; }
        string PropertiesFilePath { get; set; }
        LocalFilePropertyService PropertyService { get; set; }

        public LocalFilePropertyServiceTests()
        {
            PropertiesFilePath = @"C:\Containers\handle\properties.json";

            Directory = Substitute.For<IContainerDirectory>();
            Directory.MapPrivatePath("properties.json").Returns(PropertiesFilePath);

            Clock = Substitute.For<Clock>();

            Container = Substitute.For<IContainer>();
            Container.Directory.Returns(Directory);

            FileSystem = Substitute.For<FileSystemManager>();

            PropertyService = new LocalFilePropertyService(FileSystem, "properties.json", Clock);
        }

        protected MemoryStream SetupPropertiesFile(string json = "")
        {
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var stream = new MemoryStream();
            stream.Write(jsonBytes, 0, jsonBytes.Length);
            stream.Position = 0;

            FileSystem.OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>())
                .Returns(stream);

            return stream;
        }

        public class GetProperties : LocalFilePropertyServiceTests
        {
            [Fact]
            public void ReadsPropertiesFromFileSystem()
            {
                SetupPropertiesFile(@"
                {
                    ""name1"": ""value1"",
                    ""name2"": ""value2""
                }
                ");

                var properties = PropertyService.GetProperties(Container);

                Assert.Equal("value1", properties["name1"]);
                Assert.Equal("value2", properties["name2"]);
            }

            [InlineData("")]
            [InlineData("{}")]
            [Theory]
            public void WhenPropertiesFileIsEmpty_ReturnsEmptyProperties(string json)
            {
                SetupPropertiesFile(json);

                var properties = PropertyService.GetProperties(Container);

                Assert.Empty(properties);
            }

            [Fact]
            public void ReadsPropertiesWithCorrectOpenFileFlags()
            {
                SetupPropertiesFile();

                PropertyService.GetProperties(Container);

                FileSystem.Received(1).OpenFile(PropertiesFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read | FileShare.Delete);
            }

            [Fact]
            public void WhenFileIsLocked_Retries()
            {
                FileSystem.OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>())
                    .Returns(
                        call => { throw new UnauthorizedAccessException(); },
                        call => { return new MemoryStream(); }
                    );

                PropertyService.GetProperties(Container);

                FileSystem.Received(2).OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>());
                Clock.Received(1).Sleep(Arg.Any<int>());
            }

            [Fact]
            public void WhenFileIsLockedAndNumberOfRetriesExceedsMax_Throws()
            {
                FileSystem.OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>())
                    .Throws(new UnauthorizedAccessException());

                var ex = Record.Exception(() => PropertyService.GetProperties(Container));

                Assert.IsAssignableFrom<UnauthorizedAccessException>(ex);
                FileSystem.Received(10).OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>());
                Clock.Received(9).Sleep(Arg.Any<int>());
            }
        }

        public class GetProperty : GetProperties
        {
            public GetProperty()
            {
                SetupPropertiesFile(@"
                {
                    ""name1"": ""value1"",
                    ""name2"": ""value2""
                }
                ");
            }

            [Fact]
            public void ReturnsPropertyValue()
            {
                var value = PropertyService.GetProperty(Container, "name1");

                Assert.Equal("value1", value);
            }

            [Fact]
            public void WhenPropertyDoesNotExist_ReturnsNull()
            {
                var value = PropertyService.GetProperty(Container, "unknown");

                Assert.Null(value);
            }

            [Fact]
            public void PropertyNameIsCaseSensitive()
            {
                var value = PropertyService.GetProperty(Container, "NAME1");

                Assert.Null(value);
            }
        }

        public class ModifiesPropertiesTests : LocalFilePropertyServiceTests
        {
            protected MemoryStream Stream { get; set; }

            public ModifiesPropertiesTests()
            {
                Stream = SetupPropertiesFile(@"
                {
                    ""name1"": ""value1"",
                    ""name2"": ""value2""
                }
                ");
            }

            protected JObject GetParsedJson()
            {
                byte[] bytes = Stream.ToArray();
                var json = Encoding.UTF8.GetString(bytes);
                return JObject.Parse(json);
            }
        }

        public class RemoveProperty : ModifiesPropertiesTests
        {
            [Fact]
            public void RemovesProperty()
            {
                PropertyService.RemoveProperty(Container, "name1");

                var properties = GetParsedJson();
                Assert.Null(properties.Property("name1"));
                Assert.NotNull(properties.Property("name2"));
            }

            [Fact]
            public void WhenPropertyDoesNotExist_DoesNotThrow()
            {
                var ex = Record.Exception(() => PropertyService.RemoveProperty(Container, "name"));

                Assert.Null(ex);
            }

            [Fact]
            public void OpensFileWithCorrectFlags()
            {
                PropertyService.RemoveProperty(Container, "name");

                FileSystem.Received(1).OpenFile(PropertiesFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None | FileShare.Delete);
            }

            [Fact]
            public void WhenFileIsLocked_Retries()
            {
                FileSystem.OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>())
                    .Returns(
                        call => { throw new UnauthorizedAccessException(); },
                        call => { return new MemoryStream(); }
                    );

                PropertyService.RemoveProperty(Container, "name");

                FileSystem.Received(2).OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>());
                Clock.Received(1).Sleep(Arg.Any<int>());
            }

            [Fact]
            public void WhenFileIsLockedAndNumberOfRetriesExceedsMax_Throws()
            {
                FileSystem.OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>())
                    .Throws(new UnauthorizedAccessException());

                var ex = Record.Exception(() => PropertyService.RemoveProperty(Container, "name"));

                Assert.IsAssignableFrom<UnauthorizedAccessException>(ex);
                FileSystem.Received(10).OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>());
                Clock.Received(9).Sleep(Arg.Any<int>());
            }
        }

        public class SetProperties : LocalFilePropertyServiceTests
        {
            Dictionary<string, string> Properties { get; set; }

            public SetProperties()
            {
                Properties = new Dictionary<string, string>
                {
                    { "name1", "value1" },
                    { "name2", "value2" },
                };
            }

            protected JObject GetParsedJson(MemoryStream stream)
            {
                byte[] bytes = stream.ToArray();
                var json = Encoding.UTF8.GetString(bytes);
                return JObject.Parse(json);
            }

            [Fact]
            public void SetsProperties()
            {
                var stream = SetupPropertiesFile();

                PropertyService.SetProperties(Container, Properties);

                var properties = GetParsedJson(stream);
                Assert.Equal("value1", (string)properties["name1"]);
                Assert.Equal("value2", (string)properties["name2"]);
            }

            [Fact]
            public void OverwritesExistingProperties()
            {
                var stream = SetupPropertiesFile(@"
                {
                    ""foo"": ""one"",
                    ""bar"": ""two"",
                }
                ");

                PropertyService.SetProperties(Container, Properties);

                var properties = GetParsedJson(stream);
                Assert.Equal("value1", (string)properties["name1"]);
                Assert.Equal("value2", (string)properties["name2"]);
                Assert.Null(properties.Property("foo"));
                Assert.Null(properties.Property("bar"));
            }

            [Fact]
            public void OpensFileWithCorrectFlags()
            {
                SetupPropertiesFile();

                PropertyService.SetProperties(Container, Properties);

                FileSystem.Received(1).OpenFile(PropertiesFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None | FileShare.Delete);
            }

            [Fact]
            public void WhenFileIsLocked_Retries()
            {
                FileSystem.OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>())
                    .Returns(
                        call => { throw new UnauthorizedAccessException(); },
                        call => { return new MemoryStream(); }
                    );

                PropertyService.SetProperties(Container, Properties);

                FileSystem.Received(2).OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>());
                Clock.Received(1).Sleep(Arg.Any<int>());
            }

            [Fact]
            public void WhenFileIsLockedAndNumberOfRetriesExceedsMax_Throws()
            {
                FileSystem.OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>())
                    .Throws(new UnauthorizedAccessException());

                var ex = Record.Exception(() => PropertyService.SetProperties(Container, Properties));

                Assert.IsAssignableFrom<UnauthorizedAccessException>(ex);
                FileSystem.Received(10).OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>());
                Clock.Received(9).Sleep(Arg.Any<int>());
            }
        }

        public class SetProperty : ModifiesPropertiesTests
        {
            [Fact]
            public void SetsPropertyValue()
            {
                PropertyService.SetProperty(Container, "name", "value");

                var properties = GetParsedJson();
                Assert.Equal("value", (string)properties["name"]);
            }

            [Fact]
            public void WhenPropertyAlreadyExists_OverwritesPropertyValue()
            {
                Stream = SetupPropertiesFile(@"
                {
                    ""name1"": ""value1"",
                    ""name2"": ""value2""
                }
                ");

                PropertyService.SetProperty(Container, "name2", "different-value");

                var properties = GetParsedJson();
                Assert.Equal("different-value", (string)properties["name2"]);
            }

            [Fact]
            public void PropertyNameIsCaseSensitive()
            {
                Stream = SetupPropertiesFile(@"
                {
                    ""name1"": ""value1"",
                    ""name2"": ""value2""
                }
                ");

                PropertyService.SetProperty(Container, "NAME2", "different-value");

                var properties = GetParsedJson();
                Assert.Equal("value2", (string)properties["name2"]);
                Assert.Equal("different-value", (string)properties["NAME2"]);
            }

            [Fact]
            public void OpensFileWithCorrectFlags()
            {
                PropertyService.SetProperty(Container, "name", "value");

                FileSystem.Received(1).OpenFile(PropertiesFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None | FileShare.Delete);
            }

            [Fact]
            public void WhenFileIsLocked_Retries()
            {
                FileSystem.OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>())
                    .Returns(
                        call => { throw new UnauthorizedAccessException(); },
                        call => { return new MemoryStream(); }
                    );

                PropertyService.SetProperty(Container, "name", "value");

                FileSystem.Received(2).OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>());
                Clock.Received(1).Sleep(Arg.Any<int>());
            }

            [Fact]
            public void WhenFileIsLockedAndNumberOfRetriesExceedsMax_Throws()
            {
                FileSystem.OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>())
                    .Throws(new UnauthorizedAccessException());

                var ex = Record.Exception(() => PropertyService.SetProperty(Container, "name", "value"));

                Assert.IsAssignableFrom<UnauthorizedAccessException>(ex);
                FileSystem.Received(10).OpenFile(PropertiesFilePath, Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>());
                Clock.Received(9).Sleep(Arg.Any<int>());
            }
        }
    }
}
