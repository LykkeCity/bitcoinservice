using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Core.Helpers
{
    public static class ScriptExtensions
    {
        public static Script ToScript(this string hex)
        {
            return Script.FromBytesUnsafe(Encoders.Hex.DecodeData(hex));
        }
    }
}
