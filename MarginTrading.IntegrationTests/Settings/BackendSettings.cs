using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.IntegrationTests.Settings
{
    [UsedImplicitly]
    public class BackendSettings
    {
        public string ExchangeConnector { get; set; }

        [Optional]
        public bool IsFakeGavel => ExchangeConnector == "FakeExchangeConnector";
    }
}
