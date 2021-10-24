using RabbitMQ.Client;
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
    }
}
