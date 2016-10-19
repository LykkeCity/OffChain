using System;
using System.Collections.Generic;

namespace OffchainNodeLibNetCore.DbModels
{
    public partial class Session
    {
        public string SessionId { get; set; }
        public DateTime CreationDatetime { get; set; }
        public string PubKey { get; set; }
        public string Asset { get; set; }
        public double? RequestedAmount { get; set; }
        public double? Tolerance { get; set; }
        public double? ContributedAmount { get; set; }
        public string Network { get; set; }
    }
}
