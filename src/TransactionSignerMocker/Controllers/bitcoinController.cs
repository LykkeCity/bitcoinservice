using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace TransactionSignerMocker.Controllers
{
    [Route("api/[controller]")]
    public class bitcoinController : Controller
    {
        [HttpGet("sayhello")]
        public string Sayhello()
        {
            return "Hello";
        }

        [HttpGet("key")]
        public string Key()
        {
            var key = new NBitcoin.Key();
            return key.PubKey.ToHex();
        }

        [HttpPost("sign")]
        public string Sign([FromBody]string Transaction)
        {
            return Transaction;
        }

    }
}
