using NBitcoin;
using NBitcoin.OpenAsset;
using NBitcoin.RPC;
using OffchainNodeLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lykke.OffchainNodeLib
{
    

    public static class OpenAssetsHelper
    {
        private enum APIProvider
        {
            QBitNinja
        }

        public const uint MinimumRequiredSatoshi = 50000; // 100000000 satoshi is one BTC
        public const uint TransactionSendFeesInSatoshi = 10000;
        public const ulong BTCToSathoshiMultiplicationFactor = 100000000;
        public const uint ConcurrencyRetryCount = 3;
        public const uint NBitcoinColoredCoinOutputInSatoshi = 2730;
        private const APIProvider apiProvider = APIProvider.QBitNinja;
        private const int LocktimeMinutesAllowance = 120;

        public static string QBitNinjaBalanceUrl
        {
            get
            {
                return QBitNinjaBaseUrl + "balances/";
            }
        }

        public static string QBitNinjaTransactionUrl
        {
            get
            {
                return QBitNinjaBaseUrl + "transactions/";
            }
        }

        public static string QBitNinjaBaseUrl
        {
            get;
            set;
        }


        public static string GetAddressFromScriptPubKey(Script scriptPubKey, Network network)
        {
            string address = null;
            if (PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(scriptPubKey))
            {
                address = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey).GetAddress(network).ToWif().ToString();
            }
            else
            {
                if (PayToScriptHashTemplate.Instance.CheckScriptPubKey(scriptPubKey))
                {
                    address = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey).GetAddress(network).ToWif().ToString();
                }
            }

            return address;
        }


        public static async Task<Tuple<ColoredCoin[], Coin[]>> GetColoredUnColoredCoins(UniversalUnspentOutput[] walletOutputs,
            string assetId, string username, string password, string ipAddress, int port)
        {
            var walletAssetOutputs = GetWalletOutputsForAsset(walletOutputs, assetId);
            var walletUncoloredOutputs = GetWalletOutputsUncolored(walletOutputs);
            var walletColoredTransactions = await GetTransactionsHex(walletAssetOutputs, username, password, ipAddress, port);
            var walletUncoloredTransactions = await GetTransactionsHex(walletUncoloredOutputs, username, password, ipAddress, port);
            var walletColoredCoins = GenerateWalletColoredCoins(walletColoredTransactions, walletAssetOutputs, assetId);
            var walletUncoloredCoins = GenerateWalletUnColoredCoins(walletUncoloredTransactions, walletUncoloredOutputs);
            return new Tuple<ColoredCoin[], Coin[]>(walletColoredCoins, walletUncoloredCoins);
        }

        private static ColoredCoin[] GenerateWalletColoredCoins(Transaction[] transactions, UniversalUnspentOutput[] usableOutputs, string assetId)
        {
            ColoredCoin[] coins = new ColoredCoin[transactions.Length];
            for (int i = 0; i < transactions.Length; i++)
            {
                coins[i] = new ColoredCoin(new AssetMoney(new AssetId(new BitcoinAssetId(assetId)), (int)usableOutputs[i].GetAssetAmount()),
                    new Coin(transactions[i], (uint)usableOutputs[i].GetOutputIndex()));
            }
            return coins;
        }

        private static Coin[] GenerateWalletUnColoredCoins(Transaction[] transactions, UniversalUnspentOutput[] usableOutputs)
        {
            Coin[] coins = new Coin[transactions.Length];
            for (int i = 0; i < transactions.Length; i++)
            {
                coins[i] = new Coin(transactions[i], (uint)usableOutputs[i].GetOutputIndex());
            }
            return coins;
        }

        private static async Task<Transaction[]> GetTransactionsHex(UniversalUnspentOutput[] outputList, 
            string username, string password, string ipAddress, int port)
        {
            Transaction[] walletTransactions = new Transaction[outputList.Length];
            for (int i = 0; i < walletTransactions.Length; i++)
            {
                var ret = await GetTransactionHex(outputList[i].GetTransactionHash(), username, password, ipAddress, port);
                if (!ret.Item1)
                {
                    walletTransactions[i] = new Transaction(ret.Item3);
                }
                else
                {
                    throw new Exception("Could not get the transaction hex for the transaction with id: "
                        + outputList[i].GetTransactionHash() + " . The exact error message is " + ret.Item2);
                }
            }
            return walletTransactions;
        }

        public static UniversalUnspentOutput[] GetWalletOutputsUncolored(UniversalUnspentOutput[] input)
        {
            IList<UniversalUnspentOutput> outputs = new List<UniversalUnspentOutput>();
            foreach (var item in input)
            {
                if (item.GetAssetId() == null)
                {
                    outputs.Add(item);
                }
            }

            return outputs.ToArray();
        }

        public static UniversalUnspentOutput[] GetWalletOutputsForAsset(UniversalUnspentOutput[] input, string assetId)
        {
            IList<UniversalUnspentOutput> outputs = new List<UniversalUnspentOutput>();
            if (assetId != null)
            {
                foreach (var item in input)
                {
                    if (item.GetAssetId() == assetId)
                    {
                        outputs.Add(item);
                    }
                }
            }

            return outputs.ToArray();
        }

        public static async Task<Tuple<UniversalUnspentOutput[], bool, string>> GetWalletOutputs(string walletAddress)
        {
            Tuple<UniversalUnspentOutput[], bool, string> ret = null;
            switch (apiProvider)
            {

                case APIProvider.QBitNinja:
                    var qbitResult = await GetWalletOutputsQBitNinja(walletAddress);
                    ret = new Tuple<UniversalUnspentOutput[], bool, string>(qbitResult.Item1 != null ? qbitResult.Item1.Select(c => (UniversalUnspentOutput)c).ToArray() : null,
                        qbitResult.Item2, qbitResult.Item3);
                    break;
                default:
                    throw new Exception("Not supported.");
            }
            return ret;
        }

        private static async Task<Tuple<QBitNinjaUnspentOutput[], bool, string>> GetWalletOutputsQBitNinja(string walletAddress)
        {
            bool errorOccured = false;
            string errorMessage = string.Empty;
            IList<QBitNinjaUnspentOutput> unspentOutputsList = new List<QBitNinjaUnspentOutput>();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = null;
                    url = QBitNinjaBalanceUrl + walletAddress;
                    HttpResponseMessage result = await client.GetAsync(url + "?unspentonly=true&colored=true");
                    if (!result.IsSuccessStatusCode)
                    {
                        errorOccured = true;
                        errorMessage = result.ReasonPhrase;
                    }
                    else
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        var notProcessedUnspentOutputs = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaOutputResponse>
                            (webResponse);
                        if (notProcessedUnspentOutputs.operations != null && notProcessedUnspentOutputs.operations.Count > 0)
                        {
                            notProcessedUnspentOutputs.operations.ForEach((o) =>
                            {
                                var convertResult = o.receivedCoins.Select(c => new QBitNinjaUnspentOutput
                                {
                                    confirmations = o.confirmations,
                                    output_index = c.index,
                                    transaction_hash = c.transactionId,
                                    value = c.value,
                                    script_hex = c.scriptPubKey,
                                    asset_id = c.assetId,
                                    asset_quantity = c.quantity
                                });
                                ((List<QBitNinjaUnspentOutput>)unspentOutputsList).AddRange(convertResult);
                            });
                        }
                        else
                        {
                            errorOccured = true;
                            errorMessage = "No coins to retrieve.";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }

            return new Tuple<QBitNinjaUnspentOutput[], bool, string>(unspentOutputsList.ToArray(), errorOccured, errorMessage);
        }

        public static async Task<Tuple<float, float, bool, string>> GetAccountBalance(string walletAddress,
            string assetId, Network network)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return await GetAccountBalanceQBitNinja(walletAddress, assetId, network);
                default:
                    throw new Exception("Not supported.");
            }
        }

        // ToDo: confirmation number is set to be 1
        public static async Task<Tuple<float, float, bool, string>> GetAccountBalanceQBitNinja(string walletAddress,
            string assetId, Network network)
        {
            float balance = 0;
            float unconfirmedBalance = 0;
            bool errorOccured = false;
            string errorMessage = "";
            string url;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    url = QBitNinjaBalanceUrl + walletAddress;
                    HttpResponseMessage result = await client.GetAsync(url + "?unspentonly=true&colored=true");
                    if (!result.IsSuccessStatusCode)
                    {
                        return new Tuple<float, float, bool, string>(0, 0, true, result.ReasonPhrase);
                    }
                    else
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        QBitNinjaOutputResponse response = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaOutputResponse>
                            (webResponse);
                        if (response.operations != null && response.operations.Count > 0)
                        {
                            foreach (var item in response.operations)
                            {
                                response.operations.ForEach((o) =>
                                {
                                    balance += o.receivedCoins.Where(c => !string.IsNullOrEmpty(c.assetId) && c.assetId.Equals(assetId) && o.confirmations > 0).Select(c => c.quantity).Sum();
                                    unconfirmedBalance += o.receivedCoins.Where(c => !string.IsNullOrEmpty(c.assetId) && c.assetId.Equals(assetId) && o.confirmations == 0).Select(c => c.quantity).Sum();
                                });
                            }
                        }
                        else
                        {
                            errorOccured = true;
                            errorMessage = "No coins found.";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }
            return new Tuple<float, float, bool, string>(balance, unconfirmedBalance, errorOccured, errorMessage);
        }

        public static long GetValue(this UniversalUnspentOutput output)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)output).value;
                default:
                    throw new Exception("Not supported.");
            }
        }

        public static long GetAssetValue(this UniversalUnspentOutput output)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)output).asset_quantity;
                default:
                    throw new Exception("Not supported.");
            }
        }

        public static string GetTransactionHash(this UniversalUnspentOutput output)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)output).transaction_hash;
                default:
                    throw new Exception("Not supported.");
            }
        }

        public static string GetScriptHex(this UniversalUnspentOutput output)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)output).script_hex;
                default:
                    throw new Exception("Not supported.");
            }
        }

        public static int GetOutputIndex(this UniversalUnspentOutput output)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)output).output_index;
                default:
                    throw new Exception("Not supported.");
            }
        }

        public static string GetAssetId(this UniversalUnspentOutput item)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)item).asset_id;
                default:
                    throw new Exception("Not supported.");
            }
        }

        private static int GetConfirmationNumber(this UniversalUnspentOutput item)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)item).confirmations;
                default:
                    throw new Exception("Not supported.");
            }
        }

        private static long GetAssetAmount(this UniversalUnspentOutput item)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)item).asset_quantity;
                default:
                    throw new Exception("Not supported.");
            }
        }

        private static long GetBitcoinAmount(this UniversalUnspentOutput item)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)item).value;
                default:
                    throw new Exception("Not supported.");
            }
        }

        // ToDo - Clear confirmation number
        public static float GetAssetBalance(UniversalUnspentOutput[] outputs,
            string assetId, long multiplyFactor, bool includeUnconfirmed = false)
        {
            float total = 0;
            foreach (var item in outputs)
            {
                if ((item.GetAssetId() != null && item.GetAssetId().Equals(assetId))
                    || (item.GetAssetId() == null && assetId.Trim().ToUpper().Equals("BTC")))
                {
                    if (item.GetConfirmationNumber() == 0)
                    {
                        if (includeUnconfirmed)
                        {
                            if (item.GetAssetId() != null)
                            {
                                total += (float)item.GetAssetAmount();
                            }
                            else
                            {
                                total += item.GetBitcoinAmount();
                            }
                        }
                    }
                    else
                    {
                        if (item.GetAssetId() != null)
                        {
                            total += (float)item.GetAssetAmount();
                        }
                        else
                        {
                            total += item.GetBitcoinAmount();
                        }
                    }
                }
            }

            return total / multiplyFactor;
        }

        public static bool IsAssetsEnough(UniversalUnspentOutput[] outputs,
            string assetId, float assetAmount, long multiplyFactor, bool includeUnconfirmed = false)
        {
            if (!string.IsNullOrEmpty(assetId))
            {
                float total = GetAssetBalance(outputs, assetId, multiplyFactor, includeUnconfirmed);
                if (total >= assetAmount)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        // ToDo - Clear confirmation number
        public static bool IsBitcoinsEnough(UniversalUnspentOutput[] outputs,
            long amountInSatoshi, bool includeUnconfirmed = false)
        {
            long total = 0;
            foreach (var item in outputs)
            {
                if (item.GetConfirmationNumber() == 0)
                {
                    if (includeUnconfirmed)
                    {
                        total += item.GetBitcoinAmount();
                    }
                }
                else
                {
                    total += item.GetBitcoinAmount();
                }
            }

            if (total >= amountInSatoshi)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks whether the amount for assetId of the wallet is enough
        /// </summary>
        /// <param name="walletAddress">Address of the wallet</param>
        /// <param name="assetId">Asset id to check the balance for.</param>
        /// <param name="amount">The required amount to check for.</param>
        /// <returns>Whether the asset amount is enough or not.</returns>
        /// ToDo - Figure out a method for unconfirmed balance
        public static async Task<bool> IsAssetsEnough(string walletAddress, string assetId,
            int amount, Network network, long multiplyFactor, bool includeUnconfirmed = false)
        {
            Tuple<float, float, bool, string> result = await GetAccountBalance(walletAddress, assetId, network);
            if (result.Item3 == true)
            {
                return false;
            }
            else
            {
                if (!includeUnconfirmed)
                {
                    if (result.Item1 >= amount)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (result.Item1 + result.Item2 >= amount)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public class RPCConnectionParams
        {
            public string Username
            {
                get;
                set;
            }

            public string Password
            {
                get;
                set;
            }

            public string IpAddress
            {
                get;
                set;
            }

            public string Network
            {
                get;
                set;
            }

            public Network BitcoinNetwork
            {
                get
                {
                    switch (Network.ToLower())
                    {
                        case "main":
                            return NBitcoin.Network.Main;
                        case "testnet":
                            return NBitcoin.Network.TestNet;
                        default:
                            throw new NotImplementedException(string.Format("Bitcoin network {0} is not supported.", Network));
                    }
                }
            }
        }

        public static async Task<bool> HasBalanceIndexed(string qbitNinjaBaseUrl, string txId, string btcAddress)
        {
            return await HasBalanceIndexedInternal(qbitNinjaBaseUrl, txId, btcAddress);
        }

        public static async Task<bool> HasBalanceIndexedZeroConfirmation(string qbitNinjaBaseUrl, string txId, string btcAddress)
        {
            return await HasBalanceIndexedInternal(qbitNinjaBaseUrl, txId, btcAddress, false);
        }

        public static async Task<bool> HasBalanceIndexedInternal(string qbitNinjaBaseUrl, string txId, string btcAddress,
            bool confirmationRequired = true)
        {
            HttpResponseMessage result = null;
            bool exists = false;
            using (HttpClient client = new HttpClient())
            {
                string url = null;
                exists = false;
                url = qbitNinjaBaseUrl + "balances/" + btcAddress + "?unspentonly=true&colored=true";
                result = await client.GetAsync(url);
            }

            if (!result.IsSuccessStatusCode)
            {
                return false;
            }
            else
            {
                var webResponse = await result.Content.ReadAsStringAsync();
                var notProcessedUnspentOutputs = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaOutputResponse>
                    (webResponse);
                if (notProcessedUnspentOutputs.operations != null && notProcessedUnspentOutputs.operations.Count > 0)
                {
                    notProcessedUnspentOutputs.operations.ForEach((o) =>
                    {
                        exists = o.receivedCoins
                       .Where(c => c.transactionId.Equals(txId) && (!confirmationRequired | o.confirmations > 0))
                       .Any() | exists;
                        if (exists)
                        {
                            return;
                        }
                    });
                }

                return exists;
            }
        }

        public static async Task<bool> IsUrlSuccessful(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage result = await client.GetAsync(url);
                if (!result.IsSuccessStatusCode)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public static async Task<bool> HasTransactionIndexed(string qbitNinjaBaseUrl, string txId, string dummy)
        {
            string url = qbitNinjaBaseUrl + "transactions/" + txId + "?colored=true";
            return await IsUrlSuccessful(url);
        }

        public static async Task<bool> HasBlockIndexed(string qbitNinjaBaseUrl, string blockId, string dummy)
        {
            string url = qbitNinjaBaseUrl + "blocks/" + blockId + "?headeronly=true";
            return await IsUrlSuccessful(url);
        }

        public static async Task WaitUntillQBitNinjaHasIndexed(string qbitNinjaBaseUrl,
            Func<string, string, string, Task<bool>> checkIndexed, IEnumerable<string> ids, string id2 = null,
            AssetLightningEntities entities = null)
        {
            var indexed = false;
            foreach (var id in ids)
            {
                indexed = false;
                for (int i = 0; i < 30; i++)
                {
                    bool result = false;
                    try
                    {
                        result = await checkIndexed(qbitNinjaBaseUrl, id, id2);
                    }
                    catch (Exception exp)
                    {
                        if (entities != null)
                        {
                            // await GeneralLogger.WriteError("OpenAssetsHelper", string.Empty, string.Empty, exp, null, entities);
                        }
                    }

                    if (result)
                    {
                        indexed = true;
                        break;
                    }
                    await Task.Delay(1000);
                }

                if (!indexed)
                {
                    throw new Exception(string.IsNullOrEmpty(id2) ? string.Format("Item with id: {0} did not get indexed yet.", id) : string.Format("Item with id: {0} did not get indexed yet. Provided id2 is {1}", id, id2));
                }
            }
        }

        public static async Task IsTransactionFullyIndexed(Transaction tx, RPCConnectionParams connectionParams,
            AssetLightningEntities entities, bool confirmationRequired = false)
        {
            try
            {
                await WaitUntillQBitNinjaHasIndexed(OpenAssetsHelper.QBitNinjaBaseUrl, HasTransactionIndexed,
                    new string[] { tx.GetHash().ToString() }, null, entities);
            }
            catch (Exception)
            {
            }

            var destAddresses = tx.Outputs.Select(o => o.ScriptPubKey.GetDestinationAddress(connectionParams.BitcoinNetwork)?.ToWif()).Where(c => !string.IsNullOrEmpty(c)).Distinct();
            foreach (var addr in destAddresses)
            {
                try
                {
                    if (confirmationRequired)
                    {
                        await WaitUntillQBitNinjaHasIndexed(OpenAssetsHelper.QBitNinjaBaseUrl, HasBalanceIndexed,
                            new string[] { tx.GetHash().ToString() }, addr, entities);
                    }
                    else
                    {
                        await WaitUntillQBitNinjaHasIndexed(OpenAssetsHelper.QBitNinjaBaseUrl, HasBalanceIndexedZeroConfirmation,
                            new string[] { tx.GetHash().ToString() }, addr, entities);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        // The returned object is a Tuple with first parameter specifing if an error has occured,
        // second the error message and third the transaction hex
        public static async Task<Tuple<bool, string, string>> GetTransactionHex(string transactionId,
            string username, string password, string ipAddress, int port)
        {
            string transactionHex = "";
            bool errorOccured = false;
            string errorMessage = "";
            try
            {
                UriBuilder builder = new UriBuilder();
                builder.Host = ipAddress;
                builder.Scheme = "http";
                builder.Port = port;
                var uri = builder.Uri;

                RPCClient client = new RPCClient(new System.Net.NetworkCredential(username, password), uri);
                transactionHex = (await client.GetRawTransactionAsync(uint256.Parse(transactionId), true)).ToHex();
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }
            return new Tuple<bool, string, string>(errorOccured, errorMessage, transactionHex);
        }

        public class Asset
        {
            public string AssetId
            {
                get;
                set;
            }

            public BitcoinAddress AssetAddress
            {
                get;
                set;
            }

            public long AssetMultiplicationFactor
            {
                get;
                set;
            }

            public string AssetDefinitionUrl { get; set; }

            public string AssetPrivateKey { get; set; }
        }

        public partial class KeyStorage
        {
            public string WalletAddress { get; set; }
            public string WalletPrivateKey { get; set; }
            public string MultiSigAddress { get; set; }
            public string MultiSigScript { get; set; }
            public string ExchangePrivateKey { get; set; }
            public string Network { get; set; }
            public byte[] Version { get; set; }
        }

        public class Error
        {
            public ErrorCode Code { get; set; }
            public string Message { get; set; }
        }

        public enum ErrorCode
        {
            Exception,
            ProblemInRetrivingWalletOutput,
            ProblemInRetrivingTransaction,
            NotEnoughBitcoinAvailable,
            NotEnoughAssetAvailable,
            PossibleDoubleSpend,
            AssetNotFound,
            TransactionNotSignedProperly,
            BadInputParameter,
            PersistantConcurrencyProblem,
            NoCoinsToRefund,
            NoCoinsFound,
            InvalidAddress
        }



        public class AssetDefinition
        {
            public string AssetId { get; set; }
            public string AssetAddress { get; set; }
            public string Name { get; set; }
            public string PrivateKey { get; set; }
            public string DefinitionUrl { get; set; }
            public int Divisibility { get; set; }
            public long MultiplyFactor
            {
                get
                {
                    return (long)Math.Pow(10, Divisibility);
                }
            }
        }

        public class GetCoinsForWalletReturnType
        {
            public Error Error
            {
                get;
                set;
            }



            public KeyStorage MatchingAddress
            {
                get;
                set;
            }

            public Asset Asset { get; set; }
        }

        public class GetScriptCoinsForWalletReturnType : GetCoinsForWalletReturnType
        {
            public ColoredCoin[] AssetScriptCoins
            {
                get;
                set;
            }

            public ScriptCoin[] ScriptCoins
            {
                get;
                set;
            }
        }

        public class GetOrdinaryCoinsForWalletReturnType : GetCoinsForWalletReturnType
        {
            public ColoredCoin[] AssetCoins
            {
                get;
                set;
            }

            public Coin[] Coins
            {
                get;
                set;
            }
        }

        public static bool IsRealAsset(string asset)
        {
            if (asset != null && asset.Trim().ToUpper() != "BTC")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<GetCoinsForWalletReturnType> GetCoinsForWallet
            (string multiSigAddress, long requiredSatoshiAmount, float requiredAssetAmount, string asset, AssetDefinition[] assets,
            Network network, string username, string password, string ipAddress, int port, string connectionString, bool isOrdinaryReturnTypeRequired, bool isAddressMultiSig = false)
        {
            GetCoinsForWalletReturnType ret;
            if (isOrdinaryReturnTypeRequired)
            {
                ret = new GetOrdinaryCoinsForWalletReturnType();
            }
            else
            {
                ret = new GetScriptCoinsForWalletReturnType();
            }

            try
            {
                if (isAddressMultiSig)
                {
                    // ret.MatchingAddress = await GetMatchingMultisigAddress(multiSigAddress);
                }

                // Getting wallet outputs
                var walletOutputs = await GetWalletOutputs
                    (multiSigAddress);
                if (walletOutputs.Item2)
                {
                    ret.Error = new Error();
                    ret.Error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                    ret.Error.Message = walletOutputs.Item3;
                }
                else
                {
                    // Getting bitcoin outputs to provide the transaction fee
                    var bitcoinOutputs = GetWalletOutputsUncolored(walletOutputs.Item1);
                    if (!IsBitcoinsEnough(bitcoinOutputs, requiredSatoshiAmount))
                    {
                        ret.Error = new Error();
                        ret.Error.Code = ErrorCode.NotEnoughBitcoinAvailable;
                        ret.Error.Message = "The required amount of satoshis to send transaction is " + requiredSatoshiAmount +
                            " . The address is: " + multiSigAddress;
                    }
                    else
                    {
                        UniversalUnspentOutput[] assetOutputs = null;

                        if (IsRealAsset(asset))
                        {
                            ret.Asset = GetAssetFromName(assets, asset, network);
                            if (ret.Asset == null)
                            {
                                ret.Error = new Error();
                                ret.Error.Code = ErrorCode.AssetNotFound;
                                ret.Error.Message = "Could not find asset with name: " + asset;
                            }
                            else
                            {
                                // Getting the asset output to provide the assets
                                assetOutputs = GetWalletOutputsForAsset(walletOutputs.Item1, ret.Asset.AssetId);
                            }
                        }
                        if (IsRealAsset(asset) && ret.Asset != null && !IsAssetsEnough(assetOutputs, ret.Asset.AssetId, requiredAssetAmount, ret.Asset.AssetMultiplicationFactor))
                        {
                            ret.Error = new Error();
                            ret.Error.Code = ErrorCode.NotEnoughAssetAvailable;
                            ret.Error.Message = "The required amount of " + asset + " to send transaction is " + requiredAssetAmount +
                                " . The address is: " + multiSigAddress;
                        }
                        else
                        {
                            // Converting bitcoins to script coins so that we could sign the transaction
                            var coins = (await GetColoredUnColoredCoins(bitcoinOutputs, null,
                                username, password, ipAddress, port)).Item2;
                            if (coins.Length != 0)
                            {
                                if (isOrdinaryReturnTypeRequired)
                                {
                                    ((GetOrdinaryCoinsForWalletReturnType)ret).Coins = coins;
                                }
                                else
                                {
                                    ((GetScriptCoinsForWalletReturnType)ret).ScriptCoins = new ScriptCoin[coins.Length];
                                    for (int i = 0; i < coins.Length; i++)
                                    {
                                        ((GetScriptCoinsForWalletReturnType)ret).ScriptCoins[i] = new ScriptCoin(coins[i], new Script(ret.MatchingAddress.MultiSigScript));
                                    }
                                }
                            }

                            if (IsRealAsset(asset))
                            {
                                // Converting assets to script coins so that we could sign the transaction
                                var assetCoins = ret.Asset != null ? (await GetColoredUnColoredCoins(assetOutputs, ret.Asset.AssetId,
                                username, password, ipAddress, port)).Item1 : new ColoredCoin[0];

                                if (assetCoins.Length != 0)
                                {
                                    if (isOrdinaryReturnTypeRequired)
                                    {
                                        ((GetOrdinaryCoinsForWalletReturnType)ret).AssetCoins = assetCoins;
                                    }
                                    else
                                    {
                                        ((GetScriptCoinsForWalletReturnType)ret).AssetScriptCoins = new ColoredCoin[assetCoins.Length];
                                        for (int i = 0; i < assetCoins.Length; i++)
                                        {
                                            ((GetScriptCoinsForWalletReturnType)ret).AssetScriptCoins[i] = new ColoredCoin(assetCoins[i].Amount,
                                                new ScriptCoin(assetCoins[i].Bearer, new Script(ret.MatchingAddress.MultiSigScript)));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ret.Error = new Error();
                ret.Error.Code = ErrorCode.Exception;
                ret.Error.Message = e.ToString();
            }

            return ret;
        }

        public static Asset GetAssetFromName(AssetDefinition[] assets, string assetName, Network network)
        {
            Asset ret = null;
            foreach (var item in assets)
            {
                if (item.Name == assetName)
                {
                    ret = new Asset();
                    ret.AssetId = item.AssetId;
                    ret.AssetPrivateKey = item.PrivateKey;
                    ret.AssetAddress = (new BitcoinSecret(ret.AssetPrivateKey, network)).PubKey.
                        GetAddress(network);
                    ret.AssetMultiplicationFactor = item.MultiplyFactor;
                    ret.AssetDefinitionUrl = item.DefinitionUrl;
                    break;
                }
            }

            return ret;
        }

        // From: http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static async Task SendTransaction(Transaction tx, string username, string password, string ipAddress, int port)
        {
            UriBuilder builder = new UriBuilder();
            builder.Host = ipAddress;
            builder.Scheme = "http";
            builder.Port = port;
            var uri = builder.Uri;

            RPCClient client = new RPCClient(new System.Net.NetworkCredential(username, password), uri);
            await client.SendRawTransactionAsync(tx);
        }

        public class UniversalUnspentOutput
        {
        }

        public class QBitNinjaUnspentOutput : UniversalUnspentOutput
        {
            public string transaction_hash { get; set; }
            public int output_index { get; set; }
            public long value { get; set; }
            public int confirmations { get; set; }
            public string script_hex { get; set; }
            public string asset_id { get; set; }
            public long asset_quantity { get; set; }
        }

        public class QBitNinjaReceivedCoin
        {
            public string transactionId { get; set; }
            public int index { get; set; }
            public long value { get; set; }
            public string scriptPubKey { get; set; }
            public object redeemScript { get; set; }
            public string assetId { get; set; }
            public long quantity { get; set; }
        }

        public class QBitNinjaSpentCoin
        {
            public string address { get; set; }
            public string transactionId { get; set; }
            public int index { get; set; }
            public long value { get; set; }
            public string scriptPubKey { get; set; }
            public object redeemScript { get; set; }
            public string assetId { get; set; }
            public long quantity { get; set; }
        }

        public class QBitNinjaOperation
        {
            public long amount { get; set; }
            public int confirmations { get; set; }
            public int height { get; set; }
            public string blockId { get; set; }
            public string transactionId { get; set; }
            public List<QBitNinjaReceivedCoin> receivedCoins { get; set; }
            public List<QBitNinjaSpentCoin> spentCoins { get; set; }
        }



        public class QBitNinjaOutputResponse
        {
            public object continuation { get; set; }
            public List<QBitNinjaOperation> operations { get; set; }
        }

        public class QBitNinjaBlock
        {
            public string blockId { get; set; }
            public string blockHeader { get; set; }
            public int height { get; set; }
            public int confirmations { get; set; }
            public string medianTimePast { get; set; }
            public string blockTime { get; set; }
        }

        public class QBitNinjaTransactionResponse
        {
            public string transaction { get; set; }
            public string transactionId { get; set; }
            public bool isCoinbase { get; set; }
            public QBitNinjaBlock block { get; set; }
            public List<QBitNinjaSpentCoin> spentCoins { get; set; }
            public List<QBitNinjaReceivedCoin> receivedCoins { get; set; }
            public string firstSeen { get; set; }
            public int fees { get; set; }
        }

        public class CoinprismUnspentOutput : UniversalUnspentOutput
        {
            public string transaction_hash { get; set; }
            public int output_index { get; set; }
            public long value { get; set; }
            public string asset_id { get; set; }
            public int asset_quantity { get; set; }
            public string[] addresses { get; set; }
            public string script_hex { get; set; }
            public bool spent { get; set; }
            public int confirmations { get; set; }
        }

        private class CoinprismGetBalanceResponse
        {
            public string address { get; set; }
            public string asset_address { get; set; }
            public string bitcoin_address { get; set; }
            public string issuable_asset { get; set; }
            public float balance { get; set; }
            public float unconfirmed_balance { get; set; }
            public CoinprismColoredCoinBalance[] assets { get; set; }
        }

        private class CoinprismColoredCoinBalance
        {
            public string id { get; set; }
            public string balance { get; set; }
            public string unconfirmed_balance { get; set; }
        }

        public class BlockCypherInput
        {
            public string prev_hash { get; set; }
            public int output_index { get; set; }
            public string script { get; set; }
            public long output_value { get; set; }
            public object sequence { get; set; }
            public string[] addresses { get; set; }
            public string script_type { get; set; }
        }

        public class BlockCypherOutput
        {
            public long value { get; set; }
            public string script { get; set; }
            public string spent_by { get; set; }
            public string[] addresses { get; set; }
            public string script_type { get; set; }
        }

        public class BlockCypherGetTransactionResult
        {
            public string block_hash { get; set; }
            public int block_height { get; set; }
            public int block_index { get; set; }
            public string hash { get; set; }
            public string hex { get; set; }
            public string[] addresses { get; set; }
            public long total { get; set; }
            public int fees { get; set; }
            public int size { get; set; }
            public string preference { get; set; }
            public string relayed_by { get; set; }
            public string confirmed { get; set; }
            public string received { get; set; }
            public int ver { get; set; }
            public int lock_time { get; set; }
            public bool double_spend { get; set; }
            public int vin_sz { get; set; }
            public int vout_sz { get; set; }
            public int confirmations { get; set; }
            public int confidence { get; set; }
            public BlockCypherInput[] inputs { get; set; }
            public BlockCypherOutput[] outputs { get; set; }
        }
    }
}
