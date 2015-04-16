using IronFoundry.Warden.Logging;
using logmessage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Containers
{
    public interface ILogEmitter
    {
        void EmitLogMessage(LogMessageType type, string message);
    }

    public enum LogMessageType
    {
        STDIN = 0,
        STDOUT = 1,
        STDERR = 2,
    }

    public class InstanceLoggingInfo
    {
        public InstanceLoggingInfo ()
        {
            DrainUris = new List<string>();
        }

        public string ApplicationId { get; set; }
        public string InstanceIndex { get; set; }
        public string LoggregatorAddress { get; set; }
        public string LoggregatorSecret { get; set; }
        public List<string> DrainUris { get; private set; }

    }


    public class ContainerLogEmitter : ILogEmitter
    {
        private readonly InstanceLoggingInfo instanceLoggingInfo;
        private readonly LoggregatorEmitter logEmitter;

        public ContainerLogEmitter(InstanceLoggingInfo instanceLoggingInfo)
        {
            this.instanceLoggingInfo = instanceLoggingInfo;

            IPEndPoint endpoint = null;
            if (!TryParseEndpoint(instanceLoggingInfo.LoggregatorAddress, out endpoint))
            {
                throw new ArgumentException("Unable to parse and resolve the supplied Loggregator address");
            }

            logEmitter = new LoggregatorEmitter(endpoint.Address.ToString(), endpoint.Port, instanceLoggingInfo.LoggregatorSecret);
        }

        private bool TryParseEndpoint(string endpoint, out IPEndPoint result)
        {
            Uri url;
            result = null;

            if (Uri.TryCreate(String.Format("http://{0}", endpoint), UriKind.Absolute, out url))
            {
                IPAddress ipAddress;
                try
                {
                    if (!GetIPAddressFromUri(url, out ipAddress)) return false;
                }
                catch (System.Net.Sockets.SocketException)
                {
                    return false;
                }

                result = new IPEndPoint(ipAddress, url.Port);
                return true;
            }

            return false;
        }

        private static bool GetIPAddressFromUri(Uri url, out IPAddress ipAddress)
        {
            if (url.HostNameType == UriHostNameType.Dns)
            {
                var hostIPAddress = Dns.GetHostEntry(url.DnsSafeHost);
                ipAddress = hostIPAddress.AddressList.FirstOrDefault(e => e.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (ipAddress == null)
                    return false;
            }
            else
            {
                ipAddress = IPAddress.Parse(url.Host);
            }
            return true;
        }


        public void EmitLogMessage(LogMessageType type, string data)
        {
            if (String.IsNullOrEmpty(data))
            {
                return;
            }

            var message = new logmessage.LogMessage()
            {
                message_type = ToMessageType(type),
                message = Encoding.ASCII.GetBytes(data),
                app_id = instanceLoggingInfo.ApplicationId,
                source_id = instanceLoggingInfo.InstanceIndex,
                source_name = "App",
            };

            message.drain_urls.AddRange(instanceLoggingInfo.DrainUris);

            var dt = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            message.timestamp = (DateTimeOffset.UtcNow.Ticks - dt.Ticks) * 100;

            logEmitter.EmitLogMessage(message);
        }

        logmessage.LogMessage.MessageType ToMessageType(LogMessageType type)
        {
            switch (type)
            {
                case LogMessageType.STDOUT: return LogMessage.MessageType.OUT;
                case LogMessageType.STDERR: return LogMessage.MessageType.ERR;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }
    }
}
