using System;
using System.Collections.Generic;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.IntegrationTests.Settings;

namespace MarginTrading.IntegrationTests.Infrastructure
{
    public static class CqrsUtil
    {
        private static readonly CqrsEngine _cqrsEngine = CreateEngine();
        private const string DefaultRoute = "Default";
        private const string EventsRoute = "events";

        private static CqrsEngine CreateEngine()
        {
            var sett = SettingsUtil.Settings.IntegrationTestSettings.Cqrs;
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = sett.ConnectionString
            };

            var log = new LogToConsole();
            var messagingEngine = new MessagingEngine(log, new TransportResolver(new Dictionary<string, TransportInfo>
            {
                {
                    "RabbitMq",
                    new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName,
                        rabbitMqSettings.Password, "None", "RabbitMq")
                }
            }), new RabbitMqTransportFactory());
            var rabbitMqConventionEndpointResolver =
                new RabbitMqConventionEndpointResolver("RabbitMq", "messagepack", environment: sett.EnvironmentName);
            return new CqrsEngine(log, new DependencyResolver(), messagingEngine, new DefaultEndpointProvider(), true,
                Register.DefaultEndpointResolver(rabbitMqConventionEndpointResolver),
                RegisterBoundedContext(sett));
        }

        // todo: move to test-specific code
        private static IRegistration RegisterBoundedContext(CqrsSettings settings)
        {
            return Register.BoundedContext(settings.ContextNames.TradingEngine)
                .PublishingEvents(
                    typeof(Backend.Contracts.Events.PositionClosedEvent))
                .With(EventsRoute)
                .PublishingCommands(typeof(DepositCommand))
                .To(settings.ContextNames.AccountsManagement)
                .With(DefaultRoute);
        }

        public static void SendCommandToAccountManagement<T>(T command)
        {
            var sett = SettingsUtil.Settings.IntegrationTestSettings.Cqrs;
            _cqrsEngine.SendCommand(command, sett.ContextNames.TradingEngine,
                sett.ContextNames.AccountsManagement);
        }
        
        public static void SendEventToAccountManagement<T>(T @event)
        {
            var sett = SettingsUtil.Settings.IntegrationTestSettings.Cqrs;
            _cqrsEngine.PublishEvent(@event, sett.ContextNames.TradingEngine);
        }

        private class DependencyResolver : IDependencyResolver
        {
            public object GetService(Type type)
            {
                return Activator.CreateInstance(type);
            }

            public bool HasService(Type type)
            {
                return !type.IsInterface;
            }
        }
    }
}
