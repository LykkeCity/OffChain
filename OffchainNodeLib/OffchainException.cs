using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.OffchainNodeLib
{
    public class OffchainException : Exception
    {
        public OffchainException()
        {
        }

        public OffchainException(string message) : base(message)
        {
        }

        public OffchainException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected OffchainException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
