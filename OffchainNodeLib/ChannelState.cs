using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.OffchainNodeLib.RPC
{
    public enum ChannelState
    {
        Reset,
        HelloFinished,
        NegotiateChannelFinished,
        CreateBaseTransacionFinished

    }
}
