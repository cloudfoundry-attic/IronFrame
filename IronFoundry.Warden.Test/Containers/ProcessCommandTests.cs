using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IronFoundry.Container;
using IronFoundry.Container.Utilities;
using IronFoundry.Warden.Tasks;
using Xunit;
using NSubstitute;

namespace IronFoundry.Warden.Test.Containers
{
    public class ProcessCommandTests
    {
        public IContainer Container { get; set; }
        public IProcessIO ProcessIO { get; set; }
        public IRemoteCommandArgs CommandArgs { get; set; }
        public IContainerProcess Process { get; set; }

        public ProcessCommandTests()
        {
            Container = Substitute.For<IContainer>();
            ProcessIO = Substitute.For<IProcessIO>();
            CommandArgs = Substitute.For<IRemoteCommandArgs>();
            Process = Substitute.For<IContainerProcess>();
        }

        //[Fact]
        //public void SetsTheWorkingDirectoryInStartInfo()
        //{
        //    Container.Run(null, null).ReturnsForAnyArgs(Process);

        //    var cmd = new TestableProcessCommand(Container, ProcessIO, CommandArgs, "workingDir", "exe", "some args");
        //    var commandResult = cmd.InvokeAsync().Result;

        //    var spec = Container.Captured(c => c.Run(null, null)).Arg<ProcessSpec>();

        //    Assert.Equal("workingDir", spec.WorkingDirectory);
        //}

        ///// <summary>
        ///// Validate that the env log is created while the process is running.
        ///// </summary>
        //[Fact]
        //public void CreatesEnvLog()
        //{
        //    string envLogContent = InvokeCommandAndGetLogContent(ProcessCommand.EnvLogRelativePath);

        //    envLogContent.Should().Contain("env1")
        //        .And.Contain("val1")
        //        .And.Contain("val2");
        //}

        ///// <summary>
        ///// Validate that the pid log is created while the process is running.
        ///// </summary>
        //[Fact]
        //public void CreatesPidLog()
        //{
        //    string pidLogContent = InvokeCommandAndGetLogContent(ProcessCommand.PidLogRelativePath);

        //    pidLogContent.Should().Contain("100");
        //}

        ///// <summary>
        ///// Validate that the ProcessCommand translates expands environment values starting with @ROOT@
        ///// replacing it with the root of the container.
        ///// </summary>
        //[Fact]
        //public void RootsEnvironmentValues()
        //{
        //    string content = InvokeCommandAndGetLogContent(ProcessCommand.EnvLogRelativePath);

        //    string expectedPath = Path.Combine(tempDir, "sub\\dir");
        //    content.Should().Contain(expectedPath);
        //}

        //private string InvokeCommandAndGetLogContent(string logRelativePath)
        //{
        //    string content = null;

        //    // When Wait for exit is called, get the content of the env log.
        //    string expectedEnvPath = Path.Combine(tempDir, logRelativePath);
        //    process.When(p => p.WaitForExit()).Do(c => content = File.ReadAllText(expectedEnvPath));
        //    process.Id.Returns(100);

        //    container.ContainerDirectoryPath.Returns(tempDir);
        //    container.CreateProcess(null).ReturnsForAnyArgs(process);

        //    var env = new Dictionary<string, string> { { "env1", "val1" }, { "env2", "val2" }, { "rootedenv", "@ROOT@\\sub\\dir" } };
        //    var cmd = new TestableProcessCommand(container, tempDir, "some.exe", "some args", environment: env);

        //    cmd.Execute();

        //    return content;
        //}

        private class TestableProcessCommand : ProcessCommand
        {
            private string workingDirectory;
            private string exePath;
            private string[] exeArguments;

            public TestableProcessCommand(IContainer container, IProcessIO io, IRemoteCommandArgs args, string workingDirectory, string exePath, params string[] exeArgs)
            {
                this.Container = container;
                this.IO = io;
                this.CommandArgs = args;

                this.workingDirectory = workingDirectory;
                this.exePath = exePath;
                this.exeArguments = exeArgs;
            }

            protected override TaskCommandResult DoExecute()
            {
                return base.RunProcess(workingDirectory, exePath, exeArguments);
            }
        }
    }
}
