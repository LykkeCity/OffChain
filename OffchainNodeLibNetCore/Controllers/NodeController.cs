using Lykke.OffchainNodeLib;
using OffchainNodeLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OffchainNodeLibNetCore.DbModels;
using OffchainNodeLibNetCore.RPC;

namespace OffchainNodeLib.Controllers
{
    public class NodeController : Controller
    {
        // curl -H "Content-Type: application/json" -X POST -d "{\"MyPublicKey\":\"xxx\",\"Network\":\"xxx\",\"RandomMessage\":\"xxx\",\"RandomMessageSignature\":\"xxx\"}" http://localhost:8787/Node/Hello
        [HttpPost]
        public IActionResult Hello(HelloContract wallet)
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

        public IActionResult NegociateChannelRequest(NegociateRequest request)
        {
            Guid channelId = Guid.Parse(request.ChannelId);
            using (AssetLightningContext entities = new AssetLightningContext(Control.DBConnectionString))
            {
                var channel = (from c in entities.Channel
                               where c.Id.Equals(channelId)
                               select c).FirstOrDefault();
                if (channel != null && (channel.IsNegociationComplete ?? false))
                {
                    throw new OffchainException("Channel negociation for this channel has completed.");
                }
                else
                {
                    var amount = GetMatchingAssetAmountForCounterParty(request.AssetId, request.Amount);
                    if (channel != null)
                    {
                        channel.Asset = request.AssetId;
                        channel.ContributedAmount = amount;
                        channel.PeerContributedAmount = request.Amount;

                    }

                    return Json(new NegociateResponse
                    {
                        ChannelId = request.ChannelId,
                        Amount = amount,
                        IsOK = true
                    });
                }
            }
        }

        public IActionResult NegociateChannelConfirm(NegociateConfirm request)
        {
            return Ok();
        }
    }
}
