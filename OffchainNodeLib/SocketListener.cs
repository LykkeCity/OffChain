using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Common.Log;

namespace Lykke.OffchainNodeLib
{
    public class SocketListener
    {
        public static void Start(IPAddress HostIpAddress, int listenPort, Action<StreamWriter, string> handleRequest, ILog logger = null)
        {
            var server = new TcpListener(HostIpAddress, listenPort);
            server.Start();

            if (logger != null)
            {
                logger.WriteInfo("SocketListener", "", "", 
                    string.Format("You can connected with Putty on a (RAW session) to {0} to issue JsonRpc requests.", server.LocalEndpoint), DateTime.Now);
            }

            while (true)
            {
                try
                {
                    using (var client = server.AcceptTcpClient())
                    using (var stream = client.GetStream())
                    {
                        if (logger != null)
                        {
                            logger.WriteInfo("SocketListener", "", "", "Client Connected..", DateTime.Now);
                        }

                        var reader = new StreamReader(stream, Encoding.UTF8);
                        var writer = new StreamWriter(stream, new UTF8Encoding(false));

                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            handleRequest(writer, line);

                            if(logger!=null)
                            {
                                logger.WriteInfo("SocketListener", "", "", string.Format("REQUEST: {0}", line, DateTime.Now));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new OffchainException("Listener failed to start ...", e);
                }
            }
        }
    }
}
