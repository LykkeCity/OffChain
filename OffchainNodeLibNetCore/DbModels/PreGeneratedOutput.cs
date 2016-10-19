using System;
using System.Collections.Generic;

namespace OffchainNodeLibNetCore.DbModels
{
    public partial class PreGeneratedOutput
    {
        public string TransactionId { get; set; }
        public int OutputNumber { get; set; }
        public long Amount { get; set; }
        public string PrivateKey { get; set; }
        public int Consumed { get; set; }
        public string Script { get; set; }
        public string AssetId { get; set; }
        public string Address { get; set; }
        public string Network { get; set; }
        public byte[] Version { get; set; }
    }
}
