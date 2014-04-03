using System;
using System.IO;
using NLog;
using Topshelf;

namespace IronFoundry.Warden.Service
{
    static class Program
    {
        static readonly Logger log = LogManager.GetCurrentClassLogger();

        static int Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            log.Info("Current directory is: '{0}'", Directory.GetCurrentDirectory());

            var exitCode = HostFactory.Run(x =>
                {
                    x.Service<WinService>();
                    x.SetDescription(Constants.DisplayName);
                    x.SetDisplayName(Constants.DisplayName);
                    // NB: very important, must match installer
                    x.SetServiceName(Constants.ServiceName);
                    x.StartAutomaticallyDelayed();
                    x.RunAsPrompt();
                    x.UseNLog();
                });

            return (int)exitCode;
        }
    }
}
