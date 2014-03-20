using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronFoundry.Warden.Shared.Messaging;

namespace IronFoundry.Warden.ContainerHost
{
    class ProcessContext
    {
        public ProcessContext()
        {
            StandardError = new StringBuilder();
            StandardOutputTail = new Queue<string>();
        }

        public bool HasExited { get; set; }
        public int ExitCode { get; set; }
        public StringBuilder StandardError { get; set; }
        public Queue<string> StandardOutputTail { get; set; }

        public void HandleErrorData(object sender, DataReceivedEventArgs e)
        {
            StandardError.AppendLine(e.Data);
        }

        public void HandleOutputData(object sender, DataReceivedEventArgs e)
        {
            while (StandardOutputTail.Count > 100)
                StandardOutputTail.Dequeue();

            StandardOutputTail.Enqueue(e.Data);
        }

        public void HandleProcessExit(object sender, EventArgs e)
        {
            var process = (Process)sender;

            HasExited = true;
            ExitCode = process.ExitCode;
        }
    }

    class Program
    {
        static ManualResetEvent exitEvent = new ManualResetEvent(false);
        static ConcurrentDictionary<int, ProcessContext> processContexts = new ConcurrentDictionary<int, ProcessContext>();

        static void Main(string[] args)
        {
            var input = Console.In;
            var output = Console.Out;
            using (var transport = new MessageTransport(input, output))
            {
                var dispatcher = new MessageDispatcher();
                dispatcher.RegisterMethod<CreateProcessRequest>("CreateProcess", CreateProcessHandler);
                dispatcher.RegisterMethod<GetProcessExitInfoRequest>("GetProcessExitInfo", GetProcessExitInfoHandler);

                transport.SubscribeRequest(
                    async (request) =>
                    {
                        var response = await dispatcher.DispatchAsync(request);
                        await transport.PublishAsync(response);
                    });

                exitEvent.WaitOne();
            }
        }

        private static Task<object> CreateProcessHandler(CreateProcessRequest request)
        {
            //Debug.Assert(false);

            var createProcessStartInfo = request.@params;
            var processContext = new ProcessContext();
            var process = new Process();

            process.StartInfo = ToProcessStartInfo(createProcessStartInfo);
            process.ErrorDataReceived += processContext.HandleErrorData;
            process.OutputDataReceived += processContext.HandleOutputData;
            process.Exited += processContext.HandleProcessExit;

            process.EnableRaisingEvents = true;

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            
            // Through much debugging we discovered that the combination of the ruby.exe process
            // and the DEA staging scripts require a new-line in order to run the script successfully.
            // This is a total hack and we should determine if we can work around the problem another way.
            process.StandardInput.WriteLine(Environment.NewLine);
            
            processContexts[process.Id] = processContext;

            return Task.FromResult<object>(
                new CreateProcessResponse(
                    request.id,
                    new CreateProcessResult
                    {
                        Id = process.Id,
                    }));
        }

        private static Task<object> GetProcessExitInfoHandler(GetProcessExitInfoRequest request)
        {
            //Debug.Assert(false);

            ProcessContext processContext;
            if (processContexts.TryGetValue(request.@params.Id, out processContext))
            {
                return Task.FromResult<object>(
                    new GetProcessExitInfoResponse(
                        request.id,
                        new GetProcessExitInfoResult
                        {
                            ExitCode = processContext.ExitCode,
                            HasExited = processContext.HasExited,
                            StandardError = processContext.StandardError.ToString(),
                            StandardOutputTail = String.Join("\n", processContext.StandardOutputTail),
                        }));
            }
            else
            {
                throw new Exception("The process doesn't exist.");
            }
        }

        private static ProcessStartInfo ToProcessStartInfo(CreateProcessStartInfo createProcessStartInfo)
        {
            var si = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                LoadUserProfile = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                WorkingDirectory = createProcessStartInfo.WorkingDirectory,
                FileName = createProcessStartInfo.FileName,
                Arguments = createProcessStartInfo.Arguments,
                UserName = createProcessStartInfo.UserName,
                Password = createProcessStartInfo.Password,
            };

            if (createProcessStartInfo.EnvironmentVariables.Count > 0)
            {
                si.EnvironmentVariables.Clear();
                foreach (string key in createProcessStartInfo.EnvironmentVariables.Keys)
                {
                    si.EnvironmentVariables[key] = createProcessStartInfo.EnvironmentVariables[key];
                }
            }

            return si;
        }

    }
}
