using System;
using System.Threading.Tasks;
using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.IntegrationTests.Infrastructure;

namespace MarginTrading.IntegrationTests.WorkflowTests.AccountManagement
{
    public static class TestsHelpers
    {
        public const string ClientId = "IntergationalTestsClient";
        public const string AccountId = "IntergationalTestsAccount-1";

        public static async Task<AccountContract> EnsureAccountState(decimal neededBalance = 0)
        {
            var account = await ClientUtil.AccountsApi.GetById(AccountId);
            if (account == null)
            {
                account = await ClientUtil.AccountsApi.Create(new CreateAccountRequest
                {
                    AccountId = AccountId,
                    BaseAssetId = "EUR",
                });
            }

            if (account.Balance != neededBalance)
            {
                await ChargeManually(neededBalance - account.Balance);
                account = new AccountContract(account.Id, account.ClientId, account.TradingConditionId, 
                    account.BaseAssetId, neededBalance, account.WithdrawTransferLimit, account.LegalEntity,
                    account.IsDisabled, account.ModificationTimestamp, account.IsWithdrawalDisabled);
            }

            if (account.IsDisabled)
            {
                account = await ClientUtil.AccountsApi.Change(AccountId, new ChangeAccountRequest
                {
                    IsDisabled = false,
                });
            }

            return account;
        }

        public static async Task ChargeManually(decimal delta)
        {
            var operationId = await ClientUtil.AccountsApi.BeginChargeManually(AccountId,
                new AccountChargeManuallyRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = delta,
                    Reason = "integration tests",
                    ReasonType = AccountBalanceChangeReasonTypeContract.Manual,
                    EventSourceId = Guid.NewGuid().ToString(),
                });

            await RabbitUtil.WaitForCqrsMessage<AccountChangedEvent>(m => m.BalanceChange.Id == operationId);
        }

        public static Task<AccountContract> GetAccount()
        {
            return ClientUtil.AccountsApi.GetById(AccountId);
        }
    }
}
