using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffchainNodeLib.Models
{
    public class NegociateResponse
    {
        public string ChannelId
        {
            get;
            set;
        }

        public double Amount
        {
            get;
            set;
        }

        public bool IsOK
        {
            get;
            set;
        }
    }
}
