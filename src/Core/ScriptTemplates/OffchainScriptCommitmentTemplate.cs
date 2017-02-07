using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.ScriptTemplates
{
    public class OffchainScriptCommitmentTemplate
    {
        public static bool CheckScriptPubKey(Script redeemScript)
        {
            var ops = redeemScript.ToOps().ToArray();
            if (ops.Length != 15)
                return false;
            if (ops[0].Code != OpcodeType.OP_IF)
                return false;
            if (ops[ops.Length - 3].Code != OpcodeType.OP_ENDIF)
                return false;
            return ops[ops.Length - 4].Code == OpcodeType.OP_CHECKSIG;
        }


        public static OffchainScriptParams ExtractScriptSigParameters(Script script)
        {
            var ops = script.ToOps().ToArray();
            var scriptParams = new OffchainScriptParams();
            scriptParams.IsMultisig = ops.Length == 5 && ops[3].Code == OpcodeType.OP_1;
            var signCnt = scriptParams.IsMultisig ? 3 : 1;
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

        public static OffchainPubKeysParameters ExtractScriptPubKeyParameters(Script redeemScript)
        {
            if (!CheckScriptPubKey(redeemScript))
                throw new Exception("Invalid script");

            var ops = redeemScript.ToOps().ToArray();
            var result = new OffchainPubKeysParameters
            {
                MultisigPubKeys =
                {
                    [0] = new PubKey(ops[2].PushData),
                    [1] = new PubKey(ops[3].PushData)
                },
                LockedPubKey = new PubKey(ops[10].PushData)
            };

            return result;
        }
    }

    public class OffchainScriptParams
    {
        public bool IsMultisig { get; set; }

        public byte[][] Pushes { get; set; }

        public byte[] RedeemScript { get; set; }
    }

    public class OffchainPubKeysParameters
    {
        public PubKey[] MultisigPubKeys { get; set; } = new PubKey[2];

        public PubKey LockedPubKey { get; set; }
    }

}
