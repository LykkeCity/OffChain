using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffchainNodeLib.Models
{
    public class HelloContract
    {
        public string MyPublicKey
        {
            get;
            set;
        }

        public string Network
        {
            get;
            set;
        }
        
        public string RandomMessage
        {
            get;
            set;
        }

        public string RandomMessageSignature
        {
            get;
            set;
        }
    }
}
