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

        public event Action OnConnectionShutdown;
        public event Action OnReconnected;
        
        IPublisher CreatePublisher(Action<PublisherOptions> options);
        void SendMessage(RabbitMessageOutbound message, PublisherOptions options);
        void CreateExchangeOrQueue(PublisherOptions options);
        ISubscriber CreateSubscriber(Action<SubscriberOptions> options);
        void CreateExchangeOrQueue(SubscriberOptions options);
        void Subscribe(SubscriberOptions options, Action<RabbitMessageInbound> onMessage);
        void Unsubscribe(SubscriberOptions options);
        void Resume(SubscriberOptions options);
        void Suspend(SubscriberOptions options);
    }
}
