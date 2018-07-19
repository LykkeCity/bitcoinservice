using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BitcoinJob.Workflow.Commands;
using Common;
using Common.Log;
using Core.Exceptions;
using Core.OpenAssets;
using Core.Repositories.Assets;
using Core.Repositories.MultipleCashouts;
using Core.TransactionQueueWriter;
using Core.TransactionQueueWriter.Commands;
using LkeServices.Transactions;
using Lykke.Cqrs;

namespace BitcoinJob.Workflow.Handlers
{
    public class CashoutCommandHandler
    {
        private readonly ILykkeTransactionBuilderService _builder;
        private readonly ITransactionQueueWriter _transactionQueueWriter;
        private readonly ICashoutRequestRepository _cashoutRequestRepository;
        private readonly CachedDataDictionary<string, IAssetSetting> _assetSettingCache;
        private readonly ILog _logger;

        public CashoutCommandHandler(
            ILykkeTransactionBuilderService builder,
            ITransactionQueueWriter transactionQueueWriter,
            ICashoutRequestRepository cashoutRequestRepository,
            CachedDataDictionary<string, IAssetSetting> assetSettingCache, 
            ILog logger)
        {
            _builder = builder;
            _transactionQueueWriter = transactionQueueWriter;
            _cashoutRequestRepository = cashoutRequestRepository;
            _assetSettingCache = assetSettingCache;
            _logger = logger;
        }

        public async Task<CommandHandlingResult> Handle(StartCashoutCommand command,
            IEventPublisher eventPublisher)
        {
            var address = command.Address.Trim('\n', ' ', '\t');

            try
            {
                var transactionId = await _builder.AddTransactionId(command.Id, $"Cashout: {command.ToJson()}");

                if (OpenAssetsHelper.IsBitcoin(command.AssetId))
                    await _cashoutRequestRepository.CreateCashoutRequest(transactionId, command.Amount, address);
                else
                {
                    var assetSetting = await _assetSettingCache.GetItemAsync(command.AssetId);

                    var hotWallet = !string.IsNullOrEmpty(assetSetting.ChangeWallet)
                        ? assetSetting.ChangeWallet
                        : assetSetting.HotWallet;

                    await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.Transfer, new TransferCommand
                    {
                        Amount = command.Amount,
                        SourceAddress = hotWallet,
                        Asset = command.AssetId,
                        DestinationAddress = command.Address
                    }.ToJson());
                }

            }
            catch (BackendException ex) when (ex.Code == ErrorCode.DuplicateTransactionId)
            {
                _logger.WriteWarning(nameof(CashinCommandHandler), nameof(Handle), $"Duplicated id: {command.Id}");
            }

            return CommandHandlingResult.Ok();
        }
    }
}
