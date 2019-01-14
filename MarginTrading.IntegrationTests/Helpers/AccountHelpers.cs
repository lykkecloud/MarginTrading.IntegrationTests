using System;
using System.Threading;
using System.Threading.Tasks;
using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Settings;

namespace MarginTrading.IntegrationTests.Helpers
{
    public static class AccountHelpers
    {
        private static readonly BehaviorSettings BehaviorSettings = 
            SettingsUtil.Settings.IntegrationTestSettings.Behavior;
        
        public static string GetClientId => BehaviorSettings.ClientId;
        public static string GetDefaultAccount => $"{BehaviorSettings.AccountIdPrefix}1";
        public static string GetAccountIdPrefix => BehaviorSettings.AccountIdPrefix;

        public static async Task<AccountContract> EnsureAccountState(decimal neededBalance = 0, string accountId = null)
        {
            var handlingAccountId = accountId ?? GetDefaultAccount;
            var account = await ClientUtil.AccountsApi.GetById(handlingAccountId);
            if (account == null)
            {
                account = await ClientUtil.AccountsApi.Create(new CreateAccountRequest
                {
                    ClientId = GetClientId,
                    AccountId = handlingAccountId,
                    BaseAssetId = "EUR",
                });
            }

            if (account.Balance != neededBalance)
            {
                await ChargeManually(neededBalance - account.Balance, accountId);
                account = await ClientUtil.AccountsApi.GetById(accountId);
                if (account.Balance != neededBalance) 
                    throw new Exception($"{nameof(EnsureAccountState)} failed to set needed balance for [{handlingAccountId}]");
            }

            if (account.IsDisabled)
            {
                account = await ClientUtil.AccountsApi.Change(handlingAccountId, new ChangeAccountRequest
                {
                    IsDisabled = false,
                });
            }

            return account;
        }

        public static async Task<AccountContract> ChargeManually(decimal delta, string accountId = null)
        {
            var handlingAccountId = accountId ?? GetDefaultAccount;

            var operationId = await ClientUtil.AccountsApi.BeginChargeManually(handlingAccountId,
                new AccountChargeManuallyRequest
                {
                    OperationId = Guid.NewGuid().ToString(),
                    AmountDelta = delta,
                    Reason = "integration tests",
                    ReasonType = AccountBalanceChangeReasonTypeContract.Manual,
                    EventSourceId = Guid.NewGuid().ToString(),
                });

            var accountChangedEvent =
                await RabbitUtil.WaitForMessage<AccountChangedEvent>(m => m.BalanceChange?.Id == operationId);

            return accountChangedEvent.Account;
        }

        public static Task<AccountContract> GetAccount(string accountId = null)
        {
            return ClientUtil.AccountsApi.GetById(accountId ?? GetDefaultAccount);
        }

        public static async Task WaitForCommission(string accountId, string assetPairId,
            AccountBalanceChangeReasonTypeContract type, string orderId = null)
        {
            await RabbitUtil.WaitForMessage<ChangeBalanceCommand>(m =>
                m.AssetPairId == assetPairId
                && m.AccountId == accountId
                && m.ReasonType == type);
            
            await RabbitUtil.WaitForMessage<AccountChangedEvent>(m =>
                m.Account.Id == accountId
                && m.EventType == AccountChangedEventTypeContract.BalanceUpdated
                && m.BalanceChange?.Instrument == assetPairId
                && m.BalanceChange?.ReasonType == type
                && (string.IsNullOrEmpty(orderId) || m.BalanceChange?.EventSourceId == orderId));
        }
    }
}
