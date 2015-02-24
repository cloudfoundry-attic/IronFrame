using System.Collections.Generic;

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
            log.Info("Stopping Warden");
            try
            {
                cancellationTokenSource.Cancel();

                Task.WaitAll(new[] { wardenServerTask }, (int)TimeSpan.FromSeconds(25).TotalMilliseconds);

                if (wardenServer.ClientListenException != null)
                {
                    log.Log(LogLevel.Error, String.Empty, wardenServer.ClientListenException);
                }

                var destroyTasks = new List<Task>();
                foreach (ContainerHandle handle in containerManager.Handles)
                {
                    try
                    {
                        log.Info("Destroying container handle {0}", handle.ToString());
                        destroyTasks.Add(containerManager.DestroyContainerAsync(handle));
                    }
                    catch (Exception e)
                    {
                        log.Log(LogLevel.Error, String.Empty, e);
                    }
                }
                Task.WaitAll(destroyTasks.ToArray(), 2000);
                containerManager.Dispose();
            }
            catch (Exception ex)
            {
                log.Log(LogLevel.Error, String.Empty, ex);
            }

            return true;
        }
    }
}
