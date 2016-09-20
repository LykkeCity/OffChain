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

        private double GetMatchingAssetAmountForCounterParty(double amount)
        {
            return 1000000;
        }

        public IHttpActionResult NegociateChannelRequest(NegociateRequest request)
        {
            return Json(new NegociateResponse { ChannelId = request.ChannelId, Amount = GetMatchingAssetAmountForCounterParty(request.Amount), IsOK = true });
        }

        public IHttpActionResult NegociateChannelConfirm(NegociateConfirm request)
        {
            return Ok();
        }
    }
}
