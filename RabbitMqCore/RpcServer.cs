using Microsoft.Extensions.Logging;
using RabbitMqCore.Common;
using RabbitMqCore.Events;
using RabbitMqCore.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public class RpcServer : IRpcServer
    {
        protected IQueueService _queueService;

        protected RpcOptions _options;

        readonly ILogger<IQueueService> _log;

        protected IPublisher _publisher;
        protected ISubscriber _subscriber;

        public RpcServer(
            IQueueService queueService,
            RpcOptions options,
            ILogger<IQueueService> log)
        {
            _queueService = queueService;
            _options = options;
            _log = log;
        }

        internal void Initialize()
        {
            _publisher = _queueService.CreatePublisher(options =>
            {
                options.ExchangeOrQueue = Enums.ExchangeOrQueue.Queue;
                options.QueueName = string.Format(ArgumentStrings.RpcQueueNameResponse, _options.RpcName);
            });


            _subscriber = _queueService.CreateSubscriber(options =>
            {
                options.ExchangeOrQueue = Enums.ExchangeOrQueue.Queue;
                options.QueueName = string.Format(ArgumentStrings.RpcQueueNameRequest, _options.RpcName);
            });
        }

        public void Dispose()
        {
            try
            {
                _subscriber.Unsubscribe();
                _subscriber.Dispose();
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Dispose");
            }

            try
            {
                _publisher.Dispose();
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Dispose");
            }
        }

        public void Subscribe(Action<RabbitMessageInbound> action)
        {
            _subscriber.Subscribe(msg =>
            {
                action.Invoke(msg);
            });
        }

        public void Respond(RabbitMessageOutbound @object)
        {
            @object.Expiration = "5000"; // todo: from config
            _publisher.SendMessage(@object);
        }
    }
}
