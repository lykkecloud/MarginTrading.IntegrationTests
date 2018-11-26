using JetBrains.Annotations;

namespace MarginTrading.IntegrationTests.Settings
{
    [UsedImplicitly]
    public class RabbitMqSettings
    {
        public RabbitConnectionSettings OrderHistory { get; set; }
        
        public RabbitConnectionSettings PositionHistory { get; set; }
        
        public RabbitConnectionSettings SettingsChanged { get; set; }
    }
}
