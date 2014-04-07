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
        void EmitLogMessage(LogMessage.MessageType type, string message);
    }


    public class ContainerLogEmitter : ILogEmitter
    {
        private readonly Messages.InstanceLoggingInfo instanceLoggingInfo;
        private readonly LoggregatorEmitter logEmitter;

        public ContainerLogEmitter(Messages.InstanceLoggingInfo instanceLoggingInfo)
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
            IPAddress v4IPAddress = null;

            Debug.WriteLine("Trying to parse: {0}", endpoint);
            
            if (Uri.TryCreate(String.Format("http://{0}", endpoint), UriKind.Absolute, out url))
            {
                try
                {
                    var hostIPAddress = Dns.GetHostEntry(url.DnsSafeHost);
                    v4IPAddress = hostIPAddress.AddressList.FirstOrDefault(e => e.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (v4IPAddress == null)
                        return false;
                }
                catch (System.Net.Sockets.SocketException)
                {
                    return false;
                }

                result = new IPEndPoint(v4IPAddress, url.Port);
                return true;
            }

            return false;
        }


        public void EmitLogMessage(LogMessage.MessageType type, string data)
        {
            if (data.IsNullOrEmpty())
            {
                return;
            }

            var message = new logmessage.LogMessage()
            {
                message_type = type,
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
    }
}
