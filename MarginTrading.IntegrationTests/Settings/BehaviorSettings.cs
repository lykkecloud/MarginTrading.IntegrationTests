using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;
using MarginTrading.SettingsService.Contracts.Enums;

namespace MarginTrading.IntegrationTests.Settings
{
    [UsedImplicitly]
    public class BehaviorSettings
    {
        public string ClientId { get; set; }
        
        public string AccountIdPrefix { get; set; }
        
        public string Instrument { get; set; }

        public string TradingCondition { get; set; }
        
        public string LegalEntity { get; set; }
        
        public MatchingEngineModeContract MatchingEngineMode { get; set; }

        public int ApiCallRetries { get; set; } = 3;

        public int ApiCallRetryPeriodMs { get; set; } = 300;
        
        [Optional]
        public decimal DefaultBalance { get; set; }
        
        [Optional]
        public bool BalanceResetIsEnabled { get; set; }
    }
}
