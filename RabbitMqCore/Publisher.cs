using Newtonsoft.Json;
using RabbitMqCore.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public class Publisher : IPublisher
    {
        IQueueService _queueService;
        PublisherOptions _options;

        public Publisher(IQueueService queueService, PublisherOptions options)
        {
            _queueService = queueService;
            _options = options;

            _queueService.CreateExchangeOrQueue(_options);
        }

        public void SendMessage(object @object)
        {
            _queueService.SendMessage(JsonConvert.SerializeObject(@object), _options);
        }
    }
}
