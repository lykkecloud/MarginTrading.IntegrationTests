using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Settings;
using Polly;

namespace MarginTrading.IntegrationTests
{
    public static class MtCoreHelpers
    {
        private static readonly BehaviorSettings BehaviorSettings = 
            SettingsUtil.Settings.IntegrationTestSettings.Behavior;
        
        public static async Task<AccountStatContract> EnsureAccountState(int neededBalance = 0)
        {
            var positions = await ClientUtil.PositionsApi.ListAsync(AccountHelpers.GetDefaultAccount);
            foreach (var openPositionContract in positions)
            {
                await ClientUtil.PositionsApi.CloseAsync(openPositionContract.Id, new PositionCloseRequest
                {
                    Originator = OriginatorTypeContract.System,
                    Comment = "Integration test cleanup",
                });
            }

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
                throw new Exception($"Mt Core account [{account.Id}] balance state is not correct. Needed: [{neededBalance}], current: [{accountStat?.Balance}]");
            }

            return accountStat;
        }
    }
}
