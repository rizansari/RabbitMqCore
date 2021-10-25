using RabbitMqCore.Events;
using RabbitMqCore.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public class Publisher : IPublisher
    {
        protected IQueueService _queueService;

        protected PublisherOptions _options;

        public Publisher(IQueueService queueService, PublisherOptions options)
        {
            _queueService = queueService;
            _options = options;
            _queueService.CreateExchangeOrQueue(_options);
        }

        public void SendMessage(RabbitMessageOutbound message)
        {
            _queueService.SendMessage(message, _options);
        }
    }
}
