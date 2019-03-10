using System;
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
    [SetUpFixture]
    public class SetUp
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

            try
            {
                await CreateAll();
            }
            catch (Exception) // for the case of dirty state
            {
                await RemoveAll();
                await CreateAll();
            }
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await RemoveAll();
        }

        private static async Task CreateAll()
        {
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

        private static async Task RemoveAll()
        {
            await MtCoreHelpers.EnsureAllPositionsClosed();

            await SqlHelper.ClearHistoryTables();
            
            //todo gracefully handle exceptions on delete
            //Remove TradingInstruments
            await MtSettingsHelper.RemoveTradingInstruments();
            //Remove ScheduleSettings
            await MtSettingsHelper.RemoveScheduleSettings();
            //Remove AssetPairs
            await MtSettingsHelper.RemoveAssetPairs();
            //Remove Markets
            await MtSettingsHelper.RemoveMarkets();
            //Remove Assets
            await MtSettingsHelper.RemoveAssets();
            //Remove TradingRoutes
            await MtSettingsHelper.RemoveTradingRoutes();
        }

//        [Test]
//        public async Task All_Settings_Created_And_Events_Consumed()
//        {
//            //testing is done via OneTime handlers
//        }
        
        //todo On each item creation/modification event should be generated, and consumed by MTCore.
        //todo add test ScheduleSettings compiled list
        //todo add test ScheduleSettings -> backend compiled correctly
    }
}
