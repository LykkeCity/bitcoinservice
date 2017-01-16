using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.ScriptTemplates
{
    public class OffchainScriptCommitmentTemplate
    {
        public static bool CheckScript(Script redeemScript)
        {
            var ops = redeemScript.ToOps().ToArray();
            if (ops.Length != 12)
                return false;
            if (ops[0].Code != OpcodeType.OP_IF)
                return false;
            if (ops[ops.Length - 1].Code != OpcodeType.OP_ENDIF)
                return false;
            return ops[ops.Length - 2].Code == OpcodeType.OP_CHECKSIG;
        }


        public static OffchainScriptParams ExtractParamsFromScriptSig(Script script)
        {
            var ops = script.ToOps().ToArray();
            var scriptParams = new OffchainScriptParams();
            scriptParams.IsMultisig = ops.Length == 4 && ops[2].Code == OpcodeType.OP_1;
            var signCnt = scriptParams.IsMultisig ? 2 : 1;
            scriptParams.Pushes = ops.Take(signCnt).Select(o => o.PushData).ToArray();
            scriptParams.RedeemScript = ops[ops.Length - 1].PushData;
            return scriptParams;
        }

        public static Script GenerateScriptSig(OffchainScriptParams scriptParams)
        {
            List<Op> ops = new List<Op>();
            ops.AddRange(scriptParams.Pushes.Select(Op.GetPushOp));
            ops.Add(scriptParams.IsMultisig ? OpcodeType.OP_1 : OpcodeType.OP_0);
            ops.Add(Op.GetPushOp(scriptParams.RedeemScript));
            return new Script(ops);
        }    
    }

    public class OffchainScriptParams
    {
        public bool IsMultisig { get; set; }

        public byte[][] Pushes { get; set; }

        public byte[] RedeemScript { get; set; }
    }

}
