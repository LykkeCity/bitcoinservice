using System;
using System.Collections.Generic;
using System.Linq;
using Core.Exceptions;
using Core.Repositories.TransactionOutputs;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace Core.OpenAssets
{
    public static class OpenAssetsHelper
    {
        public static BitcoinAddress GetBitcoinAddressFormBase58Date(string base58Data)
        {
            BitcoinAddress address = null;
            try
            {
                address = BitcoinAddress.Create(base58Data);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
            }

            if (address != null)           
                return address;            
            return new BitcoinColoredAddress(base58Data)?.Address;            
        }

        public static bool IsBitcoin(string asset)
        {
            return asset?.Trim()?.ToUpper() == "BTC";
        }

        public static bool IsLkk(string asset)
        {
            return asset?.Trim()?.ToUpper() == "LKK";
        }

        /// <summary>
        /// determine asset ids using order-based coloring method
        /// </summary>
        public static IEnumerable<ICoin> OrderBasedColoringOutputs(Transaction tr, TransactionBuildContext context)
        {
            uint markerPosition;
            var marker = ColorMarker.Get(tr, out markerPosition);

            if (marker == null)
                return tr.Outputs.AsCoins();
            List<ICoin> outputsWithAsset = new List<ICoin>();

            var inputAmounts = context.GetAssetAmounts.ToList();
            var inputAssets = context.GetAssetIds().ToList();


            int inputIndex = 0;
            int outputIndex = (int)markerPosition + 1;
            int quantityIndex = 0;

            int issueIndex = 0;

            while (issueIndex < markerPosition && quantityIndex < marker.Quantities.Length)
            {
                var outputAmount = marker.Quantities[quantityIndex];
                if (outputAmount == 0)
                    outputsWithAsset.Add(new Coin(tr, (uint)issueIndex));
                else
                    outputsWithAsset.Add(new Coin(tr, (uint)issueIndex).ToColoredCoin(context.IssuedAssetId, outputAmount));

                issueIndex++;
                quantityIndex++;
            }

            outputsWithAsset.AddRange(tr.Outputs.AsCoins().Skip(issueIndex).Take((int)markerPosition - issueIndex).Where(x => x.Amount != Money.Zero));

            try
            {
                while (inputIndex < context.Coins.Count && outputIndex < tr.Outputs.Count && quantityIndex < marker.Quantities.Length)
                {
                    var outputAmount = (long)marker.Quantities[quantityIndex];
                    if (outputAmount == 0)
                        outputsWithAsset.Add(new Coin(tr, (uint)outputIndex));
                    else
                    {
                        long coverAmount = 0;
                        string assetId = null;
                        while (coverAmount < outputAmount && inputIndex < context.Coins.Count)
                        {
                            var currentAsset = inputAssets[inputIndex];
                            if (currentAsset != null && currentAsset != assetId && assetId != null)
                            {
                                throw new BackendException("Can't determine assets of outputs", ErrorCode.Exception);
                            }
                            assetId = assetId ?? currentAsset;

                            var amount = Math.Min(outputAmount - coverAmount, inputAmounts[inputIndex]);
                            inputAmounts[inputIndex] -= amount;
                            coverAmount += amount;
                            if (inputAmounts[inputIndex] <= 0)
                                inputIndex++;
                        }
                        if (coverAmount < outputAmount)
                            throw new BackendException("Invalid color marker", ErrorCode.Exception);
                        var coin = new Coin(tr, (uint)outputIndex);
                        if (assetId != null)
                            outputsWithAsset.Add(coin.ToColoredCoin(new BitcoinAssetId(assetId, context.Network).AssetId, (ulong)outputAmount));
                        else
                            outputsWithAsset.Add(coin);
                    }
                    outputIndex++;
                    quantityIndex++;
                }
                outputsWithAsset.AddRange(tr.Outputs.AsCoins().Skip(1 + marker.Quantities.Length).Where(x => x.Amount != Money.Zero));
                return outputsWithAsset;
            }
            catch (BackendException)
            {
                return new List<ICoin>();
            }
        }

        public static string MergeTransactionsSignatures(string transaction1, string transaction2)
        {
            var tr1 = new Transaction(transaction1);
            var tr2 = new Transaction(transaction2);
            for (int i = 0; i < tr1.Inputs.Count; i++)
            {
                var scriptParams1 = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(tr1.Inputs[i].ScriptSig);
                var scriptParams2 = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(tr2.Inputs[i].ScriptSig);
                if (scriptParams1 != null)
                {
                    for (var j = 0; j < scriptParams2.Pushes.Length; j++)
                        if (scriptParams1.Pushes[j].Length == 0)
                            scriptParams1.Pushes[j] = scriptParams2.Pushes[j];
                    tr1.Inputs[i].ScriptSig = PayToScriptHashTemplate.Instance.GenerateScriptSig(scriptParams1);
                }
                else if (tr2.Inputs[i].ScriptSig != null && tr2.Inputs[i].ScriptSig.Length > 0)
                    tr1.Inputs[i].ScriptSig = tr2.Inputs[i].ScriptSig;
            }
            return tr1.ToHex();
        }

        public static IEnumerable<ICoin> CoinSelect(IEnumerable<ICoin> coins, IMoney target)
        {
            var rand = new Random((int)DateTime.Now.Ticks);
            var zero = target.Sub(target);
            var targetCoin = coins.FirstOrDefault(c => c.Amount.CompareTo(target) == 0);

            if (targetCoin != null)
                return new[] { targetCoin };

            var result = new List<ICoin>();
            var total = zero;

            if (target.CompareTo(zero) == 0)
                return result;

            var orderedCoins = coins.OrderBy(s => s.Amount).ToArray();

            foreach (var coin in orderedCoins)
            {
                if (coin.Amount.CompareTo(target) == -1 && total.CompareTo(target) == -1)
                {
                    total = total.Add(coin.Amount);
                    result.Add(coin);
                }
                else
                {
                    if (total.CompareTo(target) == -1 && coin.Amount.CompareTo(target) == 1)
                    {
                        return new[] { coin };
                    }
                    else
                    {
                        var allCoins = orderedCoins.ToArray();
                        for (int _ = 0; _ < 1000; _++)
                        {
                            var selection = new List<ICoin>();
                            Utils.Shuffle(allCoins, rand);
                            var currentTotal = zero;
                            for (int i = 0; i < allCoins.Length; i++)
                            {
                                selection.Add(allCoins[i]);
                                currentTotal = currentTotal.Add(allCoins[i].Amount);

                                // if new count already greater than previous
                                if (selection.Count > result.Count)
                                    break;

                                if (currentTotal.CompareTo(target) >= 0)
                                {
                                    // if new count less than previous then use it
                                    // if new count equals to previous but sum of used inputs is less, then use it
                                    if (selection.Count < result.Count || selection.Count == result.Count && currentTotal.CompareTo(total) == -1)
                                    {
                                        result = selection;
                                        total = currentTotal;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (total.CompareTo(target) == -1)
                return null;
            return result;
        }

        public static void DestroyColorCoin(Transaction tr, AssetMoney money, BitcoinAddress destination, Network network)
        {
            if (money == null || money.Quantity <= 0)
                return;
            uint markerPosition;
            var colorMarker = ColorMarker.Get(tr, out markerPosition);

            for (var i = 0; i < colorMarker.Quantities.Length; i++)
            {
                if ((long)colorMarker.Quantities[i] == money.Quantity &&
                    tr.Outputs[i + 1].ScriptPubKey.GetDestinationAddress(network) == destination)
                {
                    colorMarker.Quantities[i] = 0;
                    break;
                }
            }

            tr.Outputs[markerPosition].ScriptPubKey = colorMarker.GetScript();
        }
    }
}
