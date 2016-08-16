using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.OffchainNodeLib
{
    public enum AcceptDeny
    {
        Accept,
        Deny
    }

    public class ResultBase
    {
        public string Error
        {
            get;
            set;
        }
    }

    public class NegotiateChannelResult : ResultBase
    {
        public AcceptDeny Result
        {
            get;
            set;
        }
    }

    public class GetMessageSignatureResult : ResultBase
    {
        public string Signature
        {
            get;
            set;
        }
    }

    public class GenerateNewPrivateKeyResult : ResultBase
    {
        public string PublicKey
        {
            get;
            set;
        }

        public string PrivateKey
        {
            get;
            set;
        }
    }

    public class HelloResult : ResultBase
    {
        public string SessionNumber
        {
            get;
            set;
        }
    }

    public class CreateBaseTransacionResult : ResultBase
    {
        public string TransactionHex
        {
            get;
            set;
        }
    }

    public class FundingTransactionResult : ResultBase
    {

    }
}
