using System;
using System.Collections.Concurrent;
using IronFoundry.Container.Messages;
using IronFoundry.Container.Messaging;
using IronFoundry.Warden.Utilities;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Container.Host
{
    public interface IProcessTracker
    {
        IProcess GetProcessByKey(Guid key);
        void HandleProcessData(Guid key, ProcessDataType dataType, string data);
        void TrackProcess(Guid key, IProcess process);
    }

    public class ProcessTracker : IProcessTracker
    {
        ConcurrentDictionary<Guid, IProcess> processes = new ConcurrentDictionary<Guid, IProcess>();
        MessageTransport transport;

        public ProcessTracker(MessageTransport transport)
        {
            this.transport = transport;
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
            transport.PublishEventAsync(JObject.FromObject(dataEvent));
        }

        public void TrackProcess(Guid key, IProcess process)
        {
            if (!processes.TryAdd(key, process))
                throw new InvalidOperationException(String.Format("A process with key '{0}' is already being tracked.", key));
        }
    }
}
