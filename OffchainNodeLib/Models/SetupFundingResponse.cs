using Lykke.OffchainNodeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffchainNodeLib.Models
{
    public class SetupFundingResponse
    {
        public string ChannelId
        {
            get;
            set;
        }

        public string UnsignedTransactionHex
        {
            get;
            set;
        }

        public AcceptDeny IsOK
        {
            get;
            set;
        }

        public string DenyReason
        {
            get;
            set;
        }
    }
}
