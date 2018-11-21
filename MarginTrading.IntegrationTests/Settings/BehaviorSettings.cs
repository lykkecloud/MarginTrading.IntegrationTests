using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.IntegrationTests.Settings
{
    [UsedImplicitly]
    public class BehaviorSettings
    {
        public string ClientId { get; set; }
        
        public string AccountIdPrefix { get; set; }
        
        [Optional]
        public decimal DefaultBalance { get; set; }
        
        [Optional]
        public bool BalanceResetIsEnabled { get; set; }
    }
}
