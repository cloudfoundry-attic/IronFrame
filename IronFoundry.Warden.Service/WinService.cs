namespace IronFoundry.Warden.Service
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Containers;
    using NLog;
    using Server;
    using Topshelf;
    using Utilities;

    public class WinService : ServiceControl
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly IContainerManager containerManager;
        private readonly TcpServer wardenServer;
        private readonly Task wardenServerTask;

        public WinService()
        {
            this.cancellationTokenSource = Statics.CancellationTokenSource;
            this.containerManager = Statics.ContainerManager;
            this.wardenServer = new TcpServer(containerManager, Statics.JobManager, Statics.WardenConfig.TcpPort, cancellationTokenSource.Token);
            this.wardenServerTask = new Task(wardenServer.Run, cancellationTokenSource.Token);
        }

        public bool Start(HostControl hostControl)
        {
            wardenServerTask.Start();
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            try
            {
                cancellationTokenSource.Cancel();

                Task.WaitAll(new[] { wardenServerTask }, (int)TimeSpan.FromSeconds(25).TotalMilliseconds);

                if (wardenServer.ClientListenException != null)
                {
                    log.ErrorException(wardenServer.ClientListenException);
                }

                containerManager.Dispose();
            }
            catch (Exception ex)
            {
                log.ErrorException(ex);
            }

            return true;
        }
    }
}
