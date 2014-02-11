using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Jobs;
using NLog;
using IronFoundry.Warden.Properties;
using IronFoundry.Warden.Protocol;

namespace IronFoundry.Warden.Handlers
{
    public class LinkRequestHandler : JobRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly LinkRequest request;

        public LinkRequestHandler(IContainerManager containerManager, IJobManager jobManager, Request request)
            : base(containerManager, jobManager, request)
        {
            this.request = (LinkRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            log.Trace("Handle: '{0}' JobId: '{1}'", request.Handle, request.JobId);

            return Task.Run<Response>(() =>
                {
                    LinkResponse response = null;

                    Job job = jobManager.GetJob(request.JobId);
                    if (job == null)
                    {
                        ResponseData responseData = GetResponseData(true, Resources.JobRequestHandler_NoSuchJob_Message);
                        response = new LinkResponse
                        {
                            ExitStatus = (uint)responseData.ExitStatus,
                            Stderr = responseData.Message,
                        };
                    }
                    else
                    {
                        IJobResult result = job.RunnableTask.Result;
                        response = new LinkResponse
                        {
                            ExitStatus = (uint)result.ExitCode,
                            Stderr = result.Stderr,
                            Stdout = result.Stdout,
                        };
                    }

                    response.Info = BuildInfoResponse();

                    return response;
                });
        }
    }
}
