﻿using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.IntegrationTests.Settings
{
    [UsedImplicitly]
    public class RabbitConnectionSettings
    {
        public string ExchangeName { get; set; }
        
        [Optional, CanBeNull]
        public string ConnectionString { get; set; }
        
        [Optional, CanBeNull] 
        public string RoutingKey { get; set; }
    }
}
