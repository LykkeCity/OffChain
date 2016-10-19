using Lykke.OffchainNodeLib;
using Lykke.OffchainNodeLib.RPC;
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
            catch(Exception exp)
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
