using System;
using System.Threading.Tasks;
using IronFoundry.Warden.Protocol;
using IronFoundry.Warden.Utilities;
using NLog;

namespace IronFoundry.Warden.Server
{
    public class WardenExceptionHandler
    {
        private readonly Logger log;
        private readonly Exception exception;
        private readonly MessageWriter messageWriter;

        public WardenExceptionHandler(Logger log, Exception exception, MessageWriter messageWriter)
        {
            if (log == null)
            {
                throw new ArgumentNullException("log");
            }
            this.log = log;

            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            this.exception = exception;

            if (messageWriter == null)
            {
                throw new ArgumentNullException("messageWriter");
            }
            this.messageWriter = messageWriter;
        }

        public async Task HandleAsync()
        {
            log.ErrorException(exception);

            var wardenException = exception as WardenException;
            if (wardenException != null)
            {
                var response = new ErrorResponse
                {
                    Message = wardenException.ResponseMessage + "\n",
                    Data = wardenException.StackTrace
                };
                await messageWriter.WriteAsync(response);
            }
        }
    }
}
