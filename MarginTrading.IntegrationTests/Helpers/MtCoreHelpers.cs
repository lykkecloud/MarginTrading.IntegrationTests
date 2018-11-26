using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Settings;
using Polly;
using AccountStatContract = MarginTrading.Backend.Contracts.Account.AccountStatContract;

namespace MarginTrading.IntegrationTests.Helpers
{
    public static class MtCoreHelpers
    {
        private static readonly BehaviorSettings BehaviorSettings = 
            SettingsUtil.Settings.IntegrationTestSettings.Behavior;
        
        public static async Task<AccountStatContract> EnsureAccountState(decimal neededBalance = 0, 
            string accountId = null, bool closeAllPositions = true)
        {
            accountId = accountId ?? AccountHelpers.GetDefaultAccount;
            
            if (closeAllPositions)
            {
                await EnsureAllPositionsClosed(accountId);
            }

            var account = await AccountHelpers.EnsureAccountState(neededBalance, accountId);

            bool Predicate(string accId, decimal balance) => accId == account.Id && balance == neededBalance;

            var accountStat = await ApiHelpers
                .GetRefitRetryPolicy<AccountStatContract>(r => !Predicate(r.AccountId, r.Balance))
                .ExecuteAsync(async ct =>
                    await ClientUtil.AccountsStatApi.GetAccountStats(accountId), CancellationToken.None);

            if (accountStat == null || !Predicate(accountStat.AccountId, accountStat.Balance))
            {
                throw new Exception($"Mt Core account [{account.Id}] balance is not correct. Needed: [{neededBalance}], current: [{accountStat?.Balance}]");
            }

            return accountStat;
        }

        public static async Task EnsureAllPositionsClosed(string accountId = null)
        {
            var positions = await ClientUtil.PositionsApi.ListAsync(accountId);
            var accountUpdatedTasks = new List<Task>();
            foreach (var openPositionContract in positions)
            {
//                await ApiHelpers.GetRefitRetryPolicy().ExecuteAsync(async ct =>
//                    await ClientUtil.PositionsApi.CloseAsync(openPositionContract.Id, new PositionCloseRequest
//                    {
//                        Originator = OriginatorTypeContract.System,
//                        Comment = $"Integration test {nameof(EnsureAllPositionsClosed)}",
//                    }), CancellationToken.None);//this retries just to check.. seems it didn't helped
                await ClientUtil.PositionsApi.CloseAsync(openPositionContract.Id, new PositionCloseRequest
                {
                    Originator = OriginatorTypeContract.System,
                    Comment = $"Integration test {nameof(EnsureAllPositionsClosed)}",
                });
                
                accountUpdatedTasks.Add(
                    new Task(async () =>
                    {
                        var positionHistoryEvent = await RabbitUtil.WaitForMessage<PositionHistoryEvent>(m =>
                            m.Deal?.PositionId == openPositionContract.Id);
                        
                        await RabbitUtil.WaitForMessage<AccountChangedEvent>(m =>
                            m.BalanceChange?.EventSourceId == positionHistoryEvent.Deal.DealId
                            && m.BalanceChange?.ReasonType == AccountBalanceChangeReasonTypeContract.RealizedPnL);
                        
                        //todo await commissions charged + check onBehalf from AdditionalInfo
                        
                    }));
            }
            await Task.WhenAll(accountUpdatedTasks);
        }
    }
}
