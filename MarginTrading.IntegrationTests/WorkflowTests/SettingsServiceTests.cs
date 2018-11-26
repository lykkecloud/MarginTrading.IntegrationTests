using System.Threading;
using System.Threading.Tasks;
using MarginTrading.IntegrationTests.Helpers;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Settings;
using MarginTrading.SettingsService.Contracts.AssetPair;
using MarginTrading.SettingsService.Contracts.Messages;
using NUnit.Framework;

namespace MarginTrading.IntegrationTests.WorkflowTests
{
    [TestFixture]
    public class SettingsServiceTests
    {
        private readonly IntegrationTestSettings _settings = SettingsUtil.Settings.IntegrationTestSettings;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var connectionString = _settings.Cqrs.ConnectionString;
            var settingsEventsExchange = 
                $"{_settings.Cqrs.EnvironmentName}.{_settings.Cqrs.ContextNames.SettingsService}.events.exchange";
            
            //cqrs messages subscription
            RabbitUtil.ListenCqrsMessages<AssetPairChangedEvent>(connectionString, settingsEventsExchange);

            //other messages subscription
            RabbitUtil.ListenJsonMessages<SettingsChangedEvent>(connectionString, _settings.RabbitMqQueues.SettingsChanged.ExchangeName);
            
            // RabbitMqSubscriber does not wait for a reader thread to start before returning from the Start() method
            Thread.Sleep(_settings.MessagingDelay);
            
            //Create Assets
            await MtSettingsHelper.CreateAssets();
            //Create Markets
            await MtSettingsHelper.CreateMarkets();
            //Create AssetPairs
            await MtSettingsHelper.CreateAssetPairs();
            //Create ScheduleSettings
            await MtSettingsHelper.CreateScheduleSettings();
            //no tests for TradingConditions 'cause no DELETE method, using testCond everywhere
            //Create TradingInstruments
            await MtSettingsHelper.CreateTradingInstruments();
            //Create TradingRoutes
            await MtSettingsHelper.CreateTradingRoutes();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            //Remove TradingInstruments
            await MtSettingsHelper.RemoveTradingInstruments();
            //Remove AssetPairs
            await MtSettingsHelper.RemoveAssetPairs();
            //Remove Markets
            await MtSettingsHelper.RemoveMarkets();
            //Remove Assets
            await MtSettingsHelper.RemoveAssets();
            //Remove ScheduleSettings
            await MtSettingsHelper.RemoveScheduleSettings();
            //Remove TradingRoutes
            await MtSettingsHelper.RemoveTradingRoutes();
        }

        [Test]
        public async Task All_Settings_Created_And_Events_Consumed()
        {
            //testing is done via OneTime handlers
        }
        
        //todo add test ScheduleSettings compiled list
        //todo add test ScheduleSettings -> backend compiled correctly
    }
}
