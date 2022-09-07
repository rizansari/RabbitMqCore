using Microsoft.Extensions.Logging;
using RabbitMqCore.Events;
using RabbitMqCore.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public class Subscriber : ISubscriber
    {
        protected IQueueService _queueService;
        protected SubscriberOptions _options;

        readonly ILogger<IQueueService> _log;

        private Action<RabbitMessageInbound> _onMessage;

        public bool IsSuspended { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueService"></param>
        /// <param name="options"></param>
        public Subscriber(
            IQueueService queueService, 
            SubscriberOptions options,
            ILogger<IQueueService> log)
        {
            _queueService = queueService;
            _options = options;
            _log = log;

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
        void ISubscriber.Subscribe(Action<RabbitMessageInbound> action)
        {
            _onMessage = action;
            _queueService.Subscribe(_options, opt => _onMessage(opt));
        }

        /// <summary>
        /// 
        /// </summary>
        public void Resume()
        {
            _queueService.Resume(_options);
            IsSuspended = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Suspend()
        {
            IsSuspended = true;
            _queueService.Suspend(_options);
        }

        public void Acknowledge(ulong DeliveryTag)
        {
            _queueService.Acknowledge(_options, DeliveryTag);
        }
    }
}
