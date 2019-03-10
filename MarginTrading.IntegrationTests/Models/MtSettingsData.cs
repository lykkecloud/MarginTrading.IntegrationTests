using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.IntegrationTests.Infrastructure;
using MarginTrading.SettingsService.Contracts.Asset;
using MarginTrading.SettingsService.Contracts.AssetPair;
using MarginTrading.SettingsService.Contracts.Market;
using MarginTrading.SettingsService.Contracts.Routes;
using MarginTrading.SettingsService.Contracts.Scheduling;
using MarginTrading.SettingsService.Contracts.TradingConditions;

namespace MarginTrading.IntegrationTests.Models
{
    public static class MtSettingsData
    {
        public static IEnumerable<AssetContract> Assets()
        {
            yield return new AssetContract {Id = "IT_Asset1", Name = "IT_Asset1", Accuracy = 5};
            yield return new AssetContract {Id = "IT_Asset2", Name = "IT_Asset2", Accuracy = 6};
            yield return new AssetContract {Id = "IT_Asset3", Name = "IT_Asset3", Accuracy = 7};
        }


        public static IEnumerable<AssetPairContract> AssetPairs()
        {
            yield return new AssetPairContract
            {
                Id = "IT_AssetPair1", 
                Name = "IT_AssetPair1", 
                BaseAssetId = "IT_Asset1", 
                QuoteAssetId = "EUR", 
                Accuracy = 5,
                MarketId = "IT_Market1",
                LegalEntity = SettingsUtil.Settings.IntegrationTestSettings.Behavior.LegalEntity,
                MatchingEngineMode = SettingsUtil.Settings.IntegrationTestSettings.Behavior.MatchingEngineMode,
                StpMultiplierMarkupBid = 1,
                StpMultiplierMarkupAsk = 1
            };
            yield return new AssetPairContract
            {
                Id = "IT_AssetPair2", 
                Name = "IT_AssetPair2", 
                BaseAssetId = "IT_Asset2", 
                QuoteAssetId = "EUR", 
                Accuracy = 6,
                MarketId = "IT_Market1",
                LegalEntity = SettingsUtil.Settings.IntegrationTestSettings.Behavior.LegalEntity,
                MatchingEngineMode = SettingsUtil.Settings.IntegrationTestSettings.Behavior.MatchingEngineMode,
                StpMultiplierMarkupBid = 1,
                StpMultiplierMarkupAsk = 1
            };
            yield return new AssetPairContract
            {
                Id = "IT_AssetPair3", 
                Name = "IT_AssetPair3", 
                BaseAssetId = "IT_Asset3", 
                QuoteAssetId = "EUR", 
                Accuracy = 6,
                MarketId = "IT_Market1",
                LegalEntity = SettingsUtil.Settings.IntegrationTestSettings.Behavior.LegalEntity,
                MatchingEngineMode = SettingsUtil.Settings.IntegrationTestSettings.Behavior.MatchingEngineMode,
                StpMultiplierMarkupBid = 1,
                StpMultiplierMarkupAsk = 1
            };
        }

        public static IEnumerable<MarketContract> Markets()
        {
            yield return new MarketContract { Id = "IT_Market1", Name = "IT_Market1"};
            yield return new MarketContract { Id = "IT_Market2", Name = "IT_Market2"};
        }

        public static IEnumerable<ScheduleSettingsContract> ScheduleSettings()
        {
            yield return new ScheduleSettingsContract
            {
                Id = "IT_ScheduleSettings1",
                Rank = 100000,
                AssetPairRegex = null,
                AssetPairs = new [] {"IT_AssetPair1"}.ToHashSet(),
                MarketId = "IT_Market1",
                IsTradeEnabled = true,
                PendingOrdersCutOff = null,
                Start = new ScheduleConstraintContract { DayOfWeek = DayOfWeek.Monday, Time = TimeSpan.FromHours(0)},
                End = new ScheduleConstraintContract { DayOfWeek = DayOfWeek.Sunday, Time = new TimeSpan(23,59,59)},
            };
            yield return new ScheduleSettingsContract
            {
                Id = "IT_ScheduleSettings2",
                Rank = 1,
                AssetPairRegex = null,
                AssetPairs = new [] {"IT_AssetPair1"}.ToHashSet(),
                MarketId = "IT_Market1",
                IsTradeEnabled = false,
                PendingOrdersCutOff = null,
                Start = new ScheduleConstraintContract { DayOfWeek = DayOfWeek.Friday, Time = TimeSpan.FromHours(20)},
                End = new ScheduleConstraintContract { DayOfWeek = DayOfWeek.Monday, Time = TimeSpan.FromHours(8)},
            };
        }

        public static IEnumerable<TradingInstrumentContract> TradingInstruments()
        {
            yield return new TradingInstrumentContract
            {
                TradingConditionId = SettingsUtil.Settings.IntegrationTestSettings.Behavior.TradingCondition,
                Instrument = "IT_AssetPair1",
                LeverageInit = 50,
                LeverageMaintenance = 50,
                SwapLong = 0,
                SwapShort = 0,
                Delta = 0,
                DealMinLimit = 1,
                DealMaxLimit = 10000,
                PositionLimit = 1000000,
                LiquidationThreshold = 0,
                CommissionRate = 0.001M,
                CommissionMin = 9.95M,
                CommissionMax = 69,
                CommissionCurrency = "EUR",
            };
            yield return new TradingInstrumentContract
            {
                TradingConditionId = SettingsUtil.Settings.IntegrationTestSettings.Behavior.TradingCondition,
                Instrument = "IT_AssetPair2",
                LeverageInit = 50,
                LeverageMaintenance = 50,
                SwapLong = 0,
                SwapShort = 0,
                Delta = 0,
                DealMinLimit = 1,
                DealMaxLimit = 10000,
                PositionLimit = 1000000,
                LiquidationThreshold = 0,
                CommissionRate = 0.001M,
                CommissionMin = 9.95M,
                CommissionMax = 69,
                CommissionCurrency = "EUR",
            };
        }

        public static IEnumerable<MatchingEngineRouteContract> TradingRoutes()
        {
            yield return new MatchingEngineRouteContract
            {
                Id = "IT_Route1", 
                Rank = 1,
                TradingConditionId = SettingsUtil.Settings.IntegrationTestSettings.Behavior.TradingCondition,
                ClientId = null,
                Instrument = null,
                Type = null,
                MatchingEngineId = SettingsUtil.Settings.IntegrationTestSettings.Behavior.MatchingEngineMode.ToString(),
                Asset = null,
                RiskSystemLimitType = null,
                RiskSystemMetricType = null,
            };
        }
    }
}
