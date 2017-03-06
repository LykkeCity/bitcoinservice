using System;
using Autofac;
using Core.OpenAssets;
using NBitcoin;

namespace LkeServices.Transactions
{
    public class TransactionBuildContextFactory
    {
        private readonly IComponentContext _context;        

        public TransactionBuildContextFactory(IComponentContext context)
        {
            _context = context;            
        }

        public TransactionBuildContext Create(Network network)
        {
            return _context.Resolve<TransactionBuildContext>(new NamedParameter("network", network));
        }
    }
}
