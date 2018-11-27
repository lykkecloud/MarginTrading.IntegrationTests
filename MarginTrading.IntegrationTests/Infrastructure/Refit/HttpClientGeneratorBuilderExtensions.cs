using System;
using Lykke.HttpClientGenerator;
using Lykke.HttpClientGenerator.Infrastructure;

namespace MarginTrading.IntegrationTests.Infrastructure.Refit
{
    public static class HttpClientGeneratorBuilderExtensions
    {
        public static HttpClientGeneratorBuilder WithServiceName<T>(this HttpClientGeneratorBuilder builder, string serviceName)
        where T: class
        {
            if (builder == null)
                throw new ArgumentNullException(nameof (builder));
            if (string.IsNullOrEmpty(serviceName))
                throw new ArgumentNullException(nameof (serviceName));
            return builder.WithAdditionalCallsWrapper(new ServiceNameCallsWrapper<T>(serviceName));
        }
    }
}
