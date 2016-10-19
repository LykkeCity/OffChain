using LightNode.Server;
using OffchainNodeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lykke.OffchainNodeLib.RPC
{
    public enum ChannelStateEnum
    {
        Reset,
        HelloFinished,
        NegotiateChannelFinished,
        CreateBaseTransacionFinished
    }
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

    public class InternalChannel
    {
        public Guid ChannelId
        {
            get;
            set;
        }

        public ChannelStateEnum State
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

        public static string DBConnectionString
        {
            get;
            set;
        }
        private string CounterPartyUrl
        {
            get;
            set;
        }

        IDictionary<Guid, InternalChannel> Channels
        {
            get;
            set;
        }

        public Control()
        {
            Channels = new Dictionary<Guid, InternalChannel>();
        }

        private bool ChannelShouldBe(Guid channelId, ChannelStateEnum channelState)
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



        // http://localhost:8788/Control/CreateNewChannel?destination=localhost:9787
        public async Task<string> CreateNewChannel(string destination)
        {
            try
            {
                var guid = Guid.NewGuid();

                using (AssetLightningEntities entities = new AssetLightningEntities(DBConnectionString))
                {
                    Channel channel = new Channel();
                    channel.Id = guid;
                    channel.ChannelState = GetChannelState(entities, ChannelStateEnum.Reset);
                    channel.Destination = destination;

                    entities.Channels.Add(channel);
                    await entities.SaveChangesAsync();
                }
                return guid.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static ChannelState GetChannelState(AssetLightningEntities entities, ChannelStateEnum enumState)
        {
            return (from state in entities.ChannelStates
                    where state.StateName.Equals(enumState.ToString())
                    select state).FirstOrDefault();
        }

        public static Channel GetChannelFromDB(AssetLightningEntities entities, Guid guid, bool throwException = true)
        {
            var channel = (from c in entities.Channels
                           where c.Id.Equals(guid)
                           select c).FirstOrDefault();

            if (throwException && channel == null)
            {
                throw new OffchainException(string.Format("The specified channel with id {0} does not exist.",
                    guid.ToString()));
            }

            return channel;
        }

        // http://localhost:8788/Control/ResetChannel?channelId=1600534B-EFF3-49D6-82AD-2FBCDE5CF88E
        public async Task<string> ResetChannel(string channelId)
        {
            try
            {
                var guid = ConvertStringToGuid(channelId);
                using (AssetLightningEntities entities = new AssetLightningEntities(DBConnectionString))
                {
                    var channel = GetChannelFromDB(entities, guid);
                    channel.ChannelState = GetChannelState(entities, ChannelStateEnum.Reset);

                    await entities.SaveChangesAsync();
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }

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

        // http://localhost:8788/Control/NegociateChannel?channelId=6f57a64f-1c66-4185-b965-72813ffe74de&assetId=TestExchangeUSD&amount=10
        public async Task<NegotiateChannelResult> NegociateChannel(string channelId, string assetId, double amount)
        {
            NegotiateChannelResult result = null;

            var guid = ConvertStringToGuid(channelId);
            using (AssetLightningEntities entities = new AssetLightningEntities(DBConnectionString))
            {
                var channel = GetChannelFromDB(entities, guid);
                using (NodeClient client = new NodeClient(channel.Destination))
                {
                    result = await client.NegociateChannel(new InternalChannel { ChannelId = channel.Id, CounterPartyUrl = channel.Destination, Asset = assetId },
                        assetId, amount);
                }
            }

            return result;
        }

        public string Echo(string x)
        {
            return x;
        }
    }
}
