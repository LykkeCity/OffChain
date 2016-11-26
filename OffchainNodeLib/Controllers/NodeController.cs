using Lykke.OffchainNodeLib;
using Lykke.OffchainNodeLib.RPC;
using NBitcoin;
using NBitcoin.OpenAsset;
using OffchainNodeLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace OffchainNodeLib.Controllers
{
    public class NodeController : ApiController
    {
        // curl -H "Content-Type: application/json" -X POST -d "{\"MyPublicKey\":\"xxx\",\"Network\":\"xxx\",\"RandomMessage\":\"xxx\",\"RandomMessageSignature\":\"xxx\"}" http://localhost:8787/Node/Hello
        [System.Web.Http.HttpPost]
        public IHttpActionResult Hello(HelloContract wallet)
        {
            return Ok();
        }

        private double GetMatchingAssetAmountForCounterParty(string assetId, double amount)
        {
            if (ChannelContributions == null || !ChannelContributions.ContainsKey(assetId))
            {
                throw new OffchainException(string.Format("Could not find the contributed amount for asset: {0}", assetId));
            }
            return ChannelContributions[assetId];
        }

        public static IDictionary<string, double> ChannelContributions = null;

        public static Channel GetChannelFromDbForNegociation(AssetLightningEntities entities, Guid channelId, bool throwExceptionOnNonExistance)
        {
            var channel = Control.GetChannelFromDB(entities, channelId, throwExceptionOnNonExistance);

            if (channel != null && (channel.IsNegociationComplete ?? false))
            {
                throw new OffchainException("Channel negociation for this channel has completed.");
            }
            else
            {
                return channel;
            }
        }

        public async Task<IHttpActionResult> SetupFundingRequest(SetupFundingRequest request)
        {
            Guid channelId = Guid.Parse(request.ChannelId);
            AcceptDeny isOK = AcceptDeny.Accept;
            string denyResult = null;

            try
            {
                using (AssetLightningEntities entities = new AssetLightningEntities(Control.DBConnectionString))
                {
                    var channel = GetChannelFromDbForNegociation(entities, channelId, false);
                    if (!IsCounterpartyOutputFineForTransactionBuilding(request.OutputTransactionHash, request.OutputTransactionNumber,
                        channel.Asset, channel.PeerContributedAmount ?? -1))
                    {
                        isOK = AcceptDeny.Deny;
                        denyResult = string.Format("The output number: {0} from transaction {1} is not the acceptable for asset {2} and value {3}.",
                            request.OutputTransactionNumber, request.OutputTransactionHash, channel.Asset, channel.PeerContributedAmount);
                    }
                    else
                    {
                        var txHex = await OpenAssetsHelper.GetTransactionHex(request.OutputTransactionHash, Control.RPCUsername, Control.RPCPassword,
                            Control.RPCServerIpAddress, Control.RPCServerPort);
                        if (txHex.Item1)
                        {
                            isOK = AcceptDeny.Deny;
                            denyResult = txHex.Item2;
                        }
                        else
                        {
                            Coin peerBearerCoin = new Coin(new Transaction(txHex.Item3), (uint)request.OutputTransactionNumber);
                            var peerColoredCoin = new ColoredCoin(new AssetMoney(new AssetId(new BitcoinAssetId(channel.Asset)),
                                (long)channel.PeerContributedAmount), peerBearerCoin);

                            var outputForChannel = (from output in entities.ChannelCreationInputs
                                                    where output.assetId == channel.Asset && output.valueWithoutDivisibility == channel.ContributedAmount
                                                    select output).FirstOrDefault();
                            if (outputForChannel != null)
                            {
                                txHex = await OpenAssetsHelper.GetTransactionHex(outputForChannel.transactionHex, Control.RPCUsername, Control.RPCPassword,
                                    Control.RPCServerIpAddress, Control.RPCServerPort);
                                if (txHex.Item1)
                                {
                                    isOK = AcceptDeny.Deny;
                                    denyResult = txHex.Item2;
                                }
                                else
                                {
                                    Coin bearerCoin = new Coin(new Transaction(txHex.Item3), (uint)outputForChannel.outputNumber);
                                    var coloredCoin = new ColoredCoin(new AssetMoney(new AssetId(new BitcoinAssetId(channel.Asset)),
                                        (long)channel.ContributedAmount), bearerCoin);

                                    TransactionBuilder builder = new TransactionBuilder();
                                    var tx = builder
                                        .AddCoins(peerColoredCoin)
                                        .AddCoins(coloredCoin)
                                        .SendAsset(Base58Data.GetFromBase58Data(channel.PeerBitcoinAddress) as BitcoinAddress, new AssetMoney(new AssetId(new BitcoinAssetId(channel.Asset)), (long)channel.PeerContributedAmount))
                                        .SendAsset(Base58Data.GetFromBase58Data(channel.BitcoinAddress) as BitcoinAddress, new AssetMoney(new AssetId(new BitcoinAssetId(channel.Asset)), (long)channel.ContributedAmount))
                                        .BuildTransaction(false);
                                }
                            }
                            else
                            {
                                isOK = AcceptDeny.Deny;
                                denyResult = "Can not find a proper output to put into the channel.";
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }

            return Json(new SetupFundingResponse
            {
                ChannelId = request.ChannelId,
                UnsignedTransactionHex = null,
                DenyReason = denyResult,
                IsOK = isOK
            });
        }

        public bool IsCounterpartyOutputFineForTransactionBuilding(string outputTransactionHex, int outputNumber, string AssetId,
            double expectedAssetAmount)
        {
            return true;
        }

        public async Task<IHttpActionResult> NegociateChannelRequest(NegociateRequest request)
        {
            Guid channelId = Guid.Parse(request.ChannelId);

            try
            {
                using (AssetLightningEntities entities = new AssetLightningEntities(Control.DBConnectionString))
                {
                    var channel = GetChannelFromDbForNegociation(entities, channelId, false);
                    var amount = GetMatchingAssetAmountForCounterParty(request.AssetId, request.Amount);
                    if (channel != null)
                    {
                        channel.Asset = request.AssetId;
                        channel.ContributedAmount = amount;
                        channel.PeerContributedAmount = request.Amount;
                    }
                    else
                    {
                        Channel channelToAddToDb = new Channel();
                        channelToAddToDb.Id = channelId;
                        channelToAddToDb.ChannelState = Control.GetChannelState(entities,
                            ChannelStateEnum.Reset);
                        channelToAddToDb.Asset = request.AssetId;
                        channelToAddToDb.ContributedAmount = amount;
                        channelToAddToDb.PeerContributedAmount = request.Amount;

                        var webRequest = ((Microsoft.Owin.OwinContext)Request.Properties["MS_OwinContext"]).Request;
                        channelToAddToDb.Destination = webRequest.RemoteIpAddress + webRequest.RemotePort;

                        entities.Channels.Add(channelToAddToDb);
                    }
                    await entities.SaveChangesAsync();

                    return Json(new NegociateResponse
                    {
                        ChannelId = request.ChannelId,
                        Amount = amount,
                        IsOK = true
                    });
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }
        }

        public async Task<IHttpActionResult> NegociateChannelConfirm(NegociateConfirm request)
        {
            Guid channelId = Guid.Parse(request.ChannelId);

            using (AssetLightningEntities entities = new AssetLightningEntities(Control.DBConnectionString))
            {
                var channel = GetChannelFromDbForNegociation(entities, channelId, true);
                channel.ChannelState = Control.GetChannelState(entities,
                    ChannelStateEnum.NegotiateChannelFinished);
                channel.IsNegociationComplete = true;
                await entities.SaveChangesAsync();
            }
            return Ok();
        }
    }
}
