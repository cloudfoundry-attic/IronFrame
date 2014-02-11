using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IronFoundry.Warden.Protocol;

namespace IronFoundry.Warden.Handlers
{
    public abstract class RequestHandler
    {
        private readonly Request request;

        public RequestHandler(Request request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            this.request = request;
        }

        public abstract Task<Response> HandleAsync();

        public override string ToString()
        {
            return String.Format("{0}: {1}", this.GetType().ToString(), request.ToString());
        }

        protected ResponseData GetResponseData(bool isErrorCase, string fmt, params object[] args)
        {
            if (fmt.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("fmt");
            }

            string errorMessage = String.Empty;
            if (args.IsNullOrEmpty())
            {
                errorMessage = fmt;
            }
            else
            {
                errorMessage = String.Format(fmt, args);
            }

            if (!errorMessage.EndsWith("\n"))
            {
                errorMessage = String.Concat(errorMessage, "\n");
            }

            return new ResponseData(isErrorCase ? 1 : 0, errorMessage);
        }
    }
}
