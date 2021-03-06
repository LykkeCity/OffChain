//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OffchainNodeLib
{
    using System;
    using System.Collections.Generic;
    
    public partial class Channel
    {
        public System.Guid Id { get; set; }
        public long State { get; set; }
        public string Destination { get; set; }
        public string Asset { get; set; }
        public Nullable<double> ContributedAmount { get; set; }
        public Nullable<double> PeerContributedAmount { get; set; }
        public Nullable<bool> IsNegociationComplete { get; set; }
        public string BitcoinAddress { get; set; }
        public string PeerBitcoinAddress { get; set; }
    
        public virtual ChannelState ChannelState { get; set; }
    }
}
