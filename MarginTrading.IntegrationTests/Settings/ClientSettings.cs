using Lykke.SettingsReader.Attributes;

namespace MarginTrading.IntegrationTests.Settings
{
    internal class ClientSettings
    {
        public string ServiceUrl { get; set; }
        
        [Optional]
        public string ApiKey { get; set; }
    }
}
