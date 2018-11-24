using System.Threading.Tasks;
using MarginTrading.IntegrationTests.Helpers;
using NUnit.Framework;

namespace MarginTrading.IntegrationTests.WorkflowTests
{
    [TestFixture]
    public class SettingsServiceTests
    {

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            //Create Assets
            await MtSettingsHelper.CreateAssets();
            //Create AssetPairs
            //Create Markets
            //Create ScheduleSettings
            //Create TradingConditions
            //Create TradingInstruments
            //Create TradingRoutes
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            //Remove Assets
            await MtSettingsHelper.RemoveAssets();
            //Remove AssetPairs
            //Remove Markets
            //Remove ScheduleSettings
            //Remove TradingConditions
            //Remove TradingInstruments
            //Remove TradingRoutes
        }

        [Test]
        public async Task All_Settings_Created_And_Events_Consumed()
        {
            
        }
    }
}
