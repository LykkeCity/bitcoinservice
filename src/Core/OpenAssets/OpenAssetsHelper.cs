using System;
using System.Collections.Generic;
using System.Linq;
using Core.Exceptions;
using Core.Repositories.TransactionOutputs;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace Core.OpenAssets
{
    public class OpenAssetsHelper
    {
        public static BitcoinAddress GetBitcoinAddressFormBase58Date(string base58Data)
        {
            var base58Decoded = Base58Data.GetFromBase58Data(base58Data);
            var address = base58Decoded as BitcoinAddress;
            if (address != null)
            {
                return address;
            }
            return (base58Decoded as BitcoinColoredAddress)?.Address;
        }

        public static bool IsBitcoin(string asset)
        {
            return asset?.Trim()?.ToUpper() == "BTC";
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
    }
}
