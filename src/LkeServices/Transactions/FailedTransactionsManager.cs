using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories.Monitoring;
using Core.TransactionMonitoring;

namespace LkeServices.Transactions
{
    public interface IFailedTransactionsManager
    {
        Task InsertFailedTransaction(Guid transactionId, string hash, string error);
    }

    public class FailedTransactionsManager : IFailedTransactionsManager
    {
        private readonly IFailedTransactionRepository _failedTransactionRepository;
        private readonly IMenuBadgesRepository _menuBadgesRepository;

        public FailedTransactionsManager(IFailedTransactionRepository failedTransactionRepository,
            IMenuBadgesRepository menuBadgesRepository)
        {
            _failedTransactionRepository = failedTransactionRepository;
            _menuBadgesRepository = menuBadgesRepository;
        }

        public async Task InsertFailedTransaction(Guid transactionId, string hash, string error)
        {
            await _failedTransactionRepository.AddFailedTransaction(transactionId, hash, error);
            await UpdateBadges();
        }

        public Task<IEnumerable<IFailedTransaction>> GetAllAsync()
        {
            return _failedTransactionRepository.GetAllAsync();
        }

        private async Task UpdateBadges()
        {
            var count = (await _failedTransactionRepository.GetAllAsync()).Count();
            await _menuBadgesRepository.SaveBadgeAsync(MenuBadges.FailedTransaction, count.ToString());
        }
    }
}
