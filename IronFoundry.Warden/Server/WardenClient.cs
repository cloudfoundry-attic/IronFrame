namespace IronFoundry.Warden.Server
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Containers;
    using Jobs;
    using NLog;
    using Protocol;
    using Utilities;

    public delegate void ClientDisconnectedDelegate(WardenClient client);

    public class WardenClient : IDisposable, IEquatable<WardenClient>
    {
        private static uint clientIdSource = 1;
        private static uint GetNextID()
        {
            return clientIdSource++;
        }

        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly TcpClient tcpClient;
        private readonly IContainerManager containerManager;
        private readonly IJobManager jobManager;
        private readonly CancellationToken cancellationToken;
        private readonly uint id;
        private readonly NetworkStream networkStream;

        private bool isActive;
     
        public WardenClient(TcpClient tcpClient, IContainerManager containerManager, IJobManager jobManager, CancellationToken cancellationToken)
        {
            if (tcpClient == null)
            {
                throw new ArgumentNullException("tcpClient");
            }
            this.tcpClient = tcpClient;
            this.networkStream = tcpClient.GetStream();

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

            this.id = GetNextID();
        }

        public event ClientDisconnectedDelegate ClientDisconnected;

        public bool IsActive
        {
            get { return isActive; }
        }

        public uint ID
        {
            get { return id; }
        }

        public async Task ProcessMessages()
        {
            isActive = true;
            bool connected = true;

            using (tcpClient)
            using (var buffer = new Buffer())
            using (networkStream)
            {
                var byteBuffer = new byte[tcpClient.ReceiveBufferSize];

                while (connected && tcpClient.Connected && !cancellationToken.IsCancellationRequested)
                {
                    Exception exToHandle = null;
                    try
                    {
                        do
                        {
                            int bytes = await networkStream.ReadAsync(byteBuffer, 0, byteBuffer.Length);
                            if (bytes == 0)
                            {
                                // Still there?
                                // ns.Write(Constants.CRLF, 0, 2); 
                                connected = false;
                                break;
                            }
                            else
                            {
                                buffer.Push(byteBuffer, bytes);
                            }
                        }
                        while (networkStream.DataAvailable && !cancellationToken.IsCancellationRequested);

                        if (!connected)
                        {
                            break;
                        }

                        IEnumerable<Message> messages = buffer.GetMessages();
                        if (!messages.IsNullOrEmpty())
                        {
                            var awaitables = new List<Task>();
                            foreach (Message message in messages)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    break;
                                }
                                var messageWriter = new MessageWriter(networkStream);
                                var messageHandler = new MessageHandler(containerManager, jobManager, cancellationToken, messageWriter);
                                awaitables.Add(messageHandler.Handle(message));
                            }
                            await Task.WhenAll(awaitables);
                            log.Trace("Processed '{0}' message tasks.", awaitables.Count);
                        }
                    }
                    catch (Exception exception)
                    {
                        var socketExceptionHandler = new SocketExceptionHandler(exception);
                        bool exceptionHandled = socketExceptionHandler.Handle();
                        if (!exceptionHandled)
                        {
                            exToHandle = exception;
                        }
                    }

                    if (exToHandle != null)
                    {
                        var messageWriter = new MessageWriter(networkStream);
                        var wardenExceptionHandler = new WardenExceptionHandler(log, exToHandle, messageWriter);
                        await wardenExceptionHandler.HandleAsync();
                    }
                }

                MarkAsDisconnected();
            }
        }

        public void Dispose()
        {
            try
            {
                this.networkStream.Close();
                this.networkStream.Dispose();
                this.tcpClient.Close();
            }
            catch (Exception ex)
            {
                log.WarnException(ex);
            }
        }

        public bool Equals(WardenClient other)
        {

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (Object.ReferenceEquals(this, null))
            {
                return false;
            }

            return this.GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as WardenClient);
        }

        public override int GetHashCode()
        {
            return tcpClient.GetHashCode();
        }
     
        private void MarkAsDisconnected()
        {
            isActive = false;
            if (ClientDisconnected != null)
            {
                ClientDisconnected(this);
            }
        }
    }
}
