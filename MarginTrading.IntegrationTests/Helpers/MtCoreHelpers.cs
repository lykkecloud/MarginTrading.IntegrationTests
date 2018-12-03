using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.TradeMonitoring;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Settings;
using Polly;
using AccountStatContract = MarginTrading.Backend.Contracts.Account.AccountStatContract;
using OrderDirectionContract = MarginTrading.Backend.Contracts.Orders.OrderDirectionContract;

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
            accountId = accountId ?? AccountHelpers.GetDefaultAccount;
            
            var positions = await ClientUtil.PositionsApi.ListAsync(accountId);
            foreach (var openPositionContract in positions)
            {
                await ClientUtil.PositionsApi.CloseAsync(openPositionContract.Id, new PositionCloseRequest
                {
                    Originator = OriginatorTypeContract.System,
                    Comment = $"Integration test {nameof(EnsureAllPositionsClosed)}",
                });

                var positionHistoryEvent = await RabbitUtil.WaitForMessage<PositionHistoryEvent>(m =>
                    m.Deal?.PositionId == openPositionContract.Id);

                await RabbitUtil.WaitForMessage<AccountChangedEvent>(m =>
                    m.BalanceChange?.EventSourceId == positionHistoryEvent.Deal.DealId
                    && m.BalanceChange?.ReasonType == AccountBalanceChangeReasonTypeContract.RealizedPnL);

                await AccountHelpers.WaitForCommission(accountId, openPositionContract.AssetPairId,
                    AccountBalanceChangeReasonTypeContract.Commission);

                //todo check onBehalf from AdditionalInfo & await if needed
            }
        }

        public static async Task<OpenPositionContract> EnsurePosition(string id, decimal volume)
        {
            var position = await ClientUtil.PositionsApi.GetAsync(id);
            
            position.Should().Match((OpenPositionContract opc) => opc.Id == id
                                                                  && opc.CurrentVolume == volume
                                                                  && opc.Direction == PositionDirectionContract.Long);

            return position;
        }

        public static async Task<OrderPlaceRequest> PlaceOrder(string accountId, string assetPairId, 
            OrderTypeContract type, decimal volume, decimal? price = null, decimal? sl = null, decimal? tp = null, 
            bool isTrailing = false, bool withOnBehalf = false)
        {
            var orderRequest = new OrderPlaceRequest
            {
                AccountId = accountId,
                InstrumentId = assetPairId,
                Direction = OrderDirectionContract.Buy,
                Type = type,
                StopLoss = sl,
                TakeProfit = tp,
                UseTrailingStop = isTrailing,
                Originator = OriginatorTypeContract.Investor,
                Volume = volume,
                Price = price,
                CorrelationId = Guid.NewGuid().ToString("N"),
                AdditionalInfo = withOnBehalf ? "{\"WithOnBehalfFees\":true}" : null,
            };
            
            await ClientUtil.OrdersApi.PlaceAsync(orderRequest);

            return orderRequest;
        }

        public static async Task<OrderHistoryEvent> WaitForOrderHistoryEvent(string correlationId)
            => await RabbitUtil.WaitForMessage<OrderHistoryEvent>(
                m => m.Type == OrderHistoryTypeContract.Executed && m.OrderSnapshot.CorrelationId == correlationId);

        public static async Task<OrderHistoryEvent> WaitForOrderHistoryEventByOrderId(string orderId)
            => await RabbitUtil.WaitForMessage<OrderHistoryEvent>(
                m => m.Type == OrderHistoryTypeContract.Executed && m.OrderSnapshot.Id == orderId);

        public static async Task<PositionHistoryEvent> WaitForPositionHistoryEvent(string orderId,
            PositionHistoryTypeContract? eventType = null, PositionCloseReasonContract? closeReason = null)
            => await RabbitUtil.WaitForMessage<PositionHistoryEvent>(m => 
                m.PositionSnapshot?.OpenTradeId == orderId
                && (eventType == null || m.EventType == eventType)
                && (closeReason == null || m.PositionSnapshot?.CloseReason == closeReason));
    }
}
