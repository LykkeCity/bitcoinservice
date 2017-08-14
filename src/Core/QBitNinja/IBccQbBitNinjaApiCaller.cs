using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using QBitNinja.Client.Models;

namespace Core.QBitNinja
{
    public interface IBccQbBitNinjaApiCaller
    {
        Task<BalanceModel> GetAddressBalance(string walletAddress);
    }
}
