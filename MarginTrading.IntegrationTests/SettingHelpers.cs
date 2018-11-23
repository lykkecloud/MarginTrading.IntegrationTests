using System;
using System.Threading.Tasks;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Settings;
using MarginTrading.SettingsService.Contracts.TradingConditions;

namespace MarginTrading.IntegrationTests
{
    public static class SettingHelpers
    {
        private static readonly BehaviorSettings BehaviorSettings = 
            SettingsUtil.Settings.IntegrationTestSettings.Behavior;
        
        public static string GetInstrumentId => BehaviorSettings.Instrument;
        public static string GetTradingConditionId => BehaviorSettings.TradingCondition;

        public static async Task<TradingInstrumentContract> EnsureInstrumentState()
        {
            var tradingInstrument = await ClientUtil.TradingInstrumentsApi.Get(GetTradingConditionId, GetInstrumentId);

            if (tradingInstrument == null)
            {
                throw new Exception($"Trading instrument [{GetInstrumentId}] with trading condition [{GetTradingConditionId}] does not exist.");
            }

            return tradingInstrument;
        }
    }
}
