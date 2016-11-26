using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffchainNodeLib.Models
{
    public class SetupFundingRequest
    {
        public string ChannelId
        {
            get;
            set;
        }

        public string OutputTransactionHash
        {
            get;
            set;
        }

        public int OutputTransactionNumber
        {
            get;
            set;
        }
    }
}
