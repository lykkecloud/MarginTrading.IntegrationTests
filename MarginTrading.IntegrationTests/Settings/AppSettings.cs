using JetBrains.Annotations;

namespace MarginTrading.IntegrationTests.Settings
{
    [UsedImplicitly]
    internal class AppSettings
    {
        public IntegrationTestSettings IntegrationTestSettings { get; set; }
        
        public ClientSettings AccountManagementClient { get; set; }
        
        public ClientSettings MtCoreClient { get; set; }
        
        public ClientSettings SettingsServiceClient { get; set; }
        
        public ClientSettings TradingHistoryClient { get; set; }
    }
}
