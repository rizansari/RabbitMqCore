using RabbitMqCore.Events;
using RabbitMqCore.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public class Subscriber : ISubscriber
    {
        IQueueService _queueService;
        SubscriberOptions _options;

        Action<RabbitMessageEventArgs> _onMessage;

        public Subscriber(IQueueService queueService, SubscriberOptions options)
        {
            _queueService = queueService;
            _options = options;

            _queueService.CreateExchangeOrQueue(_options);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Unsubscribe()
        {
            _queueService.Unsubscribe(_options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        void ISubscriber.Subscribe(Action<RabbitMessageEventArgs> action)
        {
            _onMessage = action;
            _queueService.Subscribe(_options, opt => _onMessage(opt));
        }
    }
}
