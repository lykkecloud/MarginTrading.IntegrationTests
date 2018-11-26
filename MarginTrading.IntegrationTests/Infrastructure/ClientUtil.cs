using System;
using Lykke.HttpClientGenerator;
using MarginTrading.Backend.Contracts;
using MarginTrading.IntegrationTests.Settings;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.TradingHistory.Client;
using IAccountsApi = MarginTrading.AccountsManagement.Contracts.IAccountsApi;

namespace MarginTrading.IntegrationTests.Infrastructure
{
    public static class ClientUtil
    {
        public static IAccountsApi AccountsApi { get; } = GetApi<IAccountsApi>(SettingsUtil.Settings.AccountManagementClient);

        public static Backend.Contracts.IAccountsApi AccountsStatApi { get; } = GetApi<Backend.Contracts.IAccountsApi>(SettingsUtil.Settings.MtCoreClient);
        public static IOrdersApi OrdersApi { get; } = GetApi<IOrdersApi>(SettingsUtil.Settings.MtCoreClient);
        public static IPositionsApi PositionsApi { get; } = GetApi<IPositionsApi>(SettingsUtil.Settings.MtCoreClient);
        public static IPricesApi PricesApi { get; } = GetApi<IPricesApi>(SettingsUtil.Settings.MtCoreClient);
        
        public static IAssetPairsApi AssetPairsApi { get; } = GetApi<IAssetPairsApi>(SettingsUtil.Settings.SettingsServiceClient);
        public static IAssetsApi AssetsApi { get; } = GetApi<IAssetsApi>(SettingsUtil.Settings.SettingsServiceClient);
        public static IMarketsApi MarketsApi { get; } = GetApi<IMarketsApi>(SettingsUtil.Settings.SettingsServiceClient);
        public static IScheduleSettingsApi ScheduleSettingsApi { get; } = GetApi<IScheduleSettingsApi>(SettingsUtil.Settings.SettingsServiceClient);
        public static ITradingConditionsApi TradingConditionsApi { get; } = GetApi<ITradingConditionsApi>(SettingsUtil.Settings.SettingsServiceClient);
        public static ITradingInstrumentsApi TradingInstrumentsApi { get; } = GetApi<ITradingInstrumentsApi>(SettingsUtil.Settings.SettingsServiceClient);
        public static ITradingRoutesApi TradingRoutesApi { get; } = GetApi<ITradingRoutesApi>(SettingsUtil.Settings.SettingsServiceClient);
        
        public static IDealsApi DealsApi { get; } = GetApi<IDealsApi>(SettingsUtil.Settings.TradingHistoryClient);
        public static IOrderEventsApi OrderEventsApi { get; } = GetApi<IOrderEventsApi>(SettingsUtil.Settings.TradingHistoryClient);
        public static ITradesApi TradesApi { get; } = GetApi<ITradesApi>(SettingsUtil.Settings.TradingHistoryClient);

        private static T GetApi<T>(ClientSettings apiSettings)
        {
            var generatorBuilder = HttpClientGenerator.BuildForUrl(apiSettings.ServiceUrl)
                .WithoutCaching().WithoutRetries();
            
            if (!string.IsNullOrEmpty(apiSettings.ApiKey))
            {
                generatorBuilder = generatorBuilder.WithApiKey(apiSettings.ApiKey);
            }
            
            return generatorBuilder.Create().Generate<T>();
        }
    }
}
