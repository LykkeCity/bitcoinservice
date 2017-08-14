using Core.Enums;
using Core.Settings;
using NBitcoin;

namespace Core.Bitcoin
{
    public class RpcConnectionParams
    {
        public string UserName { get; }

        public string Password { get; }
        public string IpAddress { get; }

        public Network Network { get; }

        public RpcConnectionParams(BaseSettings settings)
        {
            UserName = settings.RPCUsername;
            Password = settings.RPCPassword;
            IpAddress = settings.RPCServerIpAddress;
            Network = settings.NetworkType == NetworkType.Main ? Network.Main : Network.TestNet;
        }

        public RpcConnectionParams(BccSettings settings)
        {
            UserName = settings.RPCUsername;
            Password = settings.RPCPassword;
            IpAddress = settings.RPCServerIpAddress;
            Network = settings.NetworkType == NetworkType.Main ? Network.Main : Network.TestNet;
        }
    }
}
