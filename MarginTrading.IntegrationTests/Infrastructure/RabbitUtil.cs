﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common.Extensions;
using MarginTrading.IntegrationTests.Settings;

namespace MarginTrading.IntegrationTests.Infrastructure
{
    public static class RabbitUtil
    {
        private static readonly RabbitMqService RabbitMqService = new RabbitMqService();

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
            RabbitMqService.Subscribe(new RabbitConnectionSettings
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
            RabbitMqService.Subscribe(new RabbitConnectionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = exchange,
                RoutingKey = routingKey,
            }, false, MessageHandler, RabbitMqService.GetJsonDeserializer<T>());
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
                return old.RemoveAll(l => listeners.Contains(l));
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
