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
        internal IConnection Connection { get; }
        
        RabbitMQCoreOptions Options { get; }

        public event Action OnConnectionShutdown;

        bool IsConnected { get; }

        void Connect();
        IPublisher CreatePublisher(Action<PublisherOptions> options);
        ISubscriber CreateSubscriber(Action<SubscriberOptions> options);
    }
}
