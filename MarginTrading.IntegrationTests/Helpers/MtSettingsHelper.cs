using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.SettingsService.Contracts.Asset;
using MarginTrading.SettingsService.Contracts.AssetPair;
using MarginTrading.SettingsService.Contracts.Enums;
using MarginTrading.SettingsService.Contracts.Messages;
using MoreLinq;

namespace MarginTrading.IntegrationTests.Helpers
{
    public static class MtSettingsHelper
    {
        
        #region Assets
        
        public static async Task CreateAssets()
        {
            await Task.WhenAll(MtSettingsData.Assets().SelectMany(asset => new[]
            {
                ClientUtil.AssetsApi.Insert(asset),
                EnsureSettingsChangedEvent(SettingsTypeContract.Asset)
            }));
        }

        public static async Task RemoveAssets()
        {
            await Task.WhenAll(MtSettingsData.Assets().SelectMany(asset => new[]
            {
                ClientUtil.AssetsApi.Delete(asset.Id),
                EnsureSettingsChangedEvent(SettingsTypeContract.Asset)
            }));
        }
        
        #endregion Assets

        #region AssetPairs
        
        public static async Task CreateAssetPairs()
        {
            await Task.WhenAll(new []
            {
                ClientUtil.AssetPairsApi.BatchInsert(MtSettingsData.AssetPairs().ToArray()),
                EnsureSettingsChangedEvent(SettingsTypeContract.AssetPair),
            }.Concat(MtSettingsData.AssetPairs().Select(ap => EnsureAssetPairChangedEvent(ap.Id))));
        }

        public static async Task RemoveAssetPairs()
        {
            await Task.WhenAll(MtSettingsData.AssetPairs().SelectMany(assetPair => new[]
            {
                ClientUtil.AssetPairsApi.Delete(assetPair.Id),
                EnsureSettingsChangedEvent(SettingsTypeContract.AssetPair),
                EnsureAssetPairChangedEvent(assetPair.Id)
            }));
        }

        private static async Task EnsureAssetPairChangedEvent(string assetPairId)
        {
            await RabbitUtil.WaitForMessage<AssetPairChangedEvent>(m => m.AssetPair?.Id == assetPairId);
        }
        
        #endregion AssetPairs

        #region Markets
        
        public static async Task CreateMarkets()
        {
            await Task.WhenAll(MtSettingsData.Markets().SelectMany(market => new[]
            {
                ClientUtil.MarketsApi.Insert(market),
                EnsureSettingsChangedEvent(SettingsTypeContract.Market)
            }));
        }

        public static async Task RemoveMarkets()
        {
            await Task.WhenAll(MtSettingsData.Markets().SelectMany(market => new[]
            {
                ClientUtil.MarketsApi.Delete(market.Id),
                EnsureSettingsChangedEvent(SettingsTypeContract.Market)
            }));
        }
        
        #endregion Markets

        #region ScheduleSettings
        
        public static async Task CreateScheduleSettings()
        {
            await Task.WhenAll(MtSettingsData.ScheduleSettings().SelectMany(ss => new[]
            {
                ClientUtil.ScheduleSettingsApi.Insert(ss),
                EnsureSettingsChangedEvent(SettingsTypeContract.ScheduleSettings)
            }));
        }

        public static async Task RemoveScheduleSettings()
        {
            await Task.WhenAll(MtSettingsData.ScheduleSettings().SelectMany(ss => new[]
            {
                ClientUtil.ScheduleSettingsApi.Delete(ss.Id),
                EnsureSettingsChangedEvent(SettingsTypeContract.ScheduleSettings)
            }));
        }

        #endregion ScheduleSettings

        #region TradingInstruments
        
        public static async Task CreateTradingInstruments()
        {
            await Task.WhenAll(MtSettingsData.TradingInstruments().SelectMany(tradingInstrument => new[]
            {
                ClientUtil.TradingInstrumentsApi.Insert(tradingInstrument),
                EnsureSettingsChangedEvent(SettingsTypeContract.TradingInstrument)
            }));
        }

        public static async Task RemoveTradingInstruments()
        {
            await Task.WhenAll(MtSettingsData.TradingInstruments().SelectMany(tradingInstrument => new[]
            {
                ClientUtil.TradingInstrumentsApi.Delete(
                    SettingsUtil.Settings.IntegrationTestSettings.Behavior.TradingCondition, tradingInstrument.Instrument),
                EnsureSettingsChangedEvent(SettingsTypeContract.TradingInstrument)
            }));
        }

        #endregion TradingInstruments

        #region TradingRoutes
        
        public static async Task CreateTradingRoutes()
        {
            await Task.WhenAll(MtSettingsData.TradingRoutes().SelectMany(tradingRoute => new[]
            {
                ClientUtil.TradingRoutesApi.Insert(tradingRoute),
                EnsureSettingsChangedEvent(SettingsTypeContract.TradingRoute)
            }));
        }

        public static async Task RemoveTradingRoutes()
        {
            await Task.WhenAll(MtSettingsData.TradingRoutes().SelectMany(tradingRoute => new[]
            {
                ClientUtil.TradingRoutesApi.Delete(tradingRoute.Id),
                EnsureSettingsChangedEvent(SettingsTypeContract.TradingRoute)
            }));
        }
        
        #endregion TradingRoutes

        private static async Task EnsureSettingsChangedEvent(SettingsTypeContract settingsType)
        {
            await RabbitUtil.WaitForMessage<SettingsChangedEvent>(m => m.SettingsType == settingsType);
        }
    }
}
