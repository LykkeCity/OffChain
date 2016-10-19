using System;
using System.Collections.Generic;

namespace OffchainNodeLibNetCore.DbModels
{
    public partial class Channel
    {
        public Guid Id { get; set; }
        public long State { get; set; }
        public string Destination { get; set; }
        public string Asset { get; set; }
        public double? ContributedAmount { get; set; }
        public double? PeerContributedAmount { get; set; }
        public bool? IsNegociationComplete { get; set; }

        public virtual ChannelState StateNavigation { get; set; }
    }
}
