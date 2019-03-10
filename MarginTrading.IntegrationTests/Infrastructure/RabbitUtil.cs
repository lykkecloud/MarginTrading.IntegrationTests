using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common.Extensions;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.IntegrationTests.Settings;
using Swashbuckle.AspNetCore.Swagger;

namespace MarginTrading.IntegrationTests.Infrastructure
{
    public static class RabbitUtil
    {
        private static RabbitMqService _rabbitMqService = new RabbitMqService();

        private static readonly ConcurrentDictionary<Type, ConcurrentBag<object>> MessagesHistory =
            new ConcurrentDictionary<Type, ConcurrentBag<object>>();

        private static readonly ConcurrentDictionary<Type, ImmutableList<Listener>> Listeners =
            new ConcurrentDictionary<Type, ImmutableList<Listener>>();

        private static readonly IntegrationTestSettings Settings = SettingsUtil.Settings.IntegrationTestSettings;

        public static Task<T> WaitForMessage<T>(Func<T, bool> predicate)
            where T : class
        {
            var listener = new Listener<T>(predicate, new TaskCompletionSource<T>());

            Listeners.AddOrUpdate(typeof(T),
                k => ImmutableList.Create<Listener>(listener),
                (k, l) => l.Add(listener));

            var suitableOldMessage = MessagesHistory.GetValueOrDefault(typeof(T))?.Cast<T>().FirstOrDefault(predicate);
            if (suitableOldMessage != null)
            {
                CompleteListener(suitableOldMessage, listener);

                Listeners.AddOrUpdate(typeof(T),
                    k => ImmutableList<Listener>.Empty,
                    (k, l) => l.Remove(listener));
            }
            
            return listener.TaskCompletionSource.Task.WithTimeout(Settings.RabbitListenerTimeout);
        }

        public static void ListenCqrsMessages<T>(string connectionString, string exchange)
            where T : class
        {
            var routingKey = typeof(T).Name;
            _rabbitMqService.Subscribe(new RabbitConnectionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = exchange,
                RoutingKey = routingKey,
            }, false, MessageHandler, RabbitMqService.GetMsgPackDeserializer<T>());
        }

        public static void ListenJsonMessages<T>(string connectionString, string exchange)
            where T : class
        {
            var routingKey = typeof(T).Name;
            _rabbitMqService.Subscribe(new RabbitConnectionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = exchange,
                RoutingKey = routingKey,
            }, false, MessageHandler, RabbitMqService.GetJsonDeserializer<T>());
        }

        public static RabbitMqPublisher<TMessage> GetProducer<TMessage>(RabbitConnectionSettings settings, 
            bool isDurable = false, bool isJson = true, bool isTopic = true)
        {
            return _rabbitMqService.GetProducer(settings, isDurable,
                isJson
                    ? RabbitMqService.GetJsonSerializer<TMessage>()
                    : RabbitMqService.GetMsgPackSerializer<TMessage>(),
                isTopic
                    ? new RabbitMqService.TopicPublishStrategy(isDurable)
                    : (IRabbitMqPublishStrategy) new DefaultFanoutPublishStrategy(new RabbitMqSubscriptionSettings
                    {
                        ConnectionString = settings.ConnectionString,
                        ExchangeName = settings.ExchangeName,
                        IsDurable = isDurable,
                        RoutingKey = settings.RoutingKey,
                    }));
        }

        public static void TearDown()
        {
            //this unsubscribe from all the exchanges, it's ok only for globally non-parallel mode!
            _rabbitMqService.Dispose();
            
            _rabbitMqService = new RabbitMqService();
        }

        public static bool EnsureMessageHistoryEmpty(out string trace)
        {
            var tradeData = new Dictionary<string, int>();

            foreach (var typedHistory in MessagesHistory)
            {
                if (typedHistory.Value.IsEmpty) continue;
                
                tradeData.Add(typedHistory.Key.Name, typedHistory.Value.Count);
                    
                typedHistory.Value.Clear();
            }

            trace = string.Join(";", tradeData.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

            return tradeData.Select(x => x.Value).Sum() == 0;
        }

        private static Task MessageHandler<T>(T message)
        {
            MessagesHistory.GetOrAdd(typeof(T), t => new ConcurrentBag<object>()).Add(message);

            var listeners = Array.Empty<Listener<T>>();
            Listeners.AddOrUpdate(typeof(T), ImmutableList<Listener>.Empty, (key, old) =>
            {
                listeners = old
                    .OfType<Listener<T>>()
                    .Where(l => l.Predicate(message))
                    .ToArray();
                //return old.RemoveAll(l => listeners.Contains(l));
                return listeners.Any() ? old.Remove(listeners.First()) : old; // to keep track # of events
            });

            // Note that the async continuations, attached to the TaskCompletionSource.Task,
            // could execute synchronously inside of TrySetResult()
            var tasks = listeners.Select(l => CompleteListener(message, l));

            return Task.WhenAll(tasks);
        }

        private static Task<bool> CompleteListener<T>(T message, Listener<T> l)
        {
            //integration tests are working in synchronous mode.
            l.TaskCompletionSource.SetResult(message);
            return Task.FromResult(true);
//            return Task.Run(() => l.TaskCompletionSource.TrySetResult(message));
        }

        private class Listener
        {
        }

        private class Listener<T> : Listener
        {
            public Func<T, bool> Predicate { get; }
            public TaskCompletionSource<T> TaskCompletionSource { get; }

            public Listener(Func<T, bool> predicate, TaskCompletionSource<T> taskCompletionSource)
            {
                Predicate = predicate;
                TaskCompletionSource = taskCompletionSource;
            }
        }
    }
}
