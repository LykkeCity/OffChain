using LightNode.Server;
using OffchainNodeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.OffchainNodeLib.RPC
{
    public class ControlReply
    {
        public string ErrorText
        {
            get;
            set;
        }

        public string Result
        {
            get;
            set;
        }
    }

    public class Channel
    {
        public Guid ChannelId
        {
            get;
            set;
        }

        public ChannelState State
        {
            get;
            set;
        }

        public string ErrorMessage
        {
            get;
            set;
        }

        public string Asset
        {
            get;
            set;
        }

        public string CounterPartyUrl
        {
            get;
            set;
        }
    }

    public class Control : LightNodeContract
    {
        const string ChannelDoesNotExist = "The provided channel id does not exist.";
        const string InvalidGuidProvided = "The provided Guid is invalid.";
        private string CounterPartyUrl
        {
            get;
            set;
        }

        IDictionary<Guid, Channel> Channels
        {
            get;
            set;
        }

        public Control()
        {
            Channels = new Dictionary<Guid, Channel>();
        }

        private bool ChannelShouldBe(Guid channelId, ChannelState channelState)
        {
            if (!Channels.ContainsKey(channelId))
            {
                throw new OffchainException(ChannelDoesNotExist);
            }
            if (Channels[channelId].State != channelState)
            {
                var message = String.Format("Channel should be in {0} state."
                    , channelState.ToString());
                Channels[channelId].ErrorMessage = message;
                return false;
            }
            else
            {
                return true;
            }
        }

        public string CreateNewChannel(string destination)
        {
            var guid = Guid.NewGuid();
            Channels[guid].ChannelId = guid;
            Channels[guid].State = RPC.ChannelState.Reset;
            Channels[guid].ErrorMessage = null;
            return guid.ToString();
        }

        public string ResetChannel(string channelId)
        {
            var guid = ConvertStringToGuid(channelId);

            if (!Channels.ContainsKey(guid))
            {
                throw new OffchainException(ChannelDoesNotExist);
            }

            Channels[guid].State = RPC.ChannelState.Reset;
            return "Channel has been reset.";
        }

        private Guid ConvertStringToGuid(string guidStr)
        {
            Guid guid;
            if (!Guid.TryParse(guidStr, out guid))
            {
                throw new OffchainException(InvalidGuidProvided);
            }
            return guid;
        }

        public async Task<NegotiateChannelResult> NegociateChannel(string channelId, string assetId, double amount)
        {
            var guid = ConvertStringToGuid(channelId);

            if (!Channels.ContainsKey(guid))
            {
                return new NegotiateChannelResult { Error = string .Format( "The channel with specified id: {0} does not exist.", channelId )};
            }
            else
            {
                using (NodeClient client = new NodeClient(Channels[guid].CounterPartyUrl))
                {
                    return await client.NegociateChannel(Channels[guid], assetId, amount);
                }
            }
        }

        public string Echo(string x)
        {
            return x;
        }
    }
}
