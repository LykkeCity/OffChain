using Lykke.OffchainNodeLib;
using Lykke.OffchainNodeLib.RPC;
using Newtonsoft.Json;
using OffchainNodeLib.Controllers;
using OffchainNodeLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OffchainNodeLib
{
    public class NodeClient : IDisposable
    {
        private string CounterPartyUrl
        {
            get;
            set;
        }
        public NodeClient(string counterPartyUrl)
        {
            CounterPartyUrl = "http://" + counterPartyUrl;
        }
        public async Task<NegotiateChannelResult> NegociateChannel(InternalChannel channel,
            string assetId, double amount)
        {
            string error = null;
            try
            {
                using (AssetLightningEntities entities = new AssetLightningEntities(Control.DBConnectionString))
                {
                    var channelFromDB = NodeController.GetChannelFromDbForNegociation(entities, channel.ChannelId, true);

                    using (HttpClient webClient = new HttpClient())
                    {
                        var negociateRequest = new NegociateRequest { ChannelId = channel.ChannelId.ToString(), AssetId = assetId, Amount = amount };
                        var response = await webClient.PostAsJsonAsync(CounterPartyUrl + "/Node/NegociateChannelRequest", negociateRequest);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var deserialize = JsonConvert.DeserializeObject<NegociateResponse>(await response.Content.ReadAsStringAsync());

                            if (deserialize.IsOK)
                            {
                                channelFromDB.Asset = assetId;
                                channelFromDB.ContributedAmount = amount;
                                channelFromDB.PeerContributedAmount = deserialize.Amount;
                                await entities.SaveChangesAsync();

                                response = await webClient.PostAsJsonAsync(CounterPartyUrl + "/Node/NegociateChannelConfirm", negociateRequest);
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    channelFromDB.ChannelState = Control.GetChannelState(entities, ChannelStateEnum.NegotiateChannelFinished);
                                    channelFromDB.IsNegociationComplete = true;
                                    await entities.SaveChangesAsync();

                                    return new NegotiateChannelResult { Result = AcceptDeny.Accept };
                                }
                                else
                                {
                                    error = "Could not confirm channel negotiation.";
                                }
                            }
                            else
                            {
                                error = "Negotiation rejected by the peer.";
                            }
                        }
                        else
                        {
                            error = "Could not negotiate channel request.";
                        }

                        return new NegotiateChannelResult { Error = error };
                    }
                }
            }
            catch (Exception e)
            {
                throw new OffchainException("Communication with counterparty failed.", e);
            }
        }

        public void Dispose()
        {
        }
    }
}
