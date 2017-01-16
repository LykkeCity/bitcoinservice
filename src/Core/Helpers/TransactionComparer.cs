using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.Helpers
{
    public static class TransactionComparer
    {
        public static bool CompareTransactions(string tr1Hex, string tr2Hex)
        {
            var tr1 = new Transaction(tr1Hex);
            var tr2 = new Transaction(tr2Hex);

            if (tr1.Inputs.Count != tr2.Inputs.Count)
                return false;
            if (tr1.Version != tr2.Version)
                return false;
            for (var i = 0; i < tr1.Inputs.Count; i++)
            {
                var in1 = tr1.Inputs[i];
                var in2 = tr2.Inputs[i];
                if (in1.PrevOut != in2.PrevOut || in1.Sequence != in2.Sequence)
                    return false;
            }

            for (var i = 0; i < tr1.Outputs.Count; i++)
            {
                var out1 = tr1.Outputs[i];
                var out2 = tr2.Outputs[i];
                if (out1.ScriptPubKey != out2.ScriptPubKey || out1.Value != out2.Value)
                    return false;
            }
            return true;
        }
    }
}
