using System;
using MarginTrading.IntegrationTests.Infrastructure;
using Polly;
using Polly.Retry;

namespace MarginTrading.IntegrationTests.Helpers
{
    public static class ApiHelpers
    {

        /// <summary>
        /// Policy to call API method a number of times with retry awaiting period.
        /// It ignore Refit ApiException and awaits for return value to match <paramref name="resultPredicate"/>.
        /// </summary>
        /// <param name="resultPredicate">Predicate for API return message. Not used if null.</param>
        /// <typeparam name="T">API return message type</typeparam>
        public static RetryPolicy<T> GetRefitRetryPolicy<T>(Func<T, bool> resultPredicate)
        {
            return Policy.Handle<Refit.ApiException>()
                .OrResult<T>(resultPredicate)
                .WaitAndRetryAsync(SettingsUtil.Settings.IntegrationTestSettings.Behavior.ApiCallRetries,
                    x => TimeSpan.FromMilliseconds(
                        SettingsUtil.Settings.IntegrationTestSettings.Behavior.ApiCallRetryPeriodMs));
        }

        /// <summary>
        /// Policy to call API method a number of times with retry awaiting period ignoring Refit ApiException.
        /// </summary>
        public static RetryPolicy GetRefitRetryPolicy()
        {
            return Policy.Handle<Refit.ApiException>()
                .WaitAndRetryAsync(SettingsUtil.Settings.IntegrationTestSettings.Behavior.ApiCallRetries,
                    x => TimeSpan.FromMilliseconds(
                        SettingsUtil.Settings.IntegrationTestSettings.Behavior.ApiCallRetryPeriodMs));
        }
        
    }
}
