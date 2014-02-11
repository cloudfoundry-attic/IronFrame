using System;
using System.IO;
using System.Net.Sockets;

namespace IronFoundry.Warden.Server
{
    public class SocketExceptionHandler
    {
        private readonly Exception exception;

        public SocketExceptionHandler(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            this.exception = exception;
        }

        public bool Handle()
        {
            bool handled = false;

            var ioException = exception as IOException;
            if (ioException != null)
            {
                var socketException = ioException.InnerException as SocketException;
                if (socketException != null)
                {
                    switch (socketException.SocketErrorCode)
                    {
                        case SocketError.ConnectionAborted:
                        case SocketError.ConnectionReset:
                            handled = true;
                            break;
                    }
                }
            }

            return handled;
        }
    }
}
