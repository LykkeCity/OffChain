using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.OpenAsset;
using System.Net;

namespace Lykke.OffchainNodeLib
{
    public class NodeSettings
    {
        public bool IsListener
        {
            get;
            set;
        }

        public IPAddress HostIpAddress
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }

        public AssetId[] AcceptedAssets
        {
            get;
            set;
        }

        public IDictionary<AssetId, double> AcceptedAssetAmounts
        {
            get;
            set;
        }

        public IDictionary<AssetId, double> ContributedAssetAmounts
        {
            get;
            set;
        }

        public string RestEndPoint
        {
            get;
            set;
        }

        public string RPCRestEndPoint
        {
            get;
            set;
        }

        public string DBConnectionString
        {
            get;
            set;
        }
    }
}
