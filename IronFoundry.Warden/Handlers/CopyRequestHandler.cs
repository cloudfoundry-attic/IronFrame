namespace IronFoundry.Warden.Handlers
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Containers;
    using Microsoft.VisualBasic.FileIO;
    using NLog;
    using Protocol;
    using Utilities;

    public abstract class CopyRequestHandler : ContainerRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ICopyRequest request;
        private readonly Response response;

        public CopyRequestHandler(IContainerManager containerManager, Request request, Response response)
            : base(containerManager, request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            this.request = (ICopyRequest)request;

            if (response == null)
            {
                throw new ArgumentNullException("response");
            }
            this.response = response;
        }

        public override Task<Response> HandleAsync()
        {
            var copyResponse = Task.FromResult<Response>(response);

            log.Trace("SrcPath: '{0}' DstPath: '{1}'", request.SrcPath, request.DstPath);

            IContainerClient container = GetContainer();
            if (container == null)
            {
                return copyResponse;
            }

            string sourcePath = container.ConvertToPathWithin(request.SrcPath);

            bool sourceIsDir = false;
            try
            {
                var sourceAttrs = File.GetAttributes(sourcePath);
                sourceIsDir = sourceAttrs.HasFlag(FileAttributes.Directory);
            }
            catch (FileNotFoundException ex)
            {
                log.DebugException(ex);
                return copyResponse;
            }

            string destinationPath = container.ConvertToPathWithin(request.DstPath);
            var destinationAttrs = File.GetAttributes(destinationPath);
            bool destinationIsDir = destinationAttrs.HasFlag(FileAttributes.Directory);

            if (sourceIsDir && destinationIsDir)
            {
                FileSystem.CopyDirectory(sourcePath, destinationPath);
            }
            else if (!sourceIsDir && destinationIsDir)
            {
                var fileName = Path.GetFileName(sourcePath);
                File.Copy(sourcePath, Path.Combine(destinationPath, fileName), true);
            }
            else
            {
                File.Copy(sourcePath, destinationPath, true);
            }

            return copyResponse;
        }
    }
}
