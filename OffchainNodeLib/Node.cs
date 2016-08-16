using AustinHarris.JsonRpc;
using NBitcoin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.OffchainNodeLib
{
    public class Node : JsonRpcService
    {
        NodeSettings settings = null;
        public Node(NodeSettings settings)
        {
            this.settings = settings;
        }

        public void Listen()
        {
            if(!settings.IsListener)
            {
                throw new OffchainException("Node is not configured in Listener mode.");
            }

            // must new up an instance of the service so it can be registered to handle requests.
            var _svc = this;

            var rpcResultHandler = new AsyncCallback(
                state =>
                {
                    var async = ((JsonRpcStateAsync)state);
                    var result = async.Result;
                    var writer = ((StreamWriter)async.AsyncState);

                    writer.WriteLine(result);
                    writer.FlushAsync();
                });

            SocketListener.Start(settings.HostIpAddress, settings.Port, (writer, line) =>
            {
                var async = new JsonRpcStateAsync(rpcResultHandler, writer) { JsonRpc = line };
                JsonRpcProcessor.Process(async, writer);
            });
        }

        /*
        private string Hello(string myPublicKey, string network, string randomMessage, string randomMessageSignature)
        {
            HelloResult result = new HelloResult();
            network = network.ToLower();
            if (!network.Equals("main"))
            {
                network = "testnet";
            }

            try
            {
                using (AssetLightningEntities context = new AssetLightningEntities())
                {
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        if (randomMessage.Length > 100)
                        {
                            throw new ArgumentException("randomMessage should be less than 100 characters.", "randomMessage");
                        }
                        PubKey pubkey = new PubKey(myPublicKey);
                        if (pubkey.VerifyMessage(randomMessage, randomMessageSignature))
                        {
                            result.SessionNumber = Guid.NewGuid().ToString();
                        }
                        else
                        {
                            result.Error = "The provided signature did not verify successfuly.";
                        }

                        Session session = new Session();
                        session.CreationDatetime = DateTime.Now;
                        session.SessionId = result.SessionNumber;
                        session.PubKey = myPublicKey;
                        session.Network = network;

                        context.Sessions.Add(session);
                        context.SaveChanges();

                        transaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                result.Error = e.ToString();
            }

            return JsonConvert.SerializeObject(result);
        }
        */
    }
}
