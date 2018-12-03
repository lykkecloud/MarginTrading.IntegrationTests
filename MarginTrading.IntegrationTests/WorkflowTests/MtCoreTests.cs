using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lykke.RabbitMqBroker.Publisher;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Workflow.Liquidation;
using MarginTrading.Backend.Contracts.Workflow.Liquidation.Events;
using MarginTrading.IntegrationTests.Helpers;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Models;
using MarginTrading.IntegrationTests.Settings;
using MarginTrading.TradingHistory.Client.Models;
using NUnit.Framework;
using DealContract = MarginTrading.TradingHistory.Client.Models.DealContract;
using OrderContract = MarginTrading.Backend.Contracts.Orders.OrderContract;
using OrderDirectionContract = MarginTrading.Backend.Contracts.Orders.OrderDirectionContract;
using OrderStatusContract = MarginTrading.TradingHistory.Client.Models.OrderStatusContract;
using OrderTypeContract = MarginTrading.Backend.Contracts.Orders.OrderTypeContract;
using OriginatorTypeContract = MarginTrading.Backend.Contracts.Orders.OriginatorTypeContract;
using PositionCloseReasonContract = MarginTrading.Backend.Contracts.TradeMonitoring.PositionCloseReasonContract;
using PositionDirectionContract = MarginTrading.Backend.Contracts.Positions.PositionDirectionContract;

namespace MarginTrading.IntegrationTests.WorkflowTests
{
    [TestFixture]
    public class MtCoreTests
    {
        private readonly IntegrationTestSettings _settings = SettingsUtil.Settings.IntegrationTestSettings;
        private RabbitMqPublisher<OrderBook> _quotePublisher;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var connectionString = _settings.Cqrs.ConnectionString;
            var mtCoreEventsExchange = 
                $"{_settings.Cqrs.EnvironmentName}.{_settings.Cqrs.ContextNames.TradingEngine}.events.exchange";
            var accountEventsExchange = 
                $"{_settings.Cqrs.EnvironmentName}.{_settings.Cqrs.ContextNames.AccountsManagement}.events.exchange";
            var accountCommandsExchange = 
                $"{_settings.Cqrs.EnvironmentName}.{_settings.Cqrs.ContextNames.AccountsManagement}.commands.exchange";

            _quotePublisher = RabbitUtil.GetProducer<OrderBook>(new RabbitConnectionSettings
            {
                ExchangeName = SettingsUtil.Settings.IntegrationTestSettings.RabbitMqQueues.FakeOrderBook.ExchangeName,
                ConnectionString = connectionString,
            }, isDurable: false, isJson: false, isTopic: false);
            
            //cqrs messages subscription
            RabbitUtil.ListenCqrsMessages<AccountChangedEvent>(connectionString, accountEventsExchange);
            RabbitUtil.ListenCqrsMessages<ChangeBalanceCommand>(connectionString, accountCommandsExchange);
            RabbitUtil.ListenCqrsMessages<LiquidationFinishedEvent>(connectionString, mtCoreEventsExchange);

            //other messages subscription
            RabbitUtil.ListenJsonMessages<OrderHistoryEvent>(connectionString, _settings.RabbitMqQueues.OrderHistory.ExchangeName);
            RabbitUtil.ListenJsonMessages<PositionHistoryEvent>(connectionString, _settings.RabbitMqQueues.PositionHistory.ExchangeName);
            
            // RabbitMqSubscriber does not wait for a reader thread to start before returning from the Start() method
            Thread.Sleep(_settings.MessagingDelay);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            //await MtCoreHelpers.EnsureAccountState();
            
            //dispose listeners
            RabbitUtil.TearDown();
        }

        [Test]
        public async Task MarketOrder_Executed_WithCommission()
        {
            //0. Make preparations
            var accountStat = await MtCoreHelpers.EnsureAccountState(neededBalance: 100);
            var tradingInstrument = await SettingHelpers.EnsureInstrumentState();
            await _quotePublisher.ProduceAsync(QuotesData.GetNormalOrderBook());
            
            //1. Market order placed
            var orderRequest = await MtCoreHelpers.PlaceOrder(accountStat.AccountId, tradingInstrument.Instrument,
                OrderTypeContract.Market, 42);
            
            //2. Order & position history events generated
            var historyEvents = await TradingHistoryHelpers.WaitForHistoryEvents(orderRequest.CorrelationId);
            
            //3. Commission is calculated, change balance command sent and commission charged to account
            await AccountHelpers.WaitForCommission(AccountHelpers.GetDefaultAccount, tradingInstrument.Instrument,
                AccountBalanceChangeReasonTypeContract.Commission);

            //4. Position is on place
            var accountPositions = await ClientUtil.PositionsApi.ListAsync(AccountHelpers.GetDefaultAccount);
            accountPositions.Should().Match((List<OpenPositionContract> pl) => pl.Any(p =>
                p != null
                && p.CurrentVolume == orderRequest.Volume
                && p.Direction == PositionDirectionContract.Long
                && p.AccountId == accountStat.AccountId
                && p.AssetPairId == tradingInstrument.Instrument));
            
            //5. Trading history was written
            await TradingHistoryHelpers.EnsureTrade(historyEvents.order.OrderSnapshot.Id, 
                orderRequest.Volume, tradingInstrument.Instrument);
            
            //6. Clean up
            await MtCoreHelpers.EnsureAllPositionsClosed();
        }

        [Test]
        public async Task LimitOrder_Match_Creates_Position()
        {
            //0. Make preparations
            var accountStat = await MtCoreHelpers.EnsureAccountState(neededBalance: 100);
            var tradingInstrument = await SettingHelpers.EnsureInstrumentState();
            
            //1. Set initial fake exchange connector rates
            await _quotePublisher.ProduceAsync(QuotesData.GetNormalOrderBook());

            //2. Create limit order
            var orderRequest = await MtCoreHelpers.PlaceOrder(accountStat.AccountId, tradingInstrument.Instrument,
                OrderTypeContract.Limit, 30, 9);

            //3. Change rates to match the order
            await _quotePublisher.ProduceAsync(QuotesData.GetLowerOrderBook());

            //4. Check limit order executed
            var historyEvents = await TradingHistoryHelpers.WaitForHistoryEvents(orderRequest.CorrelationId);

            //5. Check position is created
            await MtCoreHelpers.EnsurePosition(historyEvents.position.PositionSnapshot.Id, 
                orderRequest.Volume);

            //6. Trade history was written
            var openTrade = await TradingHistoryHelpers.EnsureTrade(historyEvents.order.OrderSnapshot.Id, 
                orderRequest.Volume, tradingInstrument.Instrument);

            //7. Clean up
            await MtCoreHelpers.EnsureAllPositionsClosed();
        }

        [Test]
        public async Task LimitOrder_StopLoss_Executes()
        {
            //0. Make preparations
            var accountStat = await MtCoreHelpers.EnsureAccountState(neededBalance: 100);
            var tradingInstrument = await SettingHelpers.EnsureInstrumentState();
            
            //1. Set initial fake exchange connector rates
            await _quotePublisher.ProduceAsync(QuotesData.GetNormalOrderBook());
            
            //2. Create limit order with SL
            var orderRequest = await MtCoreHelpers.PlaceOrder(accountStat.AccountId, tradingInstrument.Instrument,
                OrderTypeContract.Limit, 30, price: 9, sl: 6M);
            
            //3. Change rates to match the order
            await _quotePublisher.ProduceAsync(QuotesData.GetLowerOrderBook());
            
            //4. Check limit order executed
            var openHistoryEvents = await TradingHistoryHelpers.WaitForHistoryEvents(orderRequest.CorrelationId);

            //5. Check position is created
            var position = await MtCoreHelpers.EnsurePosition(openHistoryEvents.position.PositionSnapshot.Id, 
                orderRequest.Volume);
            
            //6. Change rates that SL is executed
            await _quotePublisher.ProduceAsync(QuotesData.GetMuchLowerOrderBook());
            
            //7. Check position was closed
            var positionCloseHistoryEvent = await MtCoreHelpers.WaitForPositionHistoryEvent(position.Id,
                PositionHistoryTypeContract.Close, PositionCloseReasonContract.StopLoss);
            await MtCoreHelpers.WaitForOrderHistoryEventByOrderId(positionCloseHistoryEvent.Deal.OpenTradeId);
            
            var currentPositions = await ClientUtil.PositionsApi.ListAsync(AccountHelpers.GetDefaultAccount);
            currentPositions.Should().NotContain(p => p.Id == position.Id);
            
            //8. Trading history was written
            var openTrade = await TradingHistoryHelpers.EnsureTrade(positionCloseHistoryEvent.Deal.OpenTradeId, 
                orderRequest.Volume, tradingInstrument.Instrument);
            var closeTrade = await TradingHistoryHelpers.EnsureTrade(positionCloseHistoryEvent.Deal.CloseTradeId, 
                orderRequest.Volume, tradingInstrument.Instrument);
            
            //9. Deal history was written
            await TradingHistoryHelpers.EnsureDeal(positionCloseHistoryEvent.Deal.DealId, openTrade.Id, closeTrade.Id);
        }

        [Test]
        public async Task LimitOrder_TakeProfit_Executes()
        {
            //0. Make preparations
            var accountStat = await MtCoreHelpers.EnsureAccountState(neededBalance: 100);
            var tradingInstrument = await SettingHelpers.EnsureInstrumentState();
            
            //1. Set initial fake exchange connector rates
            await _quotePublisher.ProduceAsync(QuotesData.GetNormalOrderBook());
            
            //2. Create limit order with TP
            var orderRequest = await MtCoreHelpers.PlaceOrder(accountStat.AccountId, tradingInstrument.Instrument,
                OrderTypeContract.Limit, 30, price: 9, tp: 12M);
            
            //3. Change rates to match the order
            await _quotePublisher.ProduceAsync(QuotesData.GetLowerOrderBook());
            
            //4. Check limit order executed
            var historyEvents = await TradingHistoryHelpers.WaitForHistoryEvents(orderRequest.CorrelationId);

            //5. Check position is created
            var position = await MtCoreHelpers.EnsurePosition(historyEvents.position.PositionSnapshot.Id, 
                orderRequest.Volume);
            
            //6. Change rates that TP is executed
            await _quotePublisher.ProduceAsync(QuotesData.GetHigherOrderBook());
            
            //7. Check position was closed
            var positionCloseHistoryEvent = await MtCoreHelpers.WaitForPositionHistoryEvent(position.Id,
                PositionHistoryTypeContract.Close, PositionCloseReasonContract.TakeProfit);
            await MtCoreHelpers.WaitForOrderHistoryEventByOrderId(positionCloseHistoryEvent.Deal.OpenTradeId);

            var currentPositions = await ClientUtil.PositionsApi.ListAsync(AccountHelpers.GetDefaultAccount);
            currentPositions.Should().NotContain(p => p.Id == position.Id);
            
            //8. Trading history was written
            var openTrade = await TradingHistoryHelpers.EnsureTrade(positionCloseHistoryEvent.Deal.OpenTradeId, 
                orderRequest.Volume, tradingInstrument.Instrument);
            var closeTrade = await TradingHistoryHelpers.EnsureTrade(positionCloseHistoryEvent.Deal.CloseTradeId, 
                orderRequest.Volume, tradingInstrument.Instrument);
            
            //9. Deal history was written
            await TradingHistoryHelpers.EnsureDeal(positionCloseHistoryEvent.Deal.DealId, openTrade.Id, closeTrade.Id);
        }

        [Test]
        public async Task LimitOrder_Ts_Moves_Sl_Price()
        {
            //0. Make preparations
            var accountStat = await MtCoreHelpers.EnsureAccountState(neededBalance: 100);
            var tradingInstrument = await SettingHelpers.EnsureInstrumentState();
            
            //1. Set initial fake exchange connector rates
            await _quotePublisher.ProduceAsync(QuotesData.GetNormalOrderBook());
            
            //2. Create limit order with SL
            var orderRequest = await MtCoreHelpers.PlaceOrder(accountStat.AccountId, tradingInstrument.Instrument,
                OrderTypeContract.Limit, 30, price: 9, sl: 6M, isTrailing: true);
            
            //3. Change rates to match the order
            await _quotePublisher.ProduceAsync(QuotesData.GetLowerOrderBook());
            
            //4. Check limit order executed
            var historyEvents = await TradingHistoryHelpers.WaitForHistoryEvents(orderRequest.CorrelationId);

            //5. Check position is created
            await MtCoreHelpers.EnsurePosition(historyEvents.position.PositionSnapshot.Id, orderRequest.Volume);
            
            //6. Change rates to move TS
            await _quotePublisher.ProduceAsync(QuotesData.GetHigherOrderBook());
            
            //7. Check SL price has changed
            var orders = await ClientUtil.OrdersApi.ListAsync(AccountHelpers.GetDefaultAccount,
                parentPositionId: historyEvents.position.PositionSnapshot.Id);
            orders.FirstOrDefault(o => o.Type == OrderTypeContract.TrailingStop).Should().Match(
                (OrderContract oc) => oc.ParentOrderId == historyEvents.order.OrderSnapshot.Id
                                      && oc.ExpectedOpenPrice == 6M);

            //8. Trading history was written
            await TradingHistoryHelpers.EnsureTrade(historyEvents.order.OrderSnapshot.Id, orderRequest.Volume,
                tradingInstrument.Instrument);
            
            //9. Clean up
            await MtCoreHelpers.EnsureAllPositionsClosed();
        }

        [Test]
        public async Task OnBehalf_OrderExecution_Commissions_AccountCharged()
        {
            //0. Make preparations
            var accountStat = await MtCoreHelpers.EnsureAccountState(neededBalance: 100);
            var tradingInstrument = await SettingHelpers.EnsureInstrumentState();
            await _quotePublisher.ProduceAsync(QuotesData.GetNormalOrderBook());
            
            //1. Place market order WithOnBehalfFees
            var orderRequest = await MtCoreHelpers.PlaceOrder(accountStat.AccountId, tradingInstrument.Instrument,
                OrderTypeContract.Market, 42, withOnBehalf: true);
            
            //2. Order & position history events generated
            var openHistoryEvents = await TradingHistoryHelpers.WaitForHistoryEvents(orderRequest.CorrelationId);
            
            //3. Close opened position
            await ClientUtil.PositionsApi.CloseAsync(openHistoryEvents.position.PositionSnapshot.Id);
            var positionCloseHistoryEvent = await MtCoreHelpers.WaitForPositionHistoryEvent(
                openHistoryEvents.position.PositionSnapshot.Id, PositionHistoryTypeContract.Close);
            await MtCoreHelpers.WaitForOrderHistoryEventByOrderId(positionCloseHistoryEvent.Deal.OpenTradeId);

            //4. Commission is calculated, change balance command sent and commission charged to account
            await AccountHelpers.WaitForCommission(AccountHelpers.GetDefaultAccount, tradingInstrument.Instrument,
                AccountBalanceChangeReasonTypeContract.Commission);
            await AccountHelpers.WaitForCommission(AccountHelpers.GetDefaultAccount, tradingInstrument.Instrument,
                AccountBalanceChangeReasonTypeContract.OnBehalf);
            
            //5. Trading history was written
            var openTrade = await TradingHistoryHelpers.EnsureTrade(positionCloseHistoryEvent.Deal.OpenTradeId, 
                orderRequest.Volume, tradingInstrument.Instrument);
            var closeTrade = await TradingHistoryHelpers.EnsureTrade(positionCloseHistoryEvent.Deal.CloseTradeId, 
                orderRequest.Volume, tradingInstrument.Instrument);
            
            //6. Deal history was written
            await TradingHistoryHelpers.EnsureDeal(positionCloseHistoryEvent.Deal.DealId, openTrade.Id, closeTrade.Id);
        }

        [Test]
        public async Task ClosePositionGroup_Liquidation_Succeeded()
        {
            //0. Make preparations
            var accountStat = await MtCoreHelpers.EnsureAccountState(neededBalance: 100);
            var tradingInstrument = await SettingHelpers.EnsureInstrumentState();
            
            //1. Set initial fake exchange connector rates
            await _quotePublisher.ProduceAsync(QuotesData.GetNormalOrderBook());
            
            //2. Place market orders
            var orderRequest1 = await MtCoreHelpers.PlaceOrder(accountStat.AccountId, tradingInstrument.Instrument,
                OrderTypeContract.Market, 42);
            var orderRequest2 = await MtCoreHelpers.PlaceOrder(accountStat.AccountId, tradingInstrument.Instrument,
                OrderTypeContract.Market, 12.1M);
            
            //2. Order & position history events generated
            var historyEvents1 = await TradingHistoryHelpers.WaitForHistoryEvents(orderRequest1.CorrelationId);
            var historyEvents2 = await TradingHistoryHelpers.WaitForHistoryEvents(orderRequest2.CorrelationId);
            
            //3. Open trading history was written
            var openTrade1 = await TradingHistoryHelpers.EnsureTrade(historyEvents1.order.OrderSnapshot.Id, 
                orderRequest1.Volume, orderRequest1.InstrumentId);
            var openTrade2 = await TradingHistoryHelpers.EnsureTrade(historyEvents2.order.OrderSnapshot.Id, 
                orderRequest2.Volume, orderRequest2.InstrumentId);

            //4. Call close position group
            var correlationId = Guid.NewGuid().ToString();
            await ClientUtil.PositionsApi.CloseGroupAsync(tradingInstrument.Instrument, accountStat.AccountId,
                request: new PositionCloseRequest
                {
                    Comment = $"{nameof(MtCoreTests)}-{nameof(ClosePositionGroup_Liquidation_Succeeded)}",
                    CorrelationId = correlationId,
                });

            //5. Await liquidation succeeds
            var liquidationFinishedEvent = await RabbitUtil.WaitForMessage<LiquidationFinishedEvent>(
                lfe => lfe.OperationId == correlationId);
            liquidationFinishedEvent.Should().Match((LiquidationFinishedEvent lfe) =>
                lfe.LiquidationType == LiquidationTypeContract.Forced
                && lfe.OpenPositionsRemainingOnAccount == 0
                && lfe.LiquidatedPositionIds.SequenceEqual(new [] { openTrade1.PositionId, openTrade2.PositionId }));
            
            //6. Ensure positions closed
            var currentPositions = await ClientUtil.PositionsApi.ListAsync(AccountHelpers.GetDefaultAccount);
            currentPositions.Should().BeEmpty();
            
            var positionCloseHistoryEvent1 = await MtCoreHelpers.WaitForPositionHistoryEvent(
                historyEvents1.position.PositionSnapshot.Id, PositionHistoryTypeContract.Close);
            await MtCoreHelpers.WaitForOrderHistoryEventByOrderId(positionCloseHistoryEvent1.Deal.OpenTradeId);
            var positionCloseHistoryEvent2 = await MtCoreHelpers.WaitForPositionHistoryEvent(
                historyEvents2.position.PositionSnapshot.Id, PositionHistoryTypeContract.Close);
            await MtCoreHelpers.WaitForOrderHistoryEventByOrderId(positionCloseHistoryEvent2.Deal.OpenTradeId);
            
            //7. Close trading history was written
            var closeTrade1 = await TradingHistoryHelpers.EnsureTrade(historyEvents1.order.OrderSnapshot.Id,
                orderRequest1.Volume, orderRequest1.InstrumentId);
            var closeTrade2 = await TradingHistoryHelpers.EnsureTrade(historyEvents2.order.OrderSnapshot.Id,
                orderRequest2.Volume, orderRequest2.InstrumentId);
            
            //8. Deal history was written
            await TradingHistoryHelpers.EnsureDeal(positionCloseHistoryEvent1.Deal.DealId, openTrade1.Id, closeTrade1.Id);
            await TradingHistoryHelpers.EnsureDeal(positionCloseHistoryEvent2.Deal.DealId, openTrade2.Id, closeTrade2.Id);
        }

        [Test]
        public async Task SpecialLiquidation_PositionsLiquidated_EventsGenerated()
        {
            //0. Check if fake Gavel is on, skip test if it's not
            
            //1. Place market orders, to make process go to Special Liquidation when ClosePositionGroup called
            
            //2. Order & position history events generated
            
            //3. Trading history was written
            
            //4. Call close position group -> Special Liquidation, verify process branch
            
            //5. Await liquidation succeeds
        }
    }
}
