namespace IronFoundry.Warden.Handlers
{
    using System.Threading.Tasks;
    using Containers;
    using Jobs;
    using NLog;
    using Protocol;

    public class RunRequestHandler : TaskRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly RunRequest request;

        public RunRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
            this.request = (RunRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            log.Trace("Handle: '{0}' Script: '{1}'", request.Handle, request.Script);

            IJobRunnable runnable = base.GetRunnableFor(request);

            return Task.Run<Response>(() =>
                {
                    IJobResult result = runnable.Run(); // run synchronously
                    return new RunResponse
                        {
                            ExitStatus = (uint)result.ExitCode,
                            Stdout = result.Stdout,
                            Stderr = result.Stderr,
                            Info = BuildInfoResponse()
                        };
                });
        }
    }
}
