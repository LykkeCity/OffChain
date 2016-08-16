using NBitcoin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static OffchainGUI.OpenAssetsHelper;
using PrimS.Telnet;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Data.Linq;
using System.Windows.Threading;
using System.Security.Cryptography;

namespace OffchainGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ChannelState channelState = ChannelState.Reset;
        public MainWindow()
        {
            InitializeComponent();

            LoadTabContents();
        }

        private static Settings settings = new Settings();

        public class Settings
        {

            public string TelnetHostName
            {
                get;
                set;
            }

            public int TelnetPort
            {
                get;
                set;
            }

            public IList<AssetDefinition> Assets
            {
                get;
                set;
            }

            public string RPCUserName
            {
                get;
                set;
            }

            public string RPCPassword
            {
                get;
                set;
            }

            public string RPCServerIpAddress
            {
                get;
                set;
            }

            public int RPCServerPort
            {
                get;
                set;
            }
        }

        private void LoadTabContents()
        {
            IList<string> list = new List<string>();
            settings.Assets = new List<AssetDefinition>();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            AssetConfigurationSection assetConfigSection =
                ConfigurationManager.GetSection("AssetsSection") as AssetConfigurationSection;
            AssetDefinition asset = null;
            for (int i = 0; i < assetConfigSection.Assets.Count; i++)
            {
                var configAsset = assetConfigSection.Assets[i];
                asset = new AssetDefinition();
                asset.AssetAddress = configAsset.AssetAddress;
                asset.AssetId = configAsset.AssetId;
                asset.DefinitionUrl = configAsset.DefinitionUrl;
                asset.Divisibility = configAsset.Divisibility;
                asset.Name = configAsset.Name;
                asset.PrivateKey = configAsset.PrivateKey;
                settings.Assets.Add(asset);
            }

            comboBoxAssetToSend.Items.Add("BTC");
            ((List<AssetDefinition>)settings.Assets).ForEach(a => { comboBoxAssetToNegotiate.Items.Add(a.Name); comboBoxAssetToIssue.Items.Add(a.Name); comboBoxAssetToSend.Items.Add(a.Name); });
            textBoxWalletPrivateKey.Text = config.AppSettings.Settings["WalletPrivateKey"].Value;
            comboNetwork.SelectedItem = ConvertStringNetworkToNBitcoinNetwork(config.AppSettings.Settings["Netwrok"].Value).ToString();
            QBitNinjaBaseUrl = config.AppSettings.Settings["QBitNinjaBaseUrl"].Value;
            settings.TelnetHostName = config.AppSettings.Settings["TelnetHostName"].Value;
            settings.TelnetPort = Int32.Parse(config.AppSettings.Settings["TelnetPort"].Value);
            settings.RPCUserName = config.AppSettings.Settings["RPCUserName"].Value;
            settings.RPCPassword = config.AppSettings.Settings["RPCPassword"].Value;
            settings.RPCServerIpAddress = config.AppSettings.Settings["RPCServerIpAddress"].Value;
            settings.RPCServerPort = Int32.Parse(config.AppSettings.Settings["RPCServerPort"].Value);
        }

        private static Network ConvertStringNetworkToNBitcoinNetwork(string network)
        {
            network = network.ToLower();
            switch (network)
            {
                case "main":
                    return Network.Main;
                case "testnet":
                    return Network.TestNet;
                case "segnet":
                    return Network.SegNet;
                default:
                    return Network.TestNet;
            }
        }

        private string GetSelectedNetwork()
        {
            return comboNetwork.GetComboBoxSelectedValueContentSafely(Dispatcher);
        }

        private BitcoinSecret GetBitcoinSecretFromScreenBoxes()
        {
            BitcoinSecret secret = null;
            var network = GetSelectedNetwork();

            try
            {
                secret = new BitcoinSecret(textBoxWalletPrivateKey.GetTextBoxValueSafely(Dispatcher), ConvertStringNetworkToNBitcoinNetwork(network));
            }
            catch (Exception)
            {
                ShowError("Can not create a valid private key from entered values.");
                return null;
            }
            return secret;
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxWalletPrivateKey.Text))
            {
                ShowError("Private key should not be null or empty.");
                return;
            }

            BitcoinSecret secret = null;
            try
            {
                var network = (comboNetwork.SelectedValue as ComboBoxItem).Content.ToString();
                if ((secret = GetBitcoinSecretFromScreenBoxes()) == null)
                {
                    return;
                }
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["WalletPrivateKey"].Value = textBoxWalletPrivateKey.Text;
                config.AppSettings.Settings["Netwrok"].Value = network;
                config.Save(ConfigurationSaveMode.Full);
            }
            catch (Exception)
            {
                ShowError("Not a valid bitcoin secret.");
                return;
            }

        }

        private void buttonGenerateNewPrivateKey_Click(object sender, RoutedEventArgs e)
        {
            NBitcoin.Key key = new NBitcoin.Key();
            BitcoinSecret secret = new BitcoinSecret(key, ConvertStringNetworkToNBitcoinNetwork((comboNetwork.SelectedValue as ComboBoxItem).Content.ToString()));
            if (checkBoxPrivateKeyInBase58.IsChecked ?? false)
            {
                textBoxWalletPrivateKey.Text =
                    Base58Encoding.Encode(secret.PrivateKey.ToBytes());
            }
            else
            {
                textBoxWalletPrivateKey.Text = secret.ToWif().ToString();
            }
        }

        public class UnspentOutput
        {
            public string Name
            {
                get;
                set;
            }

            public string Address
            {
                get;
                set;
            }

            public int OutputNumber
            {
                get;
                set;
            }
        }

        private async void buttonRefreshWalletOutputs_Click(object sender, RoutedEventArgs e)
        {
            dataGridWalletOutputs.ItemsSource = null;

            string stringNetwork = (comboNetwork.SelectedValue as ComboBoxItem).Content.ToString();
            Network bitcoinNetwork = ConvertStringNetworkToNBitcoinNetwork(stringNetwork);

            string sourceWalletAddress = null;
            if (checkBoxSendFromP2PKH.IsChecked ?? false)
            {
                sourceWalletAddress = textBoxWalletAddressP2PKHContent.GetTextBoxValueSafely(Dispatcher);
            }
            else
            {
                sourceWalletAddress = textBoxWalletAddressP2WPKHContent.GetTextBoxValueSafely(Dispatcher);
            }

            var ret = await GetWalletOutputs(sourceWalletAddress, bitcoinNetwork);

            if (ret.Item2)
            {
                ShowError(ret.Item3);
                return;
            }

            var displayLines = from coin in ret.Item1
                               join asset in settings.Assets on (((QBitNinjaUnspentOutput)coin).asset_id) equals asset.AssetId into gj
                               from subasset in gj.DefaultIfEmpty()
                               select new
                               {
                                   TransactionHash = ((QBitNinjaUnspentOutput)coin).transaction_hash,
                                   OutputIndex = ((QBitNinjaUnspentOutput)coin).output_index,
                                   SatoshiValue = ((QBitNinjaUnspentOutput)coin).value,
                                   Confirmations = ((QBitNinjaUnspentOutput)coin).confirmations,
                                   AssetId = ((QBitNinjaUnspentOutput)coin).asset_id,
                                   AssetQuantity = ((QBitNinjaUnspentOutput)coin).asset_quantity / (subasset?.MultiplyFactor ?? 1)
                               };

            dataGridWalletOutputs.ItemsSource = displayLines;
        }

        private void textBoxWalletPrivateKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxWalletPrivateKey.Text))
            {
                string stringNetwork = (comboNetwork.SelectedValue as ComboBoxItem).Content.ToString();
                Network bitcoinNetwork = ConvertStringNetworkToNBitcoinNetwork(stringNetwork);

                BitcoinSecret secret = null;
                try
                {
                    if (checkBoxPrivateKeyInBase58.IsChecked ?? false)
                    {
                        NBitcoin.Key key = new NBitcoin.Key(Base58Encoding.Decode(textBoxWalletPrivateKey.Text));
                        secret = new BitcoinSecret(key, ConvertStringNetworkToNBitcoinNetwork(GetSelectedNetwork()));
                    }
                    else
                    {
                        secret = new BitcoinSecret(textBoxWalletPrivateKey.Text, ConvertStringNetworkToNBitcoinNetwork(GetSelectedNetwork()));
                    }
                }
                catch (Exception)
                {
                    ShowError("Could not get a valid address.");
                    return;
                }

                textBoxWalletAddressP2PKHContent.Text = secret.GetAddress().ToWif();
                textBoxWalletAddressP2WPKHContent.Text = secret.PubKey.GetSegwitAddress(Network.SegNet).ToWif();
                PayToPubkeyHashTemplate p2pkh = new PayToPubkeyHashTemplate();
                Script p2pkhScript = p2pkh.GenerateScriptPubKey(new BitcoinPubKeyAddress(textBoxWalletAddressP2PKHContent.Text));
                textBoxAssetIdP2PKH.Text = (new NBitcoin.OpenAsset.AssetId(p2pkhScript)).
                    GetWif(ConvertStringNetworkToNBitcoinNetwork(GetSelectedNetwork())).
                    ToString();
                PayToWitPubKeyHashTemplate p2wpkh = new PayToWitPubKeyHashTemplate();
                Script p2wpkhScript = p2wpkh.GenerateScriptPubKey(new BitcoinWitPubKeyAddress(textBoxWalletAddressP2WPKHContent.Text));
                textBoxAssetIdP2WPKH.Text = (new NBitcoin.OpenAsset.AssetId(p2wpkhScript)).
                    GetWif(ConvertStringNetworkToNBitcoinNetwork(GetSelectedNetwork())).
                    ToString();
                textBoxWalletPubKey.Text = secret.PubKey.ToString();
                UpdateMultiSig();
            }
        }

        public class JSONRequest
        {
            public string method
            {
                get;
                set;
            }

            [JsonProperty("params")]
            public string[] parameters
            {
                get;
                set;
            }

            public int id
            {
                get;
                set;
            }
        }

        public class JSONResponse
        {
            public int id
            {
                get;
                set;
            }

            public string jsonrpc
            {
                get;
                set;
            }

            public virtual string result
            {
                get;
                set;
            }
        }

        public class JSONNegotiateResponse : JSONResponse
        {
            public new NegotiateReply result
            {
                get;
                set;
            }
        }

        public class BaseReply
        {
            public string Error
            {
                get;
                set;
            }
        }

        public class HelloReply : BaseReply
        {
            public string SessionNumber
            {
                get;
                set;
            }
        }

        public class CreateBaseTransactionReply : BaseReply
        {
            public string TransactionHex
            {
                get;
                set;
            }
        }

        public enum AcceptDeny
        {
            Accept,
            Deny
        }

        public class NegotiateReply : BaseReply
        {
            public AcceptDeny Result
            {
                get;
                set;
            }
        }

        private bool ChannelShouldBe(OffchainGUI.ChannelState channelState)
        {
            if (MainWindow.channelState != channelState)
            {
                var message = String.Format("Channel should be in {0} state."
                    , channelState.ToString());
                ShowError(message);
                return false;
            }
            else
            {
                return true;
            }
        }

        private async void buttonSayHello_Click(object sender, RoutedEventArgs e)
        {
            if (!ChannelShouldBe(ChannelState.Reset))
            {
                return;
            }

            BitcoinSecret secret = null;
            if ((secret = GetBitcoinSecretFromScreenBoxes()) == null)
            {
                return;
            }

            if (GetSelectedNetwork().ToLower() != "segnet")
            {
                ShowError("Only Segnet addresses is supported.");
                return;
            }

            string helloReply = null;

            try
            {
                string randomMessage = "Hello";
                var paramsList = new List<string>();
                paramsList.Add(secret.PubKey.ToHex());
                //paramsList.Add(GetSelectedNetwork());
                paramsList.Add("TestNet"); // Currently SegNet over testnet is the only option
                paramsList.Add(randomMessage);
                paramsList.Add(secret.PrivateKey.SignMessage(randomMessage));

                helloReply = await CreateAndSendJsonRequest("Hello", paramsList.ToArray());
            }
            catch (Exception exp)
            {
                ShowError(exp.Message);
                return;
            }

            if (string.IsNullOrEmpty(helloReply))
            {
                ShowError("Null reply");
                return;
            }
            var deserializedReply = JsonConvert.DeserializeObject<JSONResponse>(helloReply);
            textBoxChannelSessionNumber.SetTextBoxValueSafely(Dispatcher, JsonConvert.DeserializeObject<HelloReply>(deserializedReply.result).SessionNumber);

            channelState = ChannelState.HelloFinished;
        }

        private static async Task<string> CreateAndSendJsonRequest(string requestName, string[] parameters)
        {
            JSONRequest request = new JSONRequest();
            request.id = 1;
            request.method = requestName;

            string reply = null;

            request.parameters = parameters;

            string requestString = JsonConvert.SerializeObject(request);

            using (Client client = new Client(settings.TelnetHostName, settings.TelnetPort,
                new System.Threading.CancellationToken()))
            {
                await client.WriteLine(requestString);
                for (int i = 0; i < 3; i++)
                {
                    reply = await client.ReadAsync(new TimeSpan(0, 0, 20));
                }
            }

            return reply;
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e)
        {
            channelState = ChannelState.Reset;
            textBoxChannelSessionNumber.Text = string.Empty;
            textBoxNegotiateResult.Text = string.Empty;
            textBoxCreateBaseTransactionHex.Text = string.Empty;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }


        private async void buttonNegotiateChannel_Click(object sender, RoutedEventArgs e)
        {
            float requestedAmount = 0;
            float contributedAmount = 0;
            float tolerancePercentage = 0;
            if (!ChannelShouldBe(ChannelState.HelloFinished))
            {
                return;
            }

            if (comboBoxAssetToNegotiate.GetComboBoxSelectedItemSafely(Dispatcher) == null)
            {
                ShowError("At lease one asset should be selected.");
                return;
            }
            if (!float.TryParse(textBoxNegotiateRequestedAmount.GetTextBoxValueSafely(Dispatcher), out requestedAmount))
            {
                ShowError("Could not parse the number from requested amount.");
                return;
            }
            if (!float.TryParse(textBoxNegotiateToleratedPercentage.GetTextBoxValueSafely(Dispatcher),
                out tolerancePercentage))
            {
                ShowError("Could not parse the number from tolerance percentage.");
                return;
            }
            else
            {
                if (tolerancePercentage < 0 || tolerancePercentage > 100)
                {
                    ShowError("Tolerance percentage should be between 0 and 100.");
                    return;
                }
            }
            if (!float.TryParse(textBoxNegotiateContributedAmount.GetTextBoxValueSafely(Dispatcher), out contributedAmount))
            {
                ShowError("Could not parse the number from contibuted amount.");
                return;
            }

            string assetName = settings.Assets.Where(c => c.Name.Equals(comboBoxAssetToNegotiate.GetComboBoxSelectedItemSafely(Dispatcher)))
                .Select(a => a.Name).FirstOrDefault();

            string negotiateReply = null;

            try
            {
                var paramsList = new List<string>();
                paramsList.Add(textBoxChannelSessionNumber.GetTextBoxValueSafely(Dispatcher));
                paramsList.Add(assetName);
                paramsList.Add(textBoxNegotiateRequestedAmount.GetTextBoxValueSafely(Dispatcher));
                paramsList.Add(textBoxNegotiateToleratedPercentage.GetTextBoxValueSafely(Dispatcher));
                paramsList.Add(textBoxNegotiateContributedAmount.GetTextBoxValueSafely(Dispatcher));

                negotiateReply = await CreateAndSendJsonRequest("NegotiateChannel",
                    paramsList.ToArray());
            }
            catch (Exception exp)
            {
                ShowError(exp.Message);
                return;
            }

            var deserializedReply = JsonConvert.DeserializeObject<JSONNegotiateResponse>(negotiateReply);
            if (deserializedReply.result.Error != null)
            {
                ShowError(deserializedReply.result.Error);
                return;
            }
            else
            {
                textBoxNegotiateResult.SetTextBoxValueSafely(Dispatcher, deserializedReply.result.Result.ToString());
            }

            channelState = ChannelState.NegotiateChannelFinished;
        }

        private async void buttonCreateBaseTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (!ChannelShouldBe(ChannelState.NegotiateChannelFinished))
            {
                return;
            }

            if (string.IsNullOrEmpty(textBoxCreateBaseTransactionTransactionId.Text))
            {
                ShowError("Transaction Id should have a value.");
                return;
            }

            int outputNumber = 0;
            if (string.IsNullOrEmpty(textBoxCreateBaseTransactionOutputNumber.Text) ||
                !Int32.TryParse(textBoxCreateBaseTransactionOutputNumber.Text, out outputNumber))
            {
                ShowError("Output number should be a valid int.");
                return;
            }

            var transactionHex = await GetTransactionHex(textBoxCreateBaseTransactionTransactionId.Text, ConvertStringNetworkToNBitcoinNetwork(comboNetwork.Text),
                settings.RPCUserName, settings.RPCPassword, settings.RPCServerIpAddress, settings.RPCServerPort);
            if (transactionHex.Item1)
            {
                ShowError(string.Format("Error occured while getting transaction: {0}", transactionHex.Item2));
                return;
            }
            Transaction tx = new Transaction(transactionHex.Item3);
            if (tx.Outputs.Count - 1 < outputNumber)
            {
                ShowError("Output number is larger than the available outputs.");
                return;
            }
            if (GetAddressFromScriptPubKey(tx.Outputs[outputNumber].ScriptPubKey, ConvertStringNetworkToNBitcoinNetwork(GetSelectedNetwork()))
                != textBoxWalletAddressP2PKHContent.Text)
            {
                ShowError("The destination address for the specified output is different than the required address.");
                return;
            }

            string createBaseTransactionReply = null;

            try
            {
                var paramsList = new List<string>();
                paramsList.Add(textBoxChannelSessionNumber.Text);
                paramsList.Add(textBoxCreateBaseTransactionTransactionId.Text);
                paramsList.Add(textBoxCreateBaseTransactionOutputNumber.Text);

                createBaseTransactionReply = await CreateAndSendJsonRequest("CreateBaseTransaction",
                    paramsList.ToArray());
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message, "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var deserializedReply = JsonConvert.DeserializeObject<JSONResponse>(createBaseTransactionReply);
            var jsonReply = JsonConvert.DeserializeObject<CreateBaseTransactionReply>(deserializedReply.result);
            if (jsonReply.Error != null)
            {
                ShowError(jsonReply.Error);
                return;
            }
            else
            {
                textBoxCreateBaseTransactionHex.Text = jsonReply.TransactionHex;
            }

            channelState = ChannelState.CreateBaseTransacionFinished;
        }

        public async void buttonSendAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = (comboBoxAssetToSend.SelectedItem as string);
                var destinationWallet = textBoxSendDestinationAddress.GetTextBoxValueSafely(Dispatcher);
                float sendAmount = 0;
                if (string.IsNullOrEmpty(asset))
                {
                    ShowError("We should first have a valid asset to send.");
                    return;
                }
                if (string.IsNullOrEmpty(destinationWallet))
                {
                    ShowError("Destination wallet should have a valid non null / non empty value.");
                    return;
                }
                if (!float.TryParse(textBoxAmountToSend.GetTextBoxValueSafely(Dispatcher), out sendAmount))
                {
                    ShowError("Amount to issue should be a valid float.");
                    return;
                }
                string sourceWalletAddress = null;
                if (checkBoxSendFromP2PKH.IsChecked ?? false)
                {
                    sourceWalletAddress = textBoxWalletAddressP2PKHContent.GetTextBoxValueSafely(Dispatcher);
                }
                else
                {
                    sourceWalletAddress = textBoxWalletAddressP2WPKHContent.GetTextBoxValueSafely(Dispatcher);
                }
                var sourcePrivateKey = textBoxWalletPrivateKey.GetTextBoxValueSafely(Dispatcher);
                var network = ConvertStringNetworkToNBitcoinNetwork(GetSelectedNetwork());

                if (string.IsNullOrEmpty(sourceWalletAddress))
                {
                    ShowError("Source wallet address (P2WPKH) should not be empty.");
                    return;
                }

                AssetDefinition assetWallet = null;
                string assetWalletAddress = null;
                if (!asset.ToLower().Equals("btc"))
                {
                    assetWallet = (from item in settings.Assets
                                   where item.Name.Equals(asset)
                                   select item).FirstOrDefault();

                    if (assetWallet == null)
                    {
                        ShowError("The selected asset could not be found.");
                        return;
                    }
                    assetWalletAddress = assetWallet.AssetAddress;
                }

                var walletOutputs = await GetWalletOutputs(sourceWalletAddress, network);
                if (walletOutputs.Item2)
                {
                    ShowError(walletOutputs.Item3);
                    return;
                }

                var coins = await GetColoredUnColoredCoins(walletOutputs.Item1, assetWallet?.AssetId, network,
                    settings.RPCUserName, settings.RPCPassword, settings.RPCServerIpAddress, settings.RPCServerPort);

                var destinationAddress = BitcoinAddress.Create(destinationWallet, network);
                var builder = new TransactionBuilder();
                Transaction tx = null;

                var uncoloredCoins = coins.Item2;
                if (asset.ToLower().Equals("btc"))
                {
                    if (uncoloredCoins.Count() == 0)
                    {
                        ShowError("There is no uncolored coins to use for sending.");
                        return;
                    }
                    tx = builder
                    .AddKeys(new BitcoinSecret(sourcePrivateKey, network))
                    .AddCoins(uncoloredCoins)
                    .Send(destinationAddress, new Money((int)sendAmount))
                    .SetChange(BitcoinAddress.Create(sourceWalletAddress, network))
                    .SendFees(TransactionSendFeesInSatoshi)
                    .BuildTransaction(true);
                }
                else
                {
                    var coloredCoins = coins.Item1;
                    if (coloredCoins.Count() == 0)
                    {
                        ShowError(string.Format("There is no colored coins to use for sending. Asset: {0}", assetWallet.Name));
                        return;
                    }
                    tx = builder
                    .AddKeys(new BitcoinSecret(sourcePrivateKey, network))
                    .AddCoins(coloredCoins)
                    .AddCoins(uncoloredCoins)
                    .SendAsset(destinationAddress, new NBitcoin.OpenAsset.AssetMoney(new NBitcoin.OpenAsset.AssetId(new NBitcoin.OpenAsset.BitcoinAssetId(assetWallet.AssetId, network)), Convert.ToInt64(sendAmount * assetWallet.MultiplyFactor)))
                    .SetChange(BitcoinAddress.Create(sourceWalletAddress, network))
                    .SendFees(TransactionSendFeesInSatoshi)
                    .BuildTransaction(true);

                    /*
                    // For testing if in segwit transaction id does have nothing to do with signature
                    TransactionBuilder builder1 = new TransactionBuilder();
                    var tx1 = builder1
                    .AddCoins(coloredCoins)
                    .AddCoins(uncoloredCoins)
                    .SendAsset(destinationAddress, new NBitcoin.OpenAsset.AssetMoney(new NBitcoin.OpenAsset.AssetId(new NBitcoin.OpenAsset.BitcoinAssetId(assetWallet.AssetId, network)), Convert.ToInt64(sendAmount * assetWallet.MultiplyFactor)))
                    .SetChange(BitcoinAddress.Create(sourceWalletAddress, network))
                    .SendFees(TransactionSendFeesInSatoshi)
                    .BuildTransaction(false);
                    */
                }

                await SendTransaction(tx, settings.RPCUserName, settings.RPCPassword,
                    settings.RPCServerIpAddress, settings.RPCServerPort);

                textBoxSendTransactionId.SetTextBoxValueSafely(Dispatcher, tx.GetHash().ToString());
            }
            catch (Exception exp)
            {
                ShowError(string.Format("An error occured while sending asset: {0}", exp.Message));
                return;
            }
        }

        private async void buttonIssueAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = (comboBoxAssetToIssue.SelectedItem as string);
                var destinationWallet = textBoxIssuanceDestinationAddress.GetTextBoxValueSafely(Dispatcher);
                float issuedAmount = 0;
                if (string.IsNullOrEmpty(asset))
                {
                    ShowError("We should first have a valid address to issue assets from.");
                    return;
                }
                if (string.IsNullOrEmpty(destinationWallet))
                {
                    ShowError("Destination wallet should have a valid non null / non empty value.");
                    return;
                }
                if (!float.TryParse(textBoxAmountToIssue.GetTextBoxValueSafely(Dispatcher), out issuedAmount))
                {
                    ShowError("Amount to issue should be a valid float.");
                    return;
                }
                var network = ConvertStringNetworkToNBitcoinNetwork(GetSelectedNetwork());

                var issuanceWallet = (from item in settings.Assets
                                      where item.Name.Equals(asset)
                                      select item).FirstOrDefault();

                if (issuanceWallet == null)
                {
                    ShowError("The selected asset could not be found.");
                    return;
                }
                var issuanceWalletAddress = issuanceWallet.AssetAddress;

                var walletOutputs = await GetWalletOutputs(issuanceWalletAddress, network);
                if (walletOutputs.Item2)
                {
                    ShowError(walletOutputs.Item3);
                    return;
                }

                var coins = await GetColoredUnColoredCoins(walletOutputs.Item1, null, network,
                    settings.RPCUserName, settings.RPCPassword, settings.RPCServerIpAddress, settings.RPCServerPort);


                var uncoloredCoins = coins.Item2;
                if (uncoloredCoins.Count() == 0)
                {
                    ShowError("There is no uncolored coins to use for issuance.");
                    return;
                }

                var destinationAddress = BitcoinAddress.Create(destinationWallet, network);

                var issuanceCoin = new IssuanceCoin(uncoloredCoins[0]);
                var builder = new TransactionBuilder();

                var tx = builder
                    .AddKeys(new BitcoinSecret(issuanceWallet.PrivateKey, network))
                    .AddCoins(issuanceCoin)
                    .IssueAsset(destinationAddress, new NBitcoin.OpenAsset.AssetMoney(new NBitcoin.OpenAsset.AssetId(new NBitcoin.OpenAsset.BitcoinAssetId(issuanceWallet.AssetId, network)), Convert.ToInt64(issuedAmount * issuanceWallet.MultiplyFactor)))
                    .SetChange(BitcoinAddress.Create(issuanceWalletAddress, network))
                    .SendFees(TransactionSendFeesInSatoshi)
                    .BuildTransaction(true);

                await SendTransaction(tx, settings.RPCUserName, settings.RPCPassword,
                    settings.RPCServerIpAddress, settings.RPCServerPort);

                textBoxIssueTransaction.SetTextBoxValueSafely(Dispatcher, tx.GetHash().ToString());
            }
            catch (Exception exp)
            {
                ShowError(string.Format("An error occured while issuing asset: {0}", exp.Message));
                return;
            }
        }

        private void CheckChanged(object sender, RoutedEventArgs e)
        {
            if (checkBoxPrivateKeyInBase58.IsChecked ?? false)
            {
                BitcoinSecret secret = new BitcoinSecret(textBoxWalletPrivateKey.Text, ConvertStringNetworkToNBitcoinNetwork(GetSelectedNetwork()));
                textBoxWalletPrivateKey.Text =
                    Base58Encoding.Encode(secret.PrivateKey.ToBytes());
            }
            else
            {
                NBitcoin.Key key = new NBitcoin.Key(Base58Encoding.Decode(textBoxWalletPrivateKey.Text));
                BitcoinSecret secret = new BitcoinSecret(key, ConvertStringNetworkToNBitcoinNetwork(GetSelectedNetwork()));
                textBoxWalletPrivateKey.Text = secret.ToWif().ToString();
            }
        }

        private void checkBoxPrivateKeyInBase58_Checked(object sender, RoutedEventArgs e)
        {
            CheckChanged(sender, e);
        }

        private void checkBoxPrivateKeyInBase58_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckChanged(sender, e);
        }

        private void textBoxExchangePubKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                PubKey pubKey = new PubKey(textBoxExchangePubKey.Text);
            }
            catch (Exception)
            {
                ShowError("The entered value for exchange public key could not be parsed correctly.");
            }
            UpdateMultiSig();
        }

        private void UpdateMultiSig()
        {
            var pubKey01 = textBoxWalletPubKey.Text;
            var pubKey02 = textBoxExchangePubKey.Text;

            if(!string.IsNullOrEmpty(pubKey01) 
                && !string.IsNullOrEmpty(pubKey02))
            {
                var multisig = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2,
                    new PubKey[] { new PubKey(pubKey01), new PubKey(pubKey02) });
                textBoxMultisig.Text = multisig.GetScriptAddress
                    (ConvertStringNetworkToNBitcoinNetwork(GetSelectedNetwork())).ToWif();
            }
        }
    }
}