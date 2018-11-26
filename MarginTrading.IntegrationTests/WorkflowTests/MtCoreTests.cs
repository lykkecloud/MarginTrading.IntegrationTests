using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.IntegrationTests.Helpers;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Settings;
using MarginTrading.TradingHistory.Client.Models;
using NUnit.Framework;
using OrderDirectionContract = MarginTrading.Backend.Contracts.Orders.OrderDirectionContract;
using OrderStatusContract = MarginTrading.TradingHistory.Client.Models.OrderStatusContract;
using OrderTypeContract = MarginTrading.Backend.Contracts.Orders.OrderTypeContract;
using OriginatorTypeContract = MarginTrading.Backend.Contracts.Orders.OriginatorTypeContract;
using PositionDirectionContract = MarginTrading.Backend.Contracts.Positions.PositionDirectionContract;

namespace MarginTrading.IntegrationTests.WorkflowTests
{
    [TestFixture]
    public class MtCoreTests
    {
        private readonly IntegrationTestSettings _settings = SettingsUtil.Settings.IntegrationTestSettings;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var connectionString = _settings.Cqrs.ConnectionString;
            var accountEventsExchange = 
                $"{_settings.Cqrs.EnvironmentName}.{_settings.Cqrs.ContextNames.AccountsManagement}.events.exchange";
            var accountCommandsExchange = 
                $"{_settings.Cqrs.EnvironmentName}.{_settings.Cqrs.ContextNames.AccountsManagement}.commands.exchange";
            
            //cqrs messages subscription
            RabbitUtil.ListenCqrsMessages<AccountChangedEvent>(connectionString, accountEventsExchange);
            RabbitUtil.ListenCqrsMessages<ChangeBalanceCommand>(connectionString, accountCommandsExchange);

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
            
            //1. Market order placed
            var correlationId = Guid.NewGuid().ToString("N");
            var volume = 42;
            var orderRequest = new OrderPlaceRequest
            {
                AccountId = accountStat.AccountId,
                InstrumentId = tradingInstrument.Instrument,
                Direction = OrderDirectionContract.Buy,
                Type = OrderTypeContract.Market,
                Originator = OriginatorTypeContract.Investor,
                Volume = volume,
                CorrelationId = correlationId,
            };
            await ClientUtil.OrdersApi.PlaceAsync(orderRequest);
            
            //2. Order & position history events generated
            var orderHistory = await RabbitUtil.WaitForMessage<OrderHistoryEvent>(m => 
                m.OrderSnapshot.CorrelationId == correlationId);
            await RabbitUtil.WaitForMessage<PositionHistoryEvent>(m =>
                m.EventType == PositionHistoryTypeContract.Open
                && m.PositionSnapshot.Volume == volume
                && m.PositionSnapshot.Direction == PositionDirectionContract.Long
                && m.PositionSnapshot.AccountId == AccountHelpers.GetDefaultAccount
                && m.PositionSnapshot.AssetPairId == tradingInstrument.Instrument);
            
            //3. Commission is calculated and change balance command sent
            await RabbitUtil.WaitForMessage<ChangeBalanceCommand>(m =>
                m.AssetPairId == tradingInstrument.Instrument
                && m.AccountId == AccountHelpers.GetDefaultAccount
                && m.ReasonType == AccountBalanceChangeReasonTypeContract.Commission);
            
            //4. Commission charged on account
            await RabbitUtil.WaitForMessage<AccountChangedEvent>(m =>
                m.Account.Id == AccountHelpers.GetDefaultAccount
                && m.EventType == AccountChangedEventTypeContract.BalanceUpdated
                && m.BalanceChange?.Instrument == tradingInstrument.Instrument
                && m.BalanceChange?.ReasonType == AccountBalanceChangeReasonTypeContract.Commission);

            //5. Position is on place
            var accountPositions = await ClientUtil.PositionsApi.ListAsync(AccountHelpers.GetDefaultAccount);
            accountPositions.Should().Match((List<OpenPositionContract> pl) => pl.Any(p =>
                p != null
                && p.CurrentVolume == volume
                && p.Direction == PositionDirectionContract.Long
                && p.AccountId == accountStat.AccountId
                && p.AssetPairId == tradingInstrument.Instrument));
            
            //6. Trading history was written
            var orderId = orderHistory.OrderSnapshot.Id;
            var orderExecHistory = (await ClientUtil.OrderEventsApi.OrderById(orderId, OrderStatusContract.Executed))
                .First();
            orderExecHistory.Should().Match((OrderEventContract o) =>
                o.Id == orderId
                && o.Status == OrderStatusContract.Executed
                && o.Volume == volume
                && o.Type == TradingHistory.Client.Models.OrderTypeContract.Market
                && o.Direction == TradingHistory.Client.Models.OrderDirectionContract.Buy);
            
            //7. Clean up
            //await MtCoreHelpers.EnsureAllPositionsClosed();
        }

        [Test]
        public async Task LimitOrder_Match_Creates_Position()
        {
            //1. Set initial fake exchange connector rates
            
            //2. Create limit order
            
            //3. Change rates to match the order
            
            //4. Check limit order executed
            
            //5. Check position is created
            
            //6. Trading history was written
            
            //7. Clean up
        }

        [Test]
        public async Task LimitOrder_SlTp_Executes()
        {
            //1. Set initial fake exchange connector rates
            
            //2. Create limit order with SL or TP
            
            //3. Change rates to match the order
            
            //4. Check limit order executed
            
            //5. Check position is created
            
            //6. Change rates that SL or TP is executed
            
            //7. Check position was closed
            
            //8. Trading history was written
            
            //9. Clean up
        }

        [Test]
        public async Task LimitOrder_Ts_Moves_Sl_Price()
        {
            //1. Set initial fake exchange connector rates
            
            //2. Create limit order with SL or TP
            
            //3. Change rates to match the order
            
            //4. Check limit order executed
            
            //5. Check position is created
            
            //6. Change rates to move TS
            
            //7. Check SL price has changed
            
            //8. Trading history was written
            
            //9. Clean up
        }

        [Test]
        public async Task PositionClose_Events_Commissions_AccountCharged()
        {
            //1. Place market order
            
            //2. Order & position history events generated
            
            //3. Commission is calculated and change balance command sent
            
            //4. Commission charged on account
            
            //5. Trading history was written
            
            //6. Clean up
        }

        [Test]
        public async Task ClosePositionGroup_Liquidation_Succeeded()
        {
            //1. Place market orders
            
            //2. Order & position history events generated
            
            //3. Trading history was written
            
            //4. Call close position group
            
            //5. Await liquidation succeeds
        }

        [Test]
        public async Task OnBehalf_OrderExecution_Commissions_AccountCharged()
        {
            //1. Place market order WithOnBehalfFees, close opened position
            
            //2. Order & position history events generated
            
            //3. Commission is calculated and change balance command sent
            
            //4. Commission charged on account
            
            //5. Trading history was written
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
