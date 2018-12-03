using System;
using Lykke.HttpClientGenerator;
using MarginTrading.Backend.Contracts;
using MarginTrading.IntegrationTests.Infrastructure.Refit;
using MarginTrading.IntegrationTests.Settings;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.TradingHistory.Client;
using IAccountsApi = MarginTrading.AccountsManagement.Contracts.IAccountsApi;

namespace MarginTrading.IntegrationTests.Infrastructure
{
    public static class ClientUtil
    {
        private const string AccountManagement = "MT Account Management Service";
        private const string MtCore = "MT Core Backend Service";
        private const string SettingsService = "MT Settings Service";
        private const string TradingHistory = "MT Trading History Service";
        
        public static IAccountsApi AccountsApi { get; } = GetApi<IAccountsApi, LykkeErrorResponse>
            (SettingsUtil.Settings.AccountManagementClient, AccountManagement);

        public static Backend.Contracts.IAccountsApi AccountsStatApi { get; } = 
            GetApi<Backend.Contracts.IAccountsApi, LykkeErrorResponse>(SettingsUtil.Settings.MtCoreClient, MtCore);
        public static IOrdersApi OrdersApi { get; } = 
            GetApi<IOrdersApi, LykkeErrorResponse>(SettingsUtil.Settings.MtCoreClient, MtCore);
        public static IPositionsApi PositionsApi { get; } = 
            GetApi<IPositionsApi, LykkeErrorResponse>(SettingsUtil.Settings.MtCoreClient, MtCore);
        public static IPricesApi PricesApi { get; } = 
            GetApi<IPricesApi, LykkeErrorResponse>(SettingsUtil.Settings.MtCoreClient, MtCore);
        
        public static IAssetPairsApi AssetPairsApi { get; } = 
            GetApi<IAssetPairsApi, LykkeErrorResponse>(SettingsUtil.Settings.SettingsServiceClient, SettingsService);
        public static IAssetsApi AssetsApi { get; } = 
            GetApi<IAssetsApi, LykkeErrorResponse>(SettingsUtil.Settings.SettingsServiceClient, SettingsService);
        public static IMarketsApi MarketsApi { get; } = 
            GetApi<IMarketsApi, LykkeErrorResponse>(SettingsUtil.Settings.SettingsServiceClient, SettingsService);
        public static IScheduleSettingsApi ScheduleSettingsApi { get; } = 
            GetApi<IScheduleSettingsApi, LykkeErrorResponse>(SettingsUtil.Settings.SettingsServiceClient, SettingsService);
        public static ITradingConditionsApi TradingConditionsApi { get; } = 
            GetApi<ITradingConditionsApi, LykkeErrorResponse>(SettingsUtil.Settings.SettingsServiceClient, SettingsService);
        public static ITradingInstrumentsApi TradingInstrumentsApi { get; } = 
            GetApi<ITradingInstrumentsApi, LykkeErrorResponse>(SettingsUtil.Settings.SettingsServiceClient, SettingsService);
        public static ITradingRoutesApi TradingRoutesApi { get; } = 
            GetApi<ITradingRoutesApi, LykkeErrorResponse>(SettingsUtil.Settings.SettingsServiceClient, SettingsService);
        
        public static IDealsApi DealsApi { get; } = 
            GetApi<IDealsApi, LykkeErrorResponse>(SettingsUtil.Settings.TradingHistoryClient, TradingHistory);
        public static IOrderEventsApi OrderEventsApi { get; } = 
            GetApi<IOrderEventsApi, LykkeErrorResponse>(SettingsUtil.Settings.TradingHistoryClient, TradingHistory);
        public static ITradesApi TradesApi { get; } = 
            GetApi<ITradesApi, LykkeErrorResponse>(SettingsUtil.Settings.TradingHistoryClient, TradingHistory);

        private static TProxy GetApi<TProxy, TErrorResponse>(ClientSettings apiSettings, string serviceName)
            where TErrorResponse: class
        {
            var generatorBuilder = HttpClientGenerator.BuildForUrl(apiSettings.ServiceUrl)
                .WithServiceName<TErrorResponse>(serviceName)
                .WithoutCaching().WithoutRetries();
            
            if (!string.IsNullOrEmpty(apiSettings.ApiKey))
            {
                generatorBuilder = generatorBuilder.WithApiKey(apiSettings.ApiKey);
            }
            
            return generatorBuilder.Create().Generate<TProxy>();
        }
    }
}
