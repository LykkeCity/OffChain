using System;
using System.Collections.Generic;

namespace OffchainNodeLibNetCore.DbModels
{
    public partial class ChannelState
    {
        public ChannelState()
        {
            Channel = new HashSet<Channel>();
        }

        public long Id { get; set; }
        public string StateName { get; set; }

        public virtual ICollection<Channel> Channel { get; set; }
    }
}
