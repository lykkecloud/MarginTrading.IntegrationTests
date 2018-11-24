using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Settings;
using Polly;
using AccountStatContract = MarginTrading.Backend.Contracts.Account.AccountStatContract;

namespace MarginTrading.IntegrationTests
{
    public static class MtCoreHelpers
    {
        private static readonly BehaviorSettings BehaviorSettings = 
            SettingsUtil.Settings.IntegrationTestSettings.Behavior;
        
        public static async Task<AccountStatContract> EnsureAccountState(int neededBalance = 0)
        {
            var positions = await ClientUtil.PositionsApi.ListAsync(AccountHelpers.GetDefaultAccount);
            var accountUpdatedTasks = new List<Task>();
            foreach (var openPositionContract in positions)
            {
                await ClientUtil.PositionsApi.CloseAsync(openPositionContract.Id, new PositionCloseRequest
                {
                    Originator = OriginatorTypeContract.System,
                    Comment = "Integration test cleanup",
                });
                accountUpdatedTasks.Add(
                    RabbitUtil.WaitForMessage<AccountChangedEvent>(m =>
                        m.BalanceChange?.EventSourceId == openPositionContract.Id
                        && m.BalanceChange?.ReasonType == AccountBalanceChangeReasonTypeContract.RealizedPnL));
            }
            await Task.WhenAll(accountUpdatedTasks);

            var account = await AccountHelpers.EnsureAccountState(neededBalance);

            bool Predicate(string accountId, decimal balance) => accountId == account.Id && balance == neededBalance;
            
            var accountStat = await Policy.HandleResult<AccountStatContract>(r => Predicate(r.AccountId, r.Balance))
                .WaitAndRetryAsync(BehaviorSettings.ApiCallRetries,
                    x => TimeSpan.FromMilliseconds(BehaviorSettings.ApiCallRetryPeriodMs))
                .ExecuteAsync(
                    async ct => await ClientUtil.AccountsStatApi.GetAccountStats(AccountHelpers.GetDefaultAccount),
                    CancellationToken.None);

            if (accountStat == null || !Predicate(accountStat.AccountId, accountStat.Balance))
            {
                throw new Exception($"Mt Core account [{account.Id}] balance is not correct. Needed: [{neededBalance}], current: [{accountStat?.Balance}]");
            }

            return accountStat;
        }
    }
}
