using System;
using System.Reflection;
using System.Threading.Tasks;
using Lykke.HttpClientGenerator.Infrastructure;
using MongoDB.Driver;
using Newtonsoft.Json;
using Refit;

namespace MarginTrading.IntegrationTests.Infrastructure.Refit
{
    /// <inheritdoc />
    /// <summary>
    /// Create HttpClientGenerator wrapper to log Service name and exception details
    /// </summary>
    /// <typeparam name="T">Error model</typeparam>
    internal class ServiceNameCallsWrapper<T> : ICallsWrapper
        where T: class
    {
        private static readonly FieldInfo ExceptionMessageField =
            typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly string _serviceName;

        public ServiceNameCallsWrapper(string serviceName)
        {
            _serviceName = serviceName;
        }

        public async Task<object> HandleMethodCall(MethodInfo targetMethod, object[] args, Func<Task<object>> innerHandler)
        {
            try
            {
                return await innerHandler();
            }
            catch (Exception e)
            {
                var httpPathAttr = targetMethod.GetCustomAttribute<HttpMethodAttribute>();
                var apiErrorDetails = "";
                if (e is ApiException apiException)
                {
                    apiErrorDetails = $" with reason ({JsonConvert.DeserializeObject<T>(apiException.Content)})";
                }
                
                var endpoint = httpPathAttr?.Path ?? $"{targetMethod.DeclaringType.Name}.{targetMethod.Name}";
                var message = $"An error occurred while trying to reach {this._serviceName} ({endpoint}){apiErrorDetails}: {e.Message}";

                // Have to use reflection here, otherwise can't change message without wrapping exception
                // which would conflict with the requirement of not losing exception type
                //
                // This field will be present on all exceptions, as it is defined on the base Exception type
                ExceptionMessageField.SetValue(e, message);

                // Rethrow the now modified exception to preserve the stack trace
                throw;
            }
        }
    }
}
