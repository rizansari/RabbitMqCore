using RabbitMQ.Client;
using RabbitMqCore.Events;
using RabbitMqCore.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public interface IQueueService : IDisposable
    {
        IConnection Connection { get; }
        RabbitMQCoreOptions Options { get; }
        IPublisher CreatePublisher(Action<PublisherOptions> options);
        void SendMessage(string payload, PublisherOptions options);
        void CreateExchangeOrQueue(PublisherOptions options);

        ISubscriber CreateSubscriber(Action<SubscriberOptions> options);
        void CreateExchangeOrQueue(SubscriberOptions options);
        void Subscribe(SubscriberOptions options, Action<RabbitMessageEventArgs> onMessage);
        void Unsubscribe(SubscriberOptions options);
    }
}
