using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using IronFoundry.Container.Messages;
using IronFoundry.Container.Messaging;
using IronFoundry.Container.Utilities;

namespace IronFoundry.Container.Host
{
    internal interface IProcessTracker
    {
        IReadOnlyList<IProcess> GetAllChildProcesses();
        IProcess GetProcessByKey(Guid key);
        void HandleProcessData(Guid key, ProcessDataType dataType, string data);
        void TrackProcess(Guid key, IProcess process);
    }

    internal class ProcessTracker : IProcessTracker
    {
        readonly IProcess hostProcess;
        readonly JobObject jobObject;
        readonly ConcurrentDictionary<Guid, IProcess> processes = new ConcurrentDictionary<Guid, IProcess>();
        readonly ProcessHelper processHelper;
        readonly IMessageTransport transport;

        public ProcessTracker(IMessageTransport transport, JobObject jobObject, IProcess hostProcess, ProcessHelper processHelper)
        {
            this.transport = transport;
            this.jobObject = jobObject;
            this.hostProcess = hostProcess;
            this.processHelper = processHelper;
        }

        public IReadOnlyList<IProcess> GetAllChildProcesses()
        {
            var ids = jobObject.GetProcessIds()
                .Where(id => hostProcess.Id != id)
                .ToList();

            return processHelper.GetProcesses(ids).ToList();
        }

        public IProcess GetProcessByKey(Guid key)
        {
            IProcess process;
            if (processes.TryGetValue(key, out process))
                return process;

            return null;
        }

        public void HandleProcessData(Guid key, ProcessDataType dataType, string data)
        {
            var dataEvent = new ProcessDataEvent(key, dataType, data);

            transport.PublishEventAsync("processData", dataEvent).GetAwaiter().GetResult();
        }

        public void TrackProcess(Guid key, IProcess process)
        {
            if (!processes.TryAdd(key, process))
                throw new InvalidOperationException(String.Format("A process with key '{0}' is already being tracked.", key));
        }
    }
}
