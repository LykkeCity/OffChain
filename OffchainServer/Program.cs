using Lykke.OffchainNodeLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffchainServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var ControlEndpoint = ConfigurationManager.AppSettings["ControlEndpoint"];
            var CommunicationEndpoint = ConfigurationManager.AppSettings["CommunicationEndpoint"];
            var DBConnectionString = ConfigurationManager.AppSettings["DBConnectionString"];

            NodeSettings settings = new NodeSettings { RestEndPoint = CommunicationEndpoint,
                RPCRestEndPoint = ControlEndpoint,
                DBConnectionString = DBConnectionString };
            var node = new Node(settings);
            node.OwinListen();

            Console.WriteLine("Press any key to close.");
            Console.ReadLine();

            node.OwinStopListening();
        }
    }
}
