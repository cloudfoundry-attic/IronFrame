using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using logmessage;
using ProtoBuf;

namespace IronFoundry.Warden.Logging
{
    public class LoggregatorEmitter
    {
        private readonly string loggeratorAddress;
        private readonly int port;
        private readonly string secret;

        public LoggregatorEmitter(string loggeratorAddress, int port, string secret)
        {
            this.loggeratorAddress = loggeratorAddress;
            this.port = port;
            this.secret = secret;
        }

        public void EmitLogMessage(LogMessage logMessage)
        {
            using (var client = new UdpClient())
            {
                client.Connect(loggeratorAddress, port);

                var envelope = new LogEnvelope
                {
                    log_message = logMessage,
                    routing_key = logMessage.app_id,
                    signature = Encrypt(secret, DigestBytes(logMessage.message))
                };

                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(stream, envelope);
                    stream.Flush();
                    byte[] writebuffer = stream.ToArray();
                    client.Send(writebuffer, writebuffer.Length);
                }
            }
        }

        private static byte[] Encrypt(string secret, byte[] digest)
        {
            // this is a marker for them to know where the message is vs the padded zeros
            byte[] paddedMessage = digest.Concat(new[] { (byte)0x80 }).ToArray();

            using (var aes = Aes.Create())
            {
                aes.Key = GetEncryptionKey(secret);
                aes.GenerateIV();
                aes.Padding = PaddingMode.Zeros;
                aes.Mode = CipherMode.CBC;
                using (var transform = aes.CreateEncryptor())
                {
                    byte[] output = transform.TransformFinalBlock(paddedMessage, 0, paddedMessage.Length);
                    return aes.IV.Concat(output).ToArray();
                }
            }
        }

        private static byte[] GetEncryptionKey(string secret)
        {
            byte[] fullKey = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(secret));
            var key = new byte[16];
            Array.Copy(fullKey, key, 16);
            return key;
        }

        private static byte[] DigestBytes(byte[] messageBytes)
        {
            return SHA256.Create().ComputeHash(messageBytes);
        }
    }
}