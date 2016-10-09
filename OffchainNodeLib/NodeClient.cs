using Lykke.OffchainNodeLib;
using Lykke.OffchainNodeLib.RPC;
using Newtonsoft.Json;
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
        public async Task<NegotiateChannelResult> NegociateChannel(Lykke.OffchainNodeLib.RPC.InternalChannel channel,
            string assetId, double amount)
        {
            string error = null;
            try
            {
                using (HttpClient webClient = new HttpClient())
                {
                    var negociateRequest = new NegociateRequest { ChannelId = channel.ChannelId.ToString(), AssetId = assetId, Amount = amount };
                    var response = await webClient.PostAsJsonAsync(CounterPartyUrl + "/Node/NegociateChannelRequest", negociateRequest);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var deserialize = JsonConvert.DeserializeObject<NegociateResponse>(await response.Content.ReadAsStringAsync());

                        response = await webClient.PostAsJsonAsync(CounterPartyUrl + "/Node/NegociateChannelConfirm", negociateRequest);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return new NegotiateChannelResult { Result = AcceptDeny.Accept };
                        }
                        else
                        {
                            error = "Could not confirm channel negotiation.";
                        }
                    }
                    else
                    {
                        error = "Could not negotiate channel request.";
                    }

                    return new NegotiateChannelResult { Error = error };
                }
            }
            catch (Exception e)
            {
                throw new OffchainException("Communication with counterpart failed.", e);
            }
        }

        public void Dispose()
        {
        }
    }
}
