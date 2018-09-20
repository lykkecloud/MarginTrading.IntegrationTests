using System.Threading;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.IntegrationTests.Infrastructure;
using NUnit.Framework;

namespace MarginTrading.IntegrationTests.WorkflowTests.AccountManagement
{
    [SetUpFixture]
    public class InitNamespace
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            var sett = SettingsUtil.Settings.MarginTradingAccountManagement;
            var connectionString = sett.Cqrs.ConnectionString;
            var eventsExchange = $"{sett.Cqrs.EnvironmentName}.{sett.Cqrs.ContextNames.AccountsManagement}.events.exchange";
            
            RabbitUtil.ListenCqrsMessages<AccountChangedEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<DepositSucceededEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<WithdrawalSucceededEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<WithdrawalFailedEvent>(connectionString, eventsExchange);

            // todo: register other messages
            
            // RabbitMqSubscriber does not wait for a reader thread to start before returning from the Start() method
            Thread.Sleep(sett.MessagingDelay);
        }
    }
}
