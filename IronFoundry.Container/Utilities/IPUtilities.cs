﻿using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace IronFoundry.Container.Utilities
{
    internal class IPUtilities
    {
        public static ushort RandomFreePort()
        {
            ushort freePort = 0;
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Any, 0);
                listener.Start();
                freePort = Convert.ToUInt16(((IPEndPoint)listener.LocalEndpoint).Port);
            }
            finally
            {
                if (listener != null)
                {
                    listener.Stop();
                }
            }
            return freePort;
        }

        public static IPAddress GetLocalIPAddress()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}
