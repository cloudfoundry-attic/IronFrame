namespace IronFoundry.Warden.Server
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using IronFoundry.Container;
    using IronFoundry.Warden.Containers;
    using IronFoundry.Warden.Handlers;
    using IronFoundry.Warden.Jobs;
    using IronFoundry.Warden.Protocol;
    using IronFoundry.Warden.Utilities;
    using NLog;

    public class MessageHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IContainerManager containerManager;
        private readonly IJobManager jobManager;
        private readonly CancellationToken cancellationToken;
        private readonly MessageWriter messageWriter;

        public MessageHandler(IContainerManager containerManager, IJobManager jobManager,
            CancellationToken cancellationToken, MessageWriter messageWriter)
        {
            if (containerManager == null)
            {
                throw new ArgumentNullException("containerManager");
            }
            if (jobManager == null)
            {
                throw new ArgumentNullException("jobManager");
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }
            if (messageWriter == null)
            {
                throw new ArgumentNullException("messageWriter");
            }
            this.containerManager = containerManager;
            this.jobManager = jobManager;
            this.cancellationToken = cancellationToken;
            this.messageWriter = messageWriter;
        }

        public async Task Handle(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            log.Trace("MessageType: '{0}'", message.MessageType.ToString());

            var unwrapper = new MessageUnwrapper(message);
            Request request = unwrapper.GetRequest();

            RequestHandler handler = null;
            ErrorResponse errorResponse = null;
            try
            {
                var factory = new RequestHandlerFactory(containerManager, jobManager, message.MessageType, request);
                handler = factory.GetHandler();
            }
            catch (Exception ex)
            {
                log.ErrorException(ex);
                errorResponse = new ErrorResponse { Message = ex.Message };
            }

            if (errorResponse != null)
            {
                await messageWriter.WriteAsync(errorResponse);
                return;
            }

            try
            {
                var streamingHandler = handler as IStreamingHandler;
                if (streamingHandler != null)
                {
                    Response finalResponse = await streamingHandler.HandleAsync(messageWriter, cancellationToken);
                    if (finalResponse == null)
                    {
                        string errorMessage = String.Format("Null final response from streaming handler '{0}'", streamingHandler.GetType());
                        log.Error(errorMessage);
                        finalResponse = new ErrorResponse { Message = errorMessage };
                    }
                    await messageWriter.WriteAsync(finalResponse);
                    return;
                }
                else
                {
                    Response response = await handler.HandleAsync();
                    await messageWriter.WriteAsync(response);
                }
            }
            catch (Exception ex)
            {
                if (ex is WardenException)
                {
                    throw;
                }
                else
                {
                    throw new WardenException(String.Format("Exception in request handler '{0}'", handler.ToString()), ex);
                }
            }
            log.Trace("Finished handling message: '{0}'", message.MessageType.ToString());
        }
    }
}
