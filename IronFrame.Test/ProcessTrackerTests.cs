using System;
using System.Collections.Generic;
using System.Linq;
using IronFrame.Host;
using IronFrame.Messages;
using IronFrame.Messaging;
using IronFrame.Utilities;
using NSubstitute;
using Xunit;

namespace IronFrame
{
    public class ProcessTrackerTests
    {
        IProcess HostProcess { get; set; }
        JobObject JobObject { get; set; }
        ProcessHelper ProcessHelper { get; set; }
        IMessageTransport Transport { get; set; }
        ProcessTracker ProcessTracker { get; set; }

        public ProcessTrackerTests()
        {
            HostProcess = Substitute.For<IProcess>();
            JobObject = Substitute.For<JobObject>();
            ProcessHelper = Substitute.For<ProcessHelper>();
            Transport = Substitute.For<IMessageTransport>();

            ProcessTracker = new ProcessTracker(Transport, JobObject, HostProcess, ProcessHelper);
        }

        public class GetAllChildProcesses : ProcessTrackerTests
        {
            int[] ExpectedProcessIds { get; set; }
            int HostProcessId { get; set; }

            public GetAllChildProcesses() : base()
            {
                HostProcessId = 100;
                ExpectedProcessIds = new [] { 1, 2, 3, 4, 5, HostProcessId };

                HostProcess.Id.Returns(HostProcessId);

                JobObject.GetProcessIds().Returns(ExpectedProcessIds);
                ProcessHelper.GetProcesses(null)
                    .ReturnsForAnyArgs(call =>
                    {
                        var ids = call.Arg<IEnumerable<int>>();
                        return ids.Select(id =>
                        {
                            var p = Substitute.For<IProcess>();
                            p.Id.Returns(id);
                            return p;
                        });
                    });
            }
            
            [Fact]
            public void IncludesAllChildProcessesAsTrackedByJobObject()
            {
                var processes = ProcessTracker.GetAllChildProcesses();

                Assert.Collection(processes,
                    x => Assert.Equal(1, x.Id),
                    x => Assert.Equal(2, x.Id),
                    x => Assert.Equal(3, x.Id),
                    x => Assert.Equal(4, x.Id),
                    x => Assert.Equal(5, x.Id));
            }

            [Fact]
            public void DoesNotIncludeHostProcess()
            {
                var processes = ProcessTracker.GetAllChildProcesses();
                
                Assert.DoesNotContain(HostProcessId, processes.Select(p => p.Id));
            }
        }

        public class GetProcessByKey : ProcessTrackerTests
        {
            Guid KnownProcessKey { get; set; }
            IProcess KnownProcess { get; set; }
            
            public GetProcessByKey()
            {
                KnownProcessKey = Guid.NewGuid();
                KnownProcess = Substitute.For<IProcess>();

                ProcessTracker.TrackProcess(KnownProcessKey, KnownProcess);
            }

            [Fact]
            public void WhenProcessIsTracked_ReturnsProcess()
            {
                var process = ProcessTracker.GetProcessByKey(KnownProcessKey);

                Assert.Same(KnownProcess, process);
            }

            [Fact]
            public void WhenProcessIsNotTracked_ReturnsNull()
            {
                var process = ProcessTracker.GetProcessByKey(Guid.NewGuid());

                Assert.Null(process);
            }
        }

        public class HandleProcessData : ProcessTrackerTests
        {
            static ProcessDataEvent MatchProcessDataEvent(ProcessDataEvent expected)
            {
                return Arg.Is<ProcessDataEvent>(actual =>
                    expected.key == actual.key &&
                    expected.dataType == actual.dataType &&
                    expected.data == actual.data
                );
            }

            [Fact]
            public void PublishesEventToTransport()
            {
                var processKey = Guid.NewGuid();

                ProcessTracker.HandleProcessData(processKey, Messages.ProcessDataType.STDOUT, "This is STDOUT");

                var expectedProcessDataEvent = new ProcessDataEvent(processKey, ProcessDataType.STDOUT, "This is STDOUT");
                Transport.Received(1).PublishEventAsync<ProcessDataEvent>("processData", MatchProcessDataEvent(expectedProcessDataEvent));
            }
        }

        public class TrackProcess : ProcessTrackerTests
        {
            [Fact]
            public void AddsProcessToTracker()
            {
                var processKey = Guid.NewGuid();
                var process = Substitute.For<IProcess>();

                ProcessTracker.TrackProcess(processKey, process);

                var result = ProcessTracker.GetProcessByKey(processKey);
                Assert.Same(process, result);
            }

            [Fact]
            public void WhenProcessIsAlreadyBeingTracked_Throws()
            {
                var processKey = Guid.NewGuid();
                var process1 = Substitute.For<IProcess>();
                ProcessTracker.TrackProcess(processKey, process1);
                var process2 = Substitute.For<IProcess>();

                var ex = Record.Exception(() => ProcessTracker.TrackProcess(processKey, process2));

                Assert.Equal(
                    String.Format("A process with key '{0}' is already being tracked.", processKey),
                    ex.Message);
            }
        }
    }
}
