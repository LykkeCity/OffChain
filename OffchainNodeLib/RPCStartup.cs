using Owin;
using System.Web.Http;
using Microsoft.Owin.Cors;
using Microsoft.AspNetCore.Builder.Extensions;

namespace Lykke.OffchainNodeLib
{
    public class RPCStartup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseLightNode();
        }
    }
}
