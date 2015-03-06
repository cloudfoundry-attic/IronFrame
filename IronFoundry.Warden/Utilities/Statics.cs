namespace IronFoundry.Warden.Utilities
{
    using System;
    using System.IO;
    using System.Threading;
    using IronFoundry.Warden.Configuration;
    using IronFoundry.Warden.Containers;
    using IronFoundry.Warden.Jobs;
    using NLog;

    public static class Statics
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static readonly WardenConfig wardenConfig = new WardenConfig();
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
        private static readonly IContainerManager containerManager = new ContainerManager();
        private static readonly IJobManager jobManager = new JobManager();

        public static WardenConfig WardenConfig
        {
            get { return wardenConfig; }
        }

        public static CancellationTokenSource CancellationTokenSource
        {
            get { return cts; }
        }

        public static IContainerManager ContainerManager
        {
            get { return containerManager; }
        }

        public static IJobManager JobManager
        {
            get { return jobManager; }
        }

        public static void OnServiceStart()
        {
            try
            {
                string containerPath = wardenConfig.ContainerBasePath;
                Directory.CreateDirectory(containerPath);
                ContainerManager.RestoreContainers(containerPath, wardenConfig.WardenUsersGroup);
                // TODO: Restore snapshots (SnapshotManager)
            }
            catch (Exception ex)
            {
                log.ErrorException(String.Empty, ex);
                throw;
            }
        }
    }
}
