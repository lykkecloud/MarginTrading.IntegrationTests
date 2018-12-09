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
using MarginTrading.IntegrationTests.Helpers;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Models;
using MarginTrading.IntegrationTests.Settings;
using NUnit.Framework;

namespace MarginTrading.IntegrationTests.WorkflowTests
{
    [TestFixture]
    public class CommissionServiceTests
    {
        private readonly IntegrationTestSettings _settings = SettingsUtil.Settings.IntegrationTestSettings;
        private RabbitMqPublisher<OrderBook> _quotePublisher;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var connectionString = _settings.Cqrs.ConnectionString;
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

            //other messages subscription
            RabbitUtil.ListenJsonMessages<OrderHistoryEvent>(connectionString, _settings.RabbitMqQueues.OrderHistory.ExchangeName);
            RabbitUtil.ListenJsonMessages<PositionHistoryEvent>(connectionString, _settings.RabbitMqQueues.PositionHistory.ExchangeName);
            
            // RabbitMqSubscriber does not wait for a reader thread to start before returning from the Start() method
            Thread.Sleep(_settings.MessagingDelay);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            //dispose listeners
            RabbitUtil.TearDown();
        }

        [TearDown]
        public async Task TearDown()
        {
            Thread.Sleep(_settings.MessagingDelay / 5); //try to wait all the messages to pass
            
            if (!RabbitUtil.EnsureMessageHistoryEmpty(out var trace))
            {
                //Assert.Inconclusive($"One of {nameof(CommissionServiceTests)} tests failed: {trace}");
            }
        }

        [Test]
        public async Task OvernightSwaps_Calculated_And_Charged()
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
            
            //6. Start overnight swap process
            var operationId = Guid.NewGuid().ToString();
            await ClientUtil.OvernightSwapApi.StartOvernightSwapProcess(operationId);
            
            //7. Ensure swaps charged to account
            await AccountHelpers.WaitForCommission(accountStat.AccountId, tradingInstrument.Instrument,
                AccountBalanceChangeReasonTypeContract.Swap);
            
            //8. Clean up
            await MtCoreHelpers.EnsureAllPositionsClosed();
        }

        [Test]
        public async Task DailyPnl_Calculated_And_Charged()
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
            
            //6. Start daily pnl process
            var operationId = Guid.NewGuid().ToString();
            await ClientUtil.DailyPnlApi.StartDailyPnlProcess(operationId, DateTime.UtcNow);
            
            //7. Ensure swaps charged to account
            await AccountHelpers.WaitForCommission(accountStat.AccountId, tradingInstrument.Instrument,
                AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL);
            
            //8. Clean up
            await MtCoreHelpers.EnsureAllPositionsClosed();
        }
    }
}
