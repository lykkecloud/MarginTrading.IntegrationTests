using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.TradingHistory.Client.Models;

namespace MarginTrading.IntegrationTests.Helpers
{
    public static class TradingHistoryHelpers
    {
        public static async Task<(OrderHistoryEvent order, PositionHistoryEvent position)> 
            WaitForHistoryEvents(string correlationId)
        {
            var orderHistoryEvent = await MtCoreHelpers.WaitForOrderHistoryEvent(correlationId);
            var positionHistoryEvent = await MtCoreHelpers.WaitForPositionHistoryEvent(orderHistoryEvent.OrderSnapshot.Id);
            
            return (orderHistoryEvent, positionHistoryEvent);
        }

        public static async Task<TradeContract> EnsureTrade(string id, decimal volume, string assetPairId)
        {
            var trade = await ApiHelpers
                .GetRefitRetryPolicy<TradeContract>(t => t.Id == id
                                                         && t.Volume == volume
                                                         && t.AssetPairId == assetPairId)
                .ExecuteAsync(async ct =>
                    await ClientUtil.TradesApi.Get(id), CancellationToken.None);

            return trade;
        }

        public static async Task<DealContract> EnsureDeal(string dealId, string openTradeId, string closeTradeId = null)
        {

            var deal = await ApiHelpers
                .GetRefitRetryPolicy<DealContract>(d => d.OpenTradeId == openTradeId
                                                        && d.CloseTradeId == closeTradeId)
                .ExecuteAsync(async ct =>
                    await ClientUtil.DealsApi.ById(dealId), CancellationToken.None);

            return deal;
        }
    }
}
