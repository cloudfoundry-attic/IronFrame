using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Jobs;
using NLog;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Server
{
    public class TcpServer
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly CancellationToken cancellationToken;

        private readonly IContainerManager containerManager;
        private readonly IJobManager jobManager;

        private readonly IPEndPoint endpoint;
        private readonly TcpListener listener;

        private readonly List<WardenClient> wardenClients = new List<WardenClient>();
        private readonly Dictionary<WardenClient, Task> wardenClientProcessMessageTasks = new Dictionary<WardenClient, Task>();

        private Task clientListenTask;

        public TcpServer(IContainerManager containerManager, IJobManager jobManager, uint tcpPort, CancellationToken cancellationToken)
        {
            if (containerManager == null)
            {
                throw new ArgumentNullException("containerManager");
            }
            this.containerManager = containerManager;

            if (jobManager == null)
            {
                throw new ArgumentNullException("jobManager");
            }
            this.jobManager = jobManager;

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }
            this.cancellationToken = cancellationToken;

            if (tcpPort < 1025 || tcpPort > 65535)
            {
                throw new ArgumentOutOfRangeException("tcpPort", tcpPort, "TCP port must be within IANA specifications of 1025 to 65535");
            }

            this.endpoint = new IPEndPoint(IPAddress.Loopback, (int)tcpPort);
            this.listener = new TcpListener(endpoint); // lib/dea/task.rb, 66
            this.listener.Server.NoDelay = true;
        }

        public Exception ClientListenException
        {
            get { return clientListenTask.Exception; }
        }

        public void Run()
        {
            Statics.OnServiceStart();

            listener.Start();

            clientListenTask = ListenForClients();
        }

        private async Task ListenForClients()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                ClientConnected(client);
            }

            log.Debug("Stopping Server.");
            listener.Stop();
        }

        public void ClientConnected(TcpClient client)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var wardenClient = new WardenClient(client, containerManager, jobManager, cancellationToken);
            wardenClient.ClientDisconnected += wardenClient_ClientDisconnected;

            // Save the Resulting task from ReceiveInput as a Task so
            // we can check for any unhandled exceptions that may have occured
            wardenClientProcessMessageTasks.Add(wardenClient, wardenClient.ProcessMessages());

            wardenClients.Add(wardenClient);
            log.Trace("Client {0} Connected", wardenClient.ID);
        }

        private void wardenClient_ClientDisconnected(WardenClient wardenClient)
        {
            try
            {
                wardenClient.ClientDisconnected -= wardenClient_ClientDisconnected;

                Task clientReadTask;
                if (wardenClientProcessMessageTasks.TryGetValue(wardenClient, out clientReadTask))
                {
                    if (clientReadTask.Exception != null)
                    {
                        var flattened = clientReadTask.Exception.Flatten();
                        log.WarnException(String.Format("Client '{0}' exceptions!", wardenClient.ID), flattened);
                    }
                }

                wardenClient.Dispose();
            }
            catch (Exception ex)
            {
                log.ErrorException(ex);
            }

            log.Trace("Client {0} disconnected", wardenClient.ID);
        }
    }
}
