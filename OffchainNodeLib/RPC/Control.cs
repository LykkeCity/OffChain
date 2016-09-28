using LightNode.Server;
using OffchainNodeLib;
using System;
using System.Collections.Generic;
using System.Linq;
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



        // http://localhost:8788/Control/CreateNewChannel?destination=10
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

        private static ChannelState GetChannelState(AssetLightningEntities entities, ChannelStateEnum enumState)
        {
            return (from state in entities.ChannelStates where state.StateName.Equals(enumState.ToString())
                    select state).FirstOrDefault();
        }

        // http://localhost:8788/Control/ResetChannel?channelId=1600534B-EFF3-49D6-82AD-2FBCDE5CF88E
        public async Task<string> ResetChannel(string channelId)
        {
            try
            {
                var guid = ConvertStringToGuid(channelId);
                using (AssetLightningEntities entities = new AssetLightningEntities(DBConnectionString))
                {
                    var channel = (from c in entities.Channels
                                   where c.Id.Equals(guid)
                                   select c).FirstOrDefault();

                    if (channel == null)
                    {
                        throw new OffchainException(string.Format("The specified channel with id {0} does not exist.",
                            channelId));
                    }

                    channel.ChannelState = GetChannelState(entities, ChannelStateEnum.Reset);

                    await entities.SaveChangesAsync();
                }
            }
            catch(Exception exp)
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

        public async Task<NegotiateChannelResult> NegociateChannel(string channelId, string assetId, double amount)
        {
            var guid = ConvertStringToGuid(channelId);

            if (!Channels.ContainsKey(guid))
            {
                return new NegotiateChannelResult { Error = string.Format("The channel with specified id: {0} does not exist.", channelId) };
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
