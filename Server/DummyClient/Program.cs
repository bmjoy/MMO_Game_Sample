using System;
using System.Net;
using System.Threading;
using DummyClient.Session;
using ServerCore;

namespace DummyClient
{
    class Program
    {
        static int DummyClientCount { get; } = 500;

        static void Main(string[] args)
		{
            Thread.Sleep(3000);
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            Connector connector = new Connector();

            connector.Connect(endPoint,
                () => SessionManager.Instance.Generate(),
                DummyClientCount);
            
            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}