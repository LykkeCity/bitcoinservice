using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Exceptions;
using Core.TransactionQueueWriter;
using Core.TransactionQueueWriter.Commands;
using LkeServices.Transactions;
using Lykke.Bitcoin.Contracts.Commands;
using Lykke.Cqrs;

namespace BitcoinJob.Workflow.Handlers
{
    public class CashinCommandHandler
    {
        private readonly ILykkeTransactionBuilderService _builder;
        private readonly ITransactionQueueWriter _transactionQueueWriter;
        private readonly ILog _logger;

        public CashinCommandHandler(ILykkeTransactionBuilderService builder, ITransactionQueueWriter transactionQueueWriter, ILog logger)
        {
            _builder = builder;
            _transactionQueueWriter = transactionQueueWriter;
            _logger = logger;
        }

        public async Task<CommandHandlingResult> Handle(StartCashinCommand command,
            IEventPublisher eventPublisher)
        {
            try
            {
                var transactionId = await _builder.AddTransactionId(command.Id, $"SegwitTransfer: {command.ToJson()}");

                await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.SegwitTransferToHotwallet, new SegwitTransferCommand
                {
                    SourceAddress = command.Address
                }.ToJson());
            }
            catch (BackendException ex) when (ex.Code == ErrorCode.DuplicateTransactionId)
            {
                _logger.WriteWarning(nameof(CashinCommandHandler), nameof(Handle), $"Duplicated id: {command.Id}");
            }

            return CommandHandlingResult.Ok();
        }
    }
}
