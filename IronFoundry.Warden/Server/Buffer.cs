using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IronFoundry.Warden.Protocol;
using NLog;
using ProtoBuf;

namespace IronFoundry.Warden.Server
{
    public class Buffer : IDisposable
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private const int MemStreamSize = 32768;

        private MemoryStream ms = new MemoryStream(MemStreamSize);

        private bool disposed = false;

        public void Push(byte[] data, int count)
        {
            log.Trace("Buffer.Push({0}, {1}) (ms.Position:{2})", data.GetHashCode(), count, ms.Position);
            ms.Write(data, 0, count);
        }

        public IEnumerable<Message> GetMessages()
        {
            log.Trace("Buffer.GetMessages() START");
            var rv = ParseData();
            log.Trace("Buffer.GetMessages() (rv.Count {0})", rv.Count);
            return rv;
        }

        public void Dispose()
        {
            if (!disposed && ms != null)
            {
                ms.Dispose();
            }
            disposed = true;
        }

        private List<Message> ParseData()
        {
            var messages = new List<Message>();

            int startPos = 0;
            byte[] data = ms.ToArray();
            if (data.Length > 0)
            {
                do
                {
                    int crlf = CrlfIdx(data, startPos);
                    if (crlf < 0)
                    {
                        break;
                    }

                    int count = crlf - startPos;
                    if (!(count > 0))
                    {
                        throw new InvalidOperationException("Expected count > 0!");
                    }

                    string dataLengthStr = Encoding.ASCII.GetString(data, startPos, count);

                    int length = 0, crlfPlus2 = crlf + 2;
                    if (Int32.TryParse(dataLengthStr, out length))
                    {
                        int protocolLength = crlfPlus2 + length + 2;
                        if (data.Length < protocolLength)
                        {
                            break;
                        }
                        Message m = DecodePayload(data, crlfPlus2, length);
                        messages.Add(m);
                        ms.Position = startPos = protocolLength;
                    }
                    else
                    {
                        throw new InvalidOperationException(String.Format("Could not parse '{0}' as Int32!", dataLengthStr));
                    }
                } while (true);
            }

            ResetStream();

            return messages;
        }

        private void ResetStream()
        {
            var tmp = new MemoryStream(MemStreamSize);
            ms.CopyTo(tmp);
            ms.Dispose();
            ms = null;
            ms = tmp;
        }

        private static Message DecodePayload(byte[] data, int startPos, int length)
        {
            using (var str = new MemoryStream(data, startPos, length))
            {
                return Serializer.Deserialize<Message>(str);
            }
        }

        private static int CrlfIdx(byte[] data, int startPos)
        {
            int idx = -1,
                i = startPos,
                j = 0,
                len = data.Length;
            for (; i < len; ++i)
            {
                if (data[i] == Constants.CR)
                {
                    j = i + 1;
                    if (j < len)
                    {
                        if (data[j] == Constants.LF)
                        {
                            idx = i;
                            break;
                        }
                    }
                }
            }
            return idx;
        }
    }
}
