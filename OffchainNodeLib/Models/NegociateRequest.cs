using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffchainNodeLib.Models
{
    public class NegociateRequest
    {
        public string ChannelId
        {
            get;
            set;
        }

        public string AssetId
        {
            get;
            set;
        } 

        public double Amount
        {
            get;
            set;
        }
    }
}
