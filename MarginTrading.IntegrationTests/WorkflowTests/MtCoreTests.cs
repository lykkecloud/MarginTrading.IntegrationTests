using System;
using System.Threading;
using System.Threading.Tasks;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.IntegrationTests.Infrastructure;
using NUnit.Framework;

namespace MarginTrading.IntegrationTests.WorkflowTests
{
    [TestFixture]
    public class MtCoreTests
    {

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var sett = SettingsUtil.Settings.IntegrationTestSettings;
            var connectionString = sett.Cqrs.ConnectionString;
            var eventsExchange = $"{sett.Cqrs.EnvironmentName}.{sett.Cqrs.ContextNames.AccountsManagement}.events.exchange";
            
            //cqrs messages subscription
            RabbitUtil.ListenCqrsMessages<AccountChangedEvent>(connectionString, eventsExchange);

            //other messages subscription
            RabbitUtil.ListenJsonMessages<OrderHistoryEvent>(connectionString, sett.RabbitMqQueues.OrderHistory.ExchangeName);
            
            // RabbitMqSubscriber does not wait for a reader thread to start before returning from the Start() method
            Thread.Sleep(sett.MessagingDelay);
        }

        public async Task MarketOrder_Executed_WithCommission()
        {
            var orderRequest = new OrderPlaceRequest
            {
                AccountId = AccountHelpers.AccountId,
                InstrumentId = ,
                Direction = OrderDirectionContract.Buy,
                Type = OrderTypeContract.Market,
                Originator = OriginatorTypeContract.Investor,
                Volume = 42,
                CorrelationId = Guid.NewGuid().ToString("N"),
            };
            await ClientUtil.OrdersApi.PlaceAsync(orderRequest);

            RabbitUtil.WaitForMessage<OrderHistoryEvent>(m => m.OrderSnapshot.Id == orderId);
            
            //wait for commission to be calculated and charged
            await Task.WhenAll(
                RabbitUtil.WaitForMessage<AccountChangedEvent>(m => m.BalanceChange.Id == operationId));
        }
    }
}
