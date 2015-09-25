using System;
using System.Diagnostics;
using System.Threading;
using IronFrame.Host.Handlers;
using IronFrame.Messages;
using IronFrame.Messaging;
using IronFrame.Utilities;
using System.Runtime.InteropServices;

namespace IronFrame.Host
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int SetErrorMode(int wMode);

        static ManualResetEvent exitEvent = new ManualResetEvent(false);
        static ProcessTracker processTracker;
        static string containerId = null;
        static JobObject hostJobObject = null;
        static IProcess hostProcess = null;

        static void Main(string[] args)
        {
            //Debugger.Launch();

            SetErrorMode(0x0002 | SetErrorMode(0x0002)); // SEM_NOGPFAULTERRORBOX = 0x0002

            if (args.Length == 0)
                ExitWithError("Must specify container-id as the first argument.", -1);

            containerId = args[0];

            var hostJobObjectName = String.Format("{0}:host", containerId);
            hostJobObject = new JobObject(hostJobObjectName);

            hostProcess = ProcessHelper.WrapProcess(Process.GetCurrentProcess());

            var input = Console.In;
            var output = Console.Out;

            using (var transport = MessageTransport.Create(input, output))
            {
                processTracker = new ProcessTracker(transport, hostJobObject, hostProcess, new ProcessHelper());

                var createProcessHandler = new CreateProcessHandler(new ProcessRunner(), processTracker);
                var pingHandler = new PingHandler();
                var findProcessByIdHandler = new FindProcessByIdHandler(processTracker);
                var stopProcessHandler = new StopProcessHandler(processTracker);
                var stopAllProcessesHandler = new StopAllProcessesHandler(processTracker);
                var waitForProcessExitHandler = new WaitForProcessExitHandler(processTracker);

                var dispatcher = new MessageDispatcher();
                dispatcher.RegisterMethod<CreateProcessRequest>(
                    CreateProcessRequest.MethodName,
                    async (request) =>
                    {
                        var result = await createProcessHandler.ExecuteAsync(request.@params);
                        return new CreateProcessResponse(request.id, result);
                    });
                dispatcher.RegisterMethod<PingRequest>(
                    PingRequest.MethodName,
                    async (request) =>
                    {
                        await pingHandler.ExecuteAsync();
                        return new PingResponse(request.id);
                    });
                dispatcher.RegisterMethod<FindProcessByIdRequest>(
                    FindProcessByIdRequest.MethodName,
                    async (request) =>
                    {
                        var result = await findProcessByIdHandler.ExecuteAsync(request.@params);
                        return new FindProcessByIdResponse(request.id, result);
                    });
                dispatcher.RegisterMethod<StopProcessRequest>(
                    StopProcessRequest.MethodName,
                    async (request) =>
                    {
                        await stopProcessHandler.ExecuteAsync(request.@params);
                        return new StopProcessResponse(request.id);
                    });
                dispatcher.RegisterMethod<StopAllProcessesRequest>(
                    StopAllProcessesRequest.MethodName,
                    async (request) =>
                    {
                        await stopAllProcessesHandler.ExecuteAsync(request.@params);
                        return new StopAllProcessesResponse(request.id);
                    });
                dispatcher.RegisterMethod<WaitForProcessExitRequest>(
                    WaitForProcessExitRequest.MethodName,
                    async (request) =>
                    {
                        var result = await waitForProcessExitHandler.ExecuteAsync(request.@params);
                        return new WaitForProcessExitResponse(request.id, result);
                    });

                transport.SubscribeRequest(
                    async (request) =>
                    {
                        var response = await dispatcher.DispatchAsync(request);
                        await transport.PublishResponseAsync(response);
                    });

                transport.Start();

                ReportOk();
                exitEvent.WaitOne();
            }
        }

        static void ReportOk()
        {
            Console.Error.WriteLine("OK");
        }

        static void ExitWithError(string message, int exitCode)
        {
            Console.Error.WriteLine(message);
            Environment.Exit(exitCode);
        }
    }
}
