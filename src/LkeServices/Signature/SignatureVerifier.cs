using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;
using LkeServices.Transactions;
using NBitcoin;

namespace LkeServices.Signature
{
    public interface ISignatureVerifier
    {
        Task<bool> Verify(string trHex, string pubKey, SigHash hashType = SigHash.All);
        bool VerifyScriptSigs(string trHex);
    }

    public class SignatureVerifier : ISignatureVerifier
    {
        private readonly IBitcoinTransactionService _bitcoinTransactionService;
        private readonly RpcConnectionParams _rpcParams;

        public SignatureVerifier(IBitcoinTransactionService bitcoinTransactionService, RpcConnectionParams rpcParams)
        {
            _bitcoinTransactionService = bitcoinTransactionService;
            _rpcParams = rpcParams;
        }

        public async Task<bool> Verify(string trHex, string pubKey, SigHash hashType = SigHash.All)
        {
            PubKey checkPubKey = new PubKey(pubKey);
            var tr = new Transaction(trHex);
            for (var i = 0; i < tr.Inputs.Count; i++)
            {
                var input = tr.Inputs[i];
                var redeemScript = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(input.ScriptSig)?.RedeemScript;
                if (redeemScript != null)
                {
                    if (PayToMultiSigTemplate.Instance.CheckScriptPubKey(redeemScript))
                    {
                        var pubkeys = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(redeemScript).PubKeys;
                        for (int j = 0; j < pubkeys.Length; j++)
                        {
                            if (pubkeys[j] == checkPubKey)
                            {
                                var scriptParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(input.ScriptSig);
                                var hash = Script.SignatureHash(scriptParams.RedeemScript, tr, i, hashType);
                                if (!checkPubKey.Verify(hash, scriptParams.Pushes[j + 1]))
                                    return false;
                            }
                        }
                        continue;
                    }
                }

                var prevTransaction = await _bitcoinTransactionService.GetTransaction(input.PrevOut.Hash.ToString());
                var output = prevTransaction.Outputs[input.PrevOut.N];

                if (PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(output.ScriptPubKey))
                {
                    if (output.ScriptPubKey.GetDestinationAddress(_rpcParams.Network) ==
                        checkPubKey.GetAddress(_rpcParams.Network))
                    {
                        var hash = Script.SignatureHash(output.ScriptPubKey, tr, i, hashType);
                        var sign = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(input.ScriptSig)?.TransactionSignature?.Signature;
                        if (sign == null)
                            return false;
                        if (!checkPubKey.Verify(hash, sign))
                            return false;
                    }
                }
            }
            return true;
        }

        public bool VerifyScriptSigs(string trHex)
        {
            var tr = new Transaction(trHex);
            foreach (var trInput in tr.Inputs)
            {
                if (trInput.ScriptSig == null)
                    return false;
                var multiSigParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(trInput.ScriptSig);
                if (multiSigParams != null)
                {
                    foreach (var push in multiSigParams.Pushes.Skip(1))
                    {
                        if (push.Length == 0)
                            return false;
                    }
                }
            }
            return true;
        }
    }
}
