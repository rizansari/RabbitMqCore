using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMqCore.Common;
using RabbitMqCore.Events;
using RabbitMqCore.Exceptions;
using RabbitMqCore.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RabbitMqCore
{
    public class Publisher : IPublisher
    {
        protected IQueueService _queueService;

        readonly ILogger<IQueueService> _log;

        protected PublisherOptions _options;

        protected IModel _channel;

        public event EventHandler<PublisherMessageReturnEventArgs> OnMessageReturn;
        public event EventHandler<PublisherMessageReturnEventArgs> OnMessageFailed;

        public Publisher(
            IQueueService queueService, 
            PublisherOptions options,
            ILogger<IQueueService> log)
        {
            _queueService = queueService;
            _options = options;
            _log = log;
        }

        internal void Initialize(bool CreateExchangeQueue = true)
        {
            _channel = _queueService.Connection.CreateModel();

            // register events
            _channel.BasicAcks += _channel_BasicAcks;
            _channel.BasicNacks += _channel_BasicNacks;
            _channel.BasicRecoverOk += _channel_BasicRecoverOk;
            _channel.BasicReturn += _channel_BasicReturn;
            _channel.CallbackException += _channel_CallbackException;
            _channel.FlowControl += _channel_FlowControl;
            _channel.ModelShutdown += _channel_ModelShutdown;

            // create exchange or queue
            if (CreateExchangeQueue)
            {
                CreateExchangeOrQueue();
            }
        }

        internal void Cleanup()
        {
            // unregister events
            try
            {
                _channel.BasicAcks -= _channel_BasicAcks;
                _channel.BasicNacks -= _channel_BasicNacks;
                _channel.BasicRecoverOk -= _channel_BasicRecoverOk;
                _channel.BasicReturn -= _channel_BasicReturn;
                _channel.CallbackException -= _channel_CallbackException;
                _channel.FlowControl -= _channel_FlowControl;
                _channel.ModelShutdown -= _channel_ModelShutdown;
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "error in cleanup");
            }
        }

        private void _channel_ModelShutdown(object sender, ShutdownEventArgs e)
        {
            if (e != null)
                _log.LogDebug($"Publisher Model Shutdown! Reason [{e.ReplyText}] | Initiator [{e.Initiator}]");
        }

        private void _channel_FlowControl(object sender, RabbitMQ.Client.Events.FlowControlEventArgs e)
        {
            if (e != null)
                _log.LogDebug("flow control: [{0}]", e.Active);
        }

        private void _channel_CallbackException(object sender, RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
        {
            if (e != null)
                _log.LogDebug(e.Exception, string.Join(Environment.NewLine, e.Detail.Select(x => $"{x.Key} - {x.Value}")));
        }

        private void _channel_BasicReturn(object sender, RabbitMQ.Client.Events.BasicReturnEventArgs e)
        {
            if (e != null)
                _log.LogDebug("basic return correlationid: [{0}] reply text: [{1}]", e.BasicProperties.CorrelationId, e.ReplyText);


            var message = new RabbitMessageOutbound();
            message.Message = Encoding.UTF8.GetString(e.Body.ToArray());
            message.CorrelationId = e.BasicProperties.CorrelationId;

            OnMessageReturn?.Invoke(this, new PublisherMessageReturnEventArgs(message, e.ReplyText));
        }

        private void _channel_BasicRecoverOk(object sender, EventArgs e)
        {
            if (e != null)
                _log.LogDebug("basic recover ok");
        }

        private void _channel_BasicNacks(object sender, RabbitMQ.Client.Events.BasicNackEventArgs e)
        {
            if (e != null)
                _log.LogDebug("basic nack received for delivery tag: [{0}]", e.DeliveryTag);
        }

        private void _channel_BasicAcks(object sender, RabbitMQ.Client.Events.BasicAckEventArgs e)
        {
            if (e != null)
                _log.LogDebug("basic ack received for delivery tag: [{0}]", e.DeliveryTag);
        }

        private void CreateExchangeOrQueueActual()
        {
            if (_options.ExchangeOrQueue == Enums.ExchangeOrQueue.Exchange)
            {
                IDictionary<string, object> args = new Dictionary<string, object>();

                if (!string.IsNullOrWhiteSpace(_options.AlternateExchange))
                {
                    args.Add(ArgumentStrings.AlternateExchange, _options.AlternateExchange);
                }

                _channel.ExchangeDeclare(
                    exchange: _options.ExchangeName,
                    type: _options.ExchangeType.ToString(),
                    durable: _options.Durable,
                    autoDelete: _options.AutoDelete,
                    arguments: args
                    );
            }
            else if (_options.ExchangeOrQueue == Enums.ExchangeOrQueue.Queue)
            {
                IDictionary<string, object> args = new Dictionary<string, object>();

                if (_options.MessageTTL >= 0)
                {
                    args.Add(ArgumentStrings.MessageTTL, _options.MessageTTL);
                }

                if (_options.AutoExpires >= 0)
                {
                    args.Add(ArgumentStrings.AutoExpires, _options.AutoExpires);
                }

                if (_options.MaxLength >= 0)
                {
                    args.Add(ArgumentStrings.MaxLength, _options.MaxLength);
                }

                if (_options.MaxLengthBytes >= 0)
                {
                    args.Add(ArgumentStrings.MaxLengthBytes, _options.MaxLengthBytes);
                }

                if (_options.OverflowBehavior != OverflowBehavior.None)
                {
                    if (_options.OverflowBehavior == OverflowBehavior.DropHead)
                    {
                        args.Add(ArgumentStrings.OverflowBehavior, ArgumentStrings.OverflowBehaviorDropHead);
                    }
                    else if (_options.OverflowBehavior == OverflowBehavior.RejectPublish)
                    {
                        args.Add(ArgumentStrings.OverflowBehavior, ArgumentStrings.OverflowBehaviorRejectPublish);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_options.DeadLetterExchange))
                {
                    args.Add(ArgumentStrings.DeadLetterExchange, _options.DeadLetterExchange);
                }

                if (!string.IsNullOrWhiteSpace(_options.DeadLetterRoutingKey))
                {
                    args.Add(ArgumentStrings.DeadLetterRoutingKey, _options.DeadLetterRoutingKey);
                }



                _channel.QueueDeclare(
                    queue: _options.QueueName,
                    durable: _options.Durable,
                    exclusive: _options.Exclusive,
                    autoDelete: _options.AutoDelete,
                    arguments: args
                    );
            }
        }

        private void CreateExchangeOrQueue()
        {
            try
            {
                CreateExchangeOrQueueActual();
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "CreateExchangeOrQueue failed");

                if (_options.RecreateIfFailed)
                {
                    if (_channel.IsClosed || !_channel.IsOpen)
                    {
                        Initialize(false);
                    }

                    if (_options.ExchangeOrQueue == Enums.ExchangeOrQueue.Exchange)
                    {
                        _channel.ExchangeDelete(exchange: _options.ExchangeName);
                    }
                    else if (_options.ExchangeOrQueue == Enums.ExchangeOrQueue.Queue)
                    {
                        _channel.QueueDelete(queue: _options.QueueName);
                    }

                    CreateExchangeOrQueueActual();
                }
                else
                {
                    throw ex;
                }
            }

        }

        public void SendMessage(RabbitMessageOutbound message)
        {
            try
            {
                IBasicProperties props = null;
                if (!string.IsNullOrEmpty(message.CorrelationId))
                {
                    props = _channel.CreateBasicProperties();
                    props.CorrelationId = message.CorrelationId;
                }

                if (_options.ExchangeOrQueue == Enums.ExchangeOrQueue.Exchange)
                {
                    _channel.BasicPublish(_options.ExchangeName, _options.RoutingKeys.Count > 0 ? _options.RoutingKeys.First() : "", _options.MandatoryPublish, props, new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(message.Message)));
                }
                else if (_options.ExchangeOrQueue == Enums.ExchangeOrQueue.Queue)
                {
                    _channel.BasicPublish("", _options.QueueName, _options.MandatoryPublish, props, new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(message.Message)));
                }
            }
            catch (AlreadyClosedException ex)
            {
                OnMessageFailed?.Invoke(this, new PublisherMessageReturnEventArgs(message, ex.Message));
            }
            catch (Exception ex)
            {
                OnMessageFailed?.Invoke(this, new PublisherMessageReturnEventArgs(message, ex.Message));
            }
        }

        public void Dispose()
        {
            Cleanup();

            try
            {
                if (_channel != null && _channel.IsOpen && !_channel.IsClosed)
                {
                    _channel.Close();
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Publisher Dispose");
            }
        }
    }
}
