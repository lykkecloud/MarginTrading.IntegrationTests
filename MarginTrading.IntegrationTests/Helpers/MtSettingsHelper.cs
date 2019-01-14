using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.IntegrationTests.Models;
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
            foreach(var asset in MtSettingsData.Assets())
            {
                await ClientUtil.AssetsApi.Insert(asset);
                await EnsureSettingsChangedEvent(SettingsTypeContract.Asset);
            }
        }

        public static async Task RemoveAssets()
        {
            foreach(var asset in MtSettingsData.Assets())
            {
                await ClientUtil.AssetsApi.Delete(asset.Id);
                await EnsureSettingsChangedEvent(SettingsTypeContract.Asset);
            }
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
            foreach(var assetPair in MtSettingsData.AssetPairs())
            {
                await ClientUtil.AssetPairsApi.Delete(assetPair.Id);
                await EnsureSettingsChangedEvent(SettingsTypeContract.AssetPair);
                await EnsureAssetPairChangedEvent(assetPair.Id);
            }
        }

        private static async Task EnsureAssetPairChangedEvent(string assetPairId)
        {
            await RabbitUtil.WaitForMessage<AssetPairChangedEvent>(m => m.AssetPair?.Id == assetPairId);
        }
        
        #endregion AssetPairs

        #region Markets
        
        public static async Task CreateMarkets()
        {
            foreach(var market in MtSettingsData.Markets())
            {
                await ClientUtil.MarketsApi.Insert(market);
                await EnsureSettingsChangedEvent(SettingsTypeContract.Market);
            }
        }

        public static async Task RemoveMarkets()
        {
            foreach(var market in MtSettingsData.Markets())
            {
                await ClientUtil.MarketsApi.Delete(market.Id);
                await EnsureSettingsChangedEvent(SettingsTypeContract.Market);
            }
        }
        
        #endregion Markets

        #region ScheduleSettings
        
        public static async Task CreateScheduleSettings()
        {
            foreach(var ss in MtSettingsData.ScheduleSettings())
            {
                await ClientUtil.ScheduleSettingsApi.Insert(ss);
                await EnsureSettingsChangedEvent(SettingsTypeContract.ScheduleSettings);
            }
        }

        public static async Task RemoveScheduleSettings()
        {
            foreach(var ss in MtSettingsData.ScheduleSettings())
            {
                await ClientUtil.ScheduleSettingsApi.Delete(ss.Id);
                await EnsureSettingsChangedEvent(SettingsTypeContract.ScheduleSettings);
            }
        }

        #endregion ScheduleSettings

        #region TradingInstruments
        
        public static async Task CreateTradingInstruments()
        {
            foreach(var tradingInstrument in MtSettingsData.TradingInstruments())
            {
                await ClientUtil.TradingInstrumentsApi.Insert(tradingInstrument);
                await EnsureSettingsChangedEvent(SettingsTypeContract.TradingInstrument);
            }
        }

        public static async Task RemoveTradingInstruments()
        {
            foreach(var tradingInstrument in MtSettingsData.TradingInstruments())
            {
                await ClientUtil.TradingInstrumentsApi.Delete(
                    SettingsUtil.Settings.IntegrationTestSettings.Behavior.TradingCondition,
                    tradingInstrument.Instrument);
                await EnsureSettingsChangedEvent(SettingsTypeContract.TradingInstrument);
            }
        }

        #endregion TradingInstruments

        #region TradingRoutes
        
        public static async Task CreateTradingRoutes()
        {
            foreach(var tradingRoute in MtSettingsData.TradingRoutes())
            {
                await ClientUtil.TradingRoutesApi.Insert(tradingRoute);
                await EnsureSettingsChangedEvent(SettingsTypeContract.TradingRoute);
            }
        }

        public static async Task RemoveTradingRoutes()
        {
            foreach(var tradingRoute in MtSettingsData.TradingRoutes())
            {
                await ClientUtil.TradingRoutesApi.Delete(tradingRoute.Id);
                await EnsureSettingsChangedEvent(SettingsTypeContract.TradingRoute);
            }
        }
        
        #endregion TradingRoutes

        private static async Task EnsureSettingsChangedEvent(SettingsTypeContract settingsType)
        {
            //await RabbitUtil.WaitForMessage<SettingsChangedEvent>(m => m.SettingsType == settingsType);
        }
    }
}
