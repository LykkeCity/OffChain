using LightNode.Server;
using OffchainNodeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Newtonsoft.Json;
using NBitcoin.OpenAsset;

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

        public static string RPCServerIpAddress
        {
            get;
            set;
        }

        public static int RPCServerPort
        {
            get;
            set;
        }

        public static string RPCUsername
        {
            get;
            set;
        }

        public static string RPCPassword
        {
            get;
            set;
        }

        public static long FeeAmountInSatoshi
        {
            get;
            set;
        }

        // http://localhost:8788/Control/SetRPCServerProperties?RPCServerIpAddress=xxx&RPCServerPort=xx&RPCUsername=xx&RPCPassword=xx
        public void SetRPCServerProperties(string RPCServerIpAddress, int RPCServerPort, string RPCUsername, string RPCPassword)
        {
            Control.RPCUsername = RPCUsername;
            Control.RPCPassword = RPCPassword;
            Control.RPCServerIpAddress = RPCServerIpAddress;
            Control.RPCServerPort = RPCServerPort;
        }

        public class TransactionCollectionMember
        {
            public string TransactionHash
            {
                get;
                set;
            }

            public int OutputNumber
            {
                get;
                set;
            }

            public long Amount
            {
                get;
                set;
            }

            public string AssetId
            {
                get;
                set;
            }
        }

        // http://localhost:8788/Control/AddChannelCreationOutputsToPersistance?json=%5B%7B%22TransactionHash%22%3A%22d54b0f1bd2ff9dab9c43cf7297ac392423bfdccf7b9e8ad9b9b2356ab597de99%22%2C%22OutputNumber%22%3A1%2C%22Amount%22%3A2000%2C%22AssetId%22%3A%22ocmVkVUHnrdSKASFWuHy6hxqTWFc9vdL9d%22%7D%2C%7B%22TransactionHash%22%3A%22d54b0f1bd2ff9dab9c43cf7297ac392423bfdccf7b9e8ad9b9b2356ab597de99%22%2C%22OutputNumber%22%3A2%2C%22Amount%22%3A2000%2C%22AssetId%22%3A%22ocmVkVUHnrdSKASFWuHy6hxqTWFc9vdL9d%22%7D%2C%7B%22TransactionHash%22%3A%22d54b0f1bd2ff9dab9c43cf7297ac392423bfdccf7b9e8ad9b9b2356ab597de99%22%2C%22OutputNumber%22%3A3%2C%22Amount%22%3A2000%2C%22AssetId%22%3A%22ocmVkVUHnrdSKASFWuHy6hxqTWFc9vdL9d%22%7D%5D
        public async Task AddChannelCreationOutputsToPersistance(string json)
        {
            var inputs = JsonConvert.DeserializeObject<TransactionCollectionMember[]>(json);
            using (AssetLightningEntities entities = new AssetLightningEntities(DBConnectionString))
            {
                if (inputs.Count() > 0)
                {
                    DateTime now = DateTime.UtcNow;
                    foreach (var item in inputs)
                    {
                        entities.ChannelCreationInputs.Add(new ChannelCreationInput
                        { creationDate = now, transactionHex = item.TransactionHash, outputNumber = item.OutputNumber,
                            valueWithoutDivisibility = item.Amount, assetId = item.AssetId });
                    }

                    await entities.SaveChangesAsync();
                }
            }
        }

        // This is an asset transfer and not asset issuance method.
        // http://localhost:8788/Control/GenerateChannelCreationOutputs?sourcePrivateKey=cQyt2zxAS2uV7HJWR9hf16pFDTye8YsGL6hzd9pQzMoo9m24RGoV&destinationAddress=mjTqpZZXM7JoD6SPdhqyZMk936RXZAkYgC&assetId=ocmVkVUHnrdSKASFWuHy6hxqTWFc9vdL9d&count=3&amountWithoutDivisibility=2000
        public async Task<string> GenerateChannelCreationOutputs(string sourcePrivateKey, string destinationAddress,
            string assetId, int count, long amountWithoutDivisibility)
        {
            try
            {
                var secret = Base58Data.GetFromBase58Data(sourcePrivateKey) as BitcoinSecret;
                var destAddress = Base58Data.GetFromBase58Data(destinationAddress) as BitcoinAddress;
                if (count <= 0)
                {
                    throw new OffchainException("Number of generated outputs should be grater than 0.");
                }

                var walletOutputs = await OpenAssetsHelper.GetWalletOutputs(secret.GetAddress().ToWif());
                if (walletOutputs.Item2)
                {
                    throw new OffchainException(walletOutputs.Item3);
                }
                else
                {
                    var coloredUncolored = await OpenAssetsHelper.GetColoredUnColoredCoins(walletOutputs.Item1, assetId,
                        RPCUsername, RPCPassword, RPCServerIpAddress, RPCServerPort);
                    if (coloredUncolored.Item1 == null || coloredUncolored.Item1.Count() == 0)
                    {
                        throw new OffchainException(string.Format("No colored coin for specified asset: {0} is found.", assetId));
                    }
                    if (coloredUncolored.Item2 == null || coloredUncolored.Item2.Count() == 0)
                    {
                        throw new OffchainException("There is no coins for fee payment.");
                    }

                    TransactionBuilder builder = new TransactionBuilder();
                    builder
                        .AddKeys(secret)
                        .AddCoins(coloredUncolored.Item1)
                        .AddCoins(coloredUncolored.Item2);

                    for (int i = 0; i < count; i++)
                    {
                        builder.SendAsset(destAddress, new NBitcoin.OpenAsset.AssetMoney
                            (new NBitcoin.OpenAsset.AssetId(new BitcoinAssetId(assetId)), amountWithoutDivisibility));
                    }
                    builder.SetChange(secret.GetAddress());
                    builder.SendFees(new Money(FeeAmountInSatoshi));

                    var transaction = builder.BuildTransaction(true);

                    UriBuilder uriBuilder = new UriBuilder();
                    uriBuilder.Host = RPCServerIpAddress;
                    uriBuilder.Scheme = "http";
                    uriBuilder.Port = RPCServerPort;
                    var uri = uriBuilder.Uri;

                    RPCClient client = new RPCClient(new System.Net.NetworkCredential(RPCUsername, RPCPassword), uri);
                    await client.SendRawTransactionAsync(transaction);

                    await OpenAssetsHelper.IsTransactionFullyIndexed(transaction,
                        new OpenAssetsHelper.RPCConnectionParams { IpAddress = RPCServerIpAddress, Network = destAddress.Network.ToString(), Username = RPCUsername, Password = RPCPassword }, null);

                    walletOutputs = await OpenAssetsHelper.GetWalletOutputs(destAddress.ToWif());
                    if (walletOutputs.Item2)
                    {
                        throw new OffchainException(walletOutputs.Item3);
                    }
                    else
                    {
                        List<TransactionCollectionMember> transactionCollection =
                            new List<TransactionCollectionMember>();

                        var txHash = transaction.GetHash().ToString();
                        foreach (var item in walletOutputs.Item1)
                        {
                            if (item.GetTransactionHash().Equals(txHash) && item.GetAssetId() == assetId && item.GetAssetValue() == amountWithoutDivisibility)
                            {
                                transactionCollection.Add(new TransactionCollectionMember
                                {
                                    TransactionHash = txHash.ToString(),
                                    OutputNumber = item.GetOutputIndex(),
                                    Amount = amountWithoutDivisibility,
                                    AssetId = assetId
                                });
                            }
                        }

                        return JsonConvert.SerializeObject(transactionCollection.ToArray());
                    }
                }
            }
            catch(Exception exp)
            {
                throw exp;
            }
        }

        public string Echo(string x)
        {
            return x;
        }
    }
}
