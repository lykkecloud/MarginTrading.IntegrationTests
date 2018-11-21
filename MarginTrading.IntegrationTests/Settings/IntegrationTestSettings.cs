using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.IntegrationTests.Settings
{
    [UsedImplicitly]
    public class IntegrationTestSettings
    {
        /// <summary>
        /// DB connection strings
        /// </summary>
        public DbSettings Db { get; set; }
        
        /// <summary>
        /// RabbitMq exchanges connections
        /// </summary>
        public RabbitMqSettings RabbitMqQueues { get; set; }
        
        /// <summary>
        /// Behavior settings for accounts
        /// </summary>
        public BehaviorSettings Behavior { get; set; }
        
        public CqrsSettings Cqrs { get; set; }
        
        [Optional]
        public bool EnableOperationsLogs { get; set; }
        
        /// <summary>
        /// RabbitMqSubscriber does not wait for a reader thread to start before returning from the Start() method.
        /// Must be 1000 for k8s, 10000 for local
        /// </summary>
        public int MessagingDelay { get; set; }
        
        /// <summary>
        /// Timeout for a RabbitMq message to come over.
        /// Must be 6000 for k8s, 60000 for local
        /// </summary>
        public int RabbitListenerTimeout { get; set; }
    }
}
