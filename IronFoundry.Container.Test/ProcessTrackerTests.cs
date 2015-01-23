using System;
using IronFoundry.Container.Host;
using IronFoundry.Container.Messages;
using IronFoundry.Container.Messaging;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using Xunit;

namespace IronFoundry.Container
{
    public class ProcessTrackerTests
    {
        IMessageTransport Transport { get; set; }
        ProcessTracker ProcessTracker { get; set; }

        public ProcessTrackerTests()
        {
            Transport = Substitute.For<IMessageTransport>();

            ProcessTracker = new ProcessTracker(Transport);
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
