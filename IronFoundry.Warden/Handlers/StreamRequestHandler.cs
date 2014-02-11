namespace IronFoundry.Warden.Handlers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Containers;
    using Jobs;
    using Properties;
    using Protocol;
    using Utilities;
    using NLog;
    using System.Collections.Generic;

    public class StreamRequestHandler : JobRequestHandler, IStreamingHandler, IJobListener
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly StreamRequest request;

        private MessageWriter messageWriter;

        public StreamRequestHandler(IContainerManager containerManager, IJobManager jobManager, Request request)
            : base(containerManager, jobManager, request)
        {
            this.request = (StreamRequest)request;
        }

        public Task ListenStatusAsync(IJobStatus jobStatus)
        {
            Task rv = null;

            if (messageWriter == null)
            {
                throw new InvalidOperationException("messageWriter can not be null when job status observed.");
            }

            if (jobStatus == null)
            {
                log.Warn("Unexpected null job status!");
            }
            else
            {
                StreamResponse response = ToStreamResponse(jobStatus);
                rv = messageWriter.WriteAsync(response);
            }

            return rv;
        }

        public async Task<StreamResponse> HandleAsync(MessageWriter messageWriter, CancellationToken cancellationToken)
        {
            if (messageWriter == null)
            {
                throw new ArgumentNullException("messageWriter");
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            this.messageWriter = messageWriter;

            log.Trace("HandleAsync: '{0}' JobId: '{1}''", request.Handle, request.JobId);

            StreamResponse streamResponse = null;

            Job job = GetJobById(request.JobId, ref streamResponse); // if job is null, streamResponse is set to error
            if (job != null)
            {
                if (job.HasStatus)
                {
                    var awaitables = new List<Task>();
                    foreach (IJobStatus status in job.RetrieveStatus())
                    {
                        awaitables.Add(ListenStatusAsync(status));
                    }
                    await Task.WhenAll(awaitables);
                }

                try
                {
                    if (job.IsCompleted)
                    {
                        if (job.Result == null)
                        {
                            streamResponse = ToStreamResponse(GetResponseData(true, "Error! Job with ID '{0}' is completed but no result is available!\n", request.JobId));
                        }
                        else
                        {
                            streamResponse = ToStreamResponse(job.Result);
                        }
                    }
                    else
                    {
                        job.AttachListener(this);
                        IJobResult result = await job.ListenAsync();
                        streamResponse = StreamResponse.Create(result.ExitCode);
                    }
                }
                finally
                {
                    jobManager.RemoveJob(request.JobId);
                }
            }

            return streamResponse;
        }

        public override Task<Response> HandleAsync()
        {
            throw new InvalidOperationException("StreamRequestHandler implements IStreamingHandler so HandleAsync() should be called!");
        }

        /// <summary>
        /// If job can't be found, streamResponse is set to error
        /// </summary>
        private Job GetJobById(uint jobId, ref StreamResponse streamResponse)
        {
            Job job = jobManager.GetJob(jobId);
            if (job == null)
            {
                streamResponse = ToStreamResponse(GetResponseData(true, Resources.JobRequestHandler_NoSuchJob_Message));
            }
            return job;
        }

        private static StreamResponse ToStreamResponse(ResponseData responseData)
        {
            StreamResponse response = null;

            switch (responseData.ExitStatus)
            {
                case 0:
                    response = StreamResponse.Create(responseData.ExitStatus, responseData.Message, null);
                    break;
                default:
                    response = StreamResponse.Create(responseData.ExitStatus, null, responseData.Message);
                    break;
            }

            return response;
        }

        private static StreamResponse ToStreamResponse(IJobResult result)
        {
            return StreamResponse.Create(result.ExitCode, result.Stdout, result.Stderr);
        }

        private static StreamResponse ToStreamResponse(IJobStatus status)
        {
            return StreamResponse.Create(status.ExitStatus, status.DataSource.ToString(), status.Data);
        }
    }
}
