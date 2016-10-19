using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffchainNodeLib.Models
{
    public class NegociateConfirm
    {
        public string ChannelId
        {
            get;
            set;
        }

        bool IsOK
        {
            get;
            set;
        }
    }
}
