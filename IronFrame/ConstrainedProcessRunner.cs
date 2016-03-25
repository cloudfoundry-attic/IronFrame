using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IronFrame.Messages;
using IronFrame.Utilities;

namespace IronFrame
{
    internal class ConstrainedProcessRunner : IProcessRunner, IDisposable
    {
        public const int DefaultStopTimeout = 10000;

        IContainerHostClient hostClient;

        public ConstrainedProcessRunner(IContainerHostClient hostClient)
        {
            this.hostClient = hostClient;
            hostClient.Exited += (o, e) =>
            {
                this.hostClient = null;
            };
        }

        Action<ProcessDataEvent> BuildProcessDataCallback(Action<string> outputCallback, Action<string> errorCallback)
        {
            return (processData) =>
            {
                switch (processData.dataType)
                {
                    case ProcessDataType.STDOUT:
                        if (outputCallback != null)
                            outputCallback(processData.data);
                        break;

                    case ProcessDataType.STDERR:
                        if (errorCallback != null)
                            errorCallback(processData.data);
                        break;
                }
            };
        }

        public void Dispose()
        {
            if (hostClient != null)
            {
                hostClient.Dispose();
                hostClient = null;
            }
        }

        public IProcess Run(ProcessRunSpec runSpec)
        {
            Guid processKey = Guid.NewGuid();

            var defaultEnvironmentBlock = EnvironmentBlock.CreateSystemDefault();
            var environment = defaultEnvironmentBlock.Merge(runSpec.Environment).ToDictionary();

            CreateProcessParams @params = new CreateProcessParams
            {
                key = processKey,
                executablePath = runSpec.ExecutablePath,
                arguments = runSpec.Arguments,
                environment = environment,
                workingDirectory = runSpec.WorkingDirectory
            };

            var processDataCallback = BuildProcessDataCallback(runSpec.OutputCallback, runSpec.ErrorCallback);

            hostClient.SubscribeToProcessData(processKey, processDataCallback);

            var result = hostClient.CreateProcess(@params);
            var process = new ConstrainedProcess(hostClient, processKey, result.id, environment);

            return process;
        }

        public void StopAll(bool kill)
        {
            if (hostClient != null)
            {
                var timeout = kill ? 0 : DefaultStopTimeout;
                hostClient.StopAllProcesses(timeout);
            }
        }

        public IProcess FindProcessById(int id)
        {
            var @params = new FindProcessByIdParams
            {
                id = id,
            };
            var result = hostClient.FindProcessById(@params);
            if (result == null)
            {
                return null;
            }
            var env = result.environment.ToDictionary(pair => pair.Key, pair => pair.Value);
            return new ConstrainedProcess(hostClient, result.processKey, result.id, env);
        }
    }
}
