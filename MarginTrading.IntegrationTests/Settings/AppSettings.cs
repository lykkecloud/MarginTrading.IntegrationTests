using JetBrains.Annotations;

namespace MarginTrading.IntegrationTests.Settings
{
    [UsedImplicitly]
    internal class AppSettings
    {
        public AccountManagementSettings MarginTradingAccountManagement { get; set; }
        public AccountManagementServiceClientSettings MarginTradingAccountManagementServiceClient { get; set; }
    }
}
