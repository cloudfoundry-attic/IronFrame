using System;
using System.Diagnostics;
using System.Threading;
using IronFoundry.Container.Host.Handlers;
using IronFoundry.Container.Messages;
using IronFoundry.Container.Messaging;
using IronFoundry.Container.Utilities;

namespace IronFoundry.Container.Host
{
    class Program
    {
        static ManualResetEvent exitEvent = new ManualResetEvent(false);
        static ProcessTracker processTracker;
        static string containerId = null;
        static JobObject hostJobObject = null;
        static IProcess hostProcess = null;

        static void Main(string[] args)
        {
            //Debugger.Launch();

            if (args.Length == 0)
                ExitWithError("Must specify container-id as the first argument.", -1);

            containerId = args[0];

            var hostJobObjectName = String.Format("{0}:host", containerId);
            hostJobObject = new JobObject(hostJobObjectName);

            hostProcess = ProcessHelper.WrapProcess(Process.GetCurrentProcess());

            var input = Console.In;
            var output = Console.Out;

            using (var transport = new MessageTransport(input, output))
            {
                processTracker = new ProcessTracker(transport, hostJobObject, hostProcess, new ProcessHelper());

                var createProcessHandler = new CreateProcessHandler(new ProcessRunner(), processTracker);
                var pingHandler = new PingHandler();
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
