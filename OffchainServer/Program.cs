using Lykke.OffchainNodeLib;
using Lykke.OffchainNodeLib.RPC;
using OffchainNodeLib.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffchainServer
{
    public class ChannelContribution
    {
        public string Name
        {
            get;
            set;
        }

        public double Value
        {
            get;
            set;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var ControlEndpoint = ConfigurationManager.AppSettings["ControlEndpoint"];
            var CommunicationEndpoint = ConfigurationManager.AppSettings["CommunicationEndpoint"];
            var DBConnectionString = ConfigurationManager.AppSettings["DBConnectionString"];

            Control.RPCUsername = ConfigurationManager.AppSettings["RPCUsername"];
            Control.RPCPassword = ConfigurationManager.AppSettings["RPCPassword"];
            Control.RPCServerIpAddress = ConfigurationManager.AppSettings["RPCServerIpAddress"];
            Control.RPCServerPort = Int32.Parse(ConfigurationManager.AppSettings["RPCServerPort"]);
            Control.FeeAmountInSatoshi = Int32.Parse(ConfigurationManager.AppSettings["FeeAmountInSatoshi"]);

            OpenAssetsHelper.QBitNinjaBaseUrl = ConfigurationManager.AppSettings["QBitNinjaBaseUrl"];

            var ChannelContributionAmount = 
                ConfigurationManager.AppSettings["ChannelContributionAmount"];
            var channelContributionAmounts = Newtonsoft.Json.JsonConvert.DeserializeObject<ChannelContribution[]>(ChannelContributionAmount);
            IDictionary<string, double> contributedAmounts = new Dictionary<string, double>();
            for(int i=0;i<channelContributionAmounts.Length;i++)
            {
                contributedAmounts.Add(channelContributionAmounts[i].Name, channelContributionAmounts[i].Value);
            }
            NodeController.ChannelContributions = contributedAmounts;

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
