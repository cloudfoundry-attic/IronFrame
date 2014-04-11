using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CommandLine;
using CommandLine.Text;
using NLog;

namespace IronFoundry.Warden.IISHost
{
    internal static class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static readonly ManualResetEvent exitLatch = new ManualResetEvent(false);
        private static readonly FileSystemWatcher fileSystemWatcher;

        static Program()
        {
            string workingDirectory =  Directory.GetCurrentDirectory();
            fileSystemWatcher = new FileSystemWatcher(workingDirectory);
            fileSystemWatcher.Created += fileSystemWatcher_Created;
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private static void fileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                string lcName = e.Name.ToLowerInvariant().Trim();
                if (lcName == "iishost_stop")
                {
                    exitLatch.Set();
                }
            }
        }

        private static void Main(string[] args)
        {
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                exitLatch.Set();
            };

            try
            {
                var options = new Options();
                if (Parser.Default.ParseArguments(args, options))
                {
                    log.Info("Port:{0}", options.Port);
                    log.Info("Webroot:{0}", options.WebRoot);
                    log.Info("Runtime:{0}", options.RuntimeVersion);
                }
                else
                {
                    Environment.Exit(1);
                }

                ConfigSettings settings;
                var configGenerator = new ConfigGenerator(options.WebRoot);
                switch (options.RuntimeVersion)
                {
                    case "2":
                    case "2.0":
                        settings = configGenerator.Create(
                            options.Port,
                            Constants.FrameworkPaths.TwoDotZeroWebConfig,
                            Constants.RuntimeVersion.VersionTwoDotZero,
                            Constants.PipelineMode.Integrated, options.UserName, options.Password);
                        break;
                    default:
                        settings = configGenerator.Create(
                            options.Port,
                            Constants.FrameworkPaths.FourDotZeroWebConfig,
                            Constants.RuntimeVersion.VersionFourDotZero,
                            Constants.PipelineMode.Integrated, options.UserName, options.Password);
                        break;
                }

                log.Info("starting web server instance...");
                using (var webServer = new WebServer(settings))
                {
                    webServer.Start();
                    Console.WriteLine("Server Started.... press CTRL + C to stop");

                    StartInBrowser(options);

                    exitLatch.WaitOne();
                    Console.WriteLine("Server shutting down, please wait...");
                    webServer.Stop();
                }

                if (File.Exists("iishost_stop"))
                {
                    File.Delete("iishost_stop");
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("Error on startup.", ex);
                Environment.Exit(2);
            }
        }

        private static void StartInBrowser(Options options)
        {
            try
            {
                if (Environment.UserInteractive && options.StartInBrowser)
                {
                    Process.Start(String.Format("http://localhost:{0}", options.Port));
                }
            }
            catch (Exception ex)
            {
                log.DebugException("Unable to start in browser", ex);
            }
        }
    }

    internal class Options
    {
        private string webRoot;

        internal Options()
        {
            this.webRoot = Directory.GetCurrentDirectory();
        }

        [Option('p', "port", Required = true, HelpText = "The port for the IIS website.")]
        public uint Port { get; set; }

        [Option('r', "webroot", Required = false, HelpText = "The local webroot path for website.")]
        public string WebRoot
        {
            get { return webRoot; }
            set { webRoot = value; }
        }

        [Option('v', "runtimeVersion", Required = false, DefaultValue = "4.0", HelpText = "AppPool runtime version: 2.0 or 4.0")]
        public string RuntimeVersion { get; set; }

        [Option('b', "startInBrowser", Required = false, DefaultValue = false, HelpText = "Specify true to start a browser pointing to the site.")]
        public bool StartInBrowser { get; set; }

        [Option('u', "username", Required = false, HelpText = "Application pool user name.")]
        public string UserName { get; set; }

        [Option('w', "password", Required = false, HelpText = "Application pool user password.")]
        public string Password { get; set; }

        [HelpOption]
        public string Usage()
        {
            return HelpText.AutoBuild(this, c => HelpText.DefaultParsingErrorsHandler(this, c));
        }
    }
}
