using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqCore.Common;
using RabbitMqCore.Events;
using RabbitMqCore.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqCore
{
    public class Subscriber : ISubscriber
    {
        protected IQueueService _queueService;

        protected SubscriberOptions _options;

        readonly ILogger<IQueueService> _log;

        private Action<RabbitMessageInbound> _onMessage;

        private string _queueName = "";

        private AsyncEventingBasicConsumer _consumer;

        protected IModel _channel;

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
        }

        internal void Initialize(bool CreateExchangeQueue = true)
        {
            _channel = _queueService.Connection.CreateModel();

            _channel.BasicQos(_options.PrefetchSize, _options.PrefetchCount, false);

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

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.Received += _consumer_Received;
            _consumer.ConsumerCancelled += _consumer_ConsumerCancelled;
            _consumer.Registered += _consumer_Registered;
            _consumer.Shutdown += _consumer_Shutdown;
            _consumer.Unregistered += _consumer_Unregistered;
        }

        private Task _consumer_Unregistered(object sender, ConsumerEventArgs @event)
        {
            if (@event != null)
                _log.LogDebug("unregistered ctags count:[{0}]", @event.ConsumerTags.Length);

            return Task.CompletedTask;
        }

        private Task _consumer_Shutdown(object sender, ShutdownEventArgs @event)
        {
            if (@event != null)
                _log.LogDebug($"Consumer Model Shutdown! Reason [{@event.ReplyText}] | Initiator [{@event.Initiator}]");

            return Task.CompletedTask;
        }

        private Task _consumer_Registered(object sender, ConsumerEventArgs @event)
        {
            if (@event != null)
                _log.LogDebug("registered ctags count:[{0}]", @event.ConsumerTags.Length);

            return Task.CompletedTask;
        }

        private Task _consumer_ConsumerCancelled(object sender, ConsumerEventArgs @event)
        {
            if (@event != null)
                _log.LogDebug("cancelled ctags count:[{0}]", @event.ConsumerTags.Length);

            return Task.CompletedTask;
        }

        private Task _consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            try
            {
                var message = new RabbitMessageInbound();
                message.Message = Encoding.UTF8.GetString(@event.Body.ToArray());
                message.ConsumerTag = @event.ConsumerTag;
                message.DeliveryTag = @event.DeliveryTag;
                message.Exchange = @event.Exchange;
                message.Redelivered = @event.Redelivered;
                message.RoutingKey = @event.RoutingKey;
                //message.Bytes = @event.Body.ToArray();
                //message.BasicProperties = @event.BasicProperties;
                message.CorrelationId = @event.BasicProperties.CorrelationId;

                _onMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"_consumer_Received.");
            }

            return Task.CompletedTask;
        }

        internal void Cleanup()
        {
            // close consumer
            try
            {
                if (_consumer != null)
                {
                    _consumer.Received -= _consumer_Received;
                    _consumer.ConsumerCancelled -= _consumer_ConsumerCancelled;
                    _consumer.Registered -= _consumer_Registered;
                    _consumer.Shutdown -= _consumer_Shutdown;
                    _consumer.Unregistered -= _consumer_Unregistered;
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "error in consumer cleanup");
            }

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
                if (!_channel.IsClosed)
                {
                    _channel.Close();
                }
                _channel.Dispose();
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "error in events cleanup");
            }
        }

        private void _channel_ModelShutdown(object sender, ShutdownEventArgs e)
        {
            if (e != null)
                _log.LogDebug($"Subscriber Model Shutdown! Reason [{e.ReplyText}] | Initiator [{e.Initiator}]");
        }

        private void _channel_FlowControl(object sender, FlowControlEventArgs e)
        {
            if (e != null)
                _log.LogDebug("flow control: [{0}]", e.Active);
        }

        private void _channel_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (e != null)
                _log.LogDebug(e.Exception, string.Join(Environment.NewLine, e.Detail.Select(x => $"{x.Key} - {x.Value}")));
        }

        private void _channel_BasicReturn(object sender, BasicReturnEventArgs e)
        {
            if (e != null)
                _log.LogDebug("basic return correlationid: [{0}] reply text: [{1}]", e.BasicProperties.CorrelationId, e.ReplyText);
        }

        private void _channel_BasicRecoverOk(object sender, EventArgs e)
        {
            if (e != null)
                _log.LogDebug("basic recover ok");
        }

        private void _channel_BasicNacks(object sender, BasicNackEventArgs e)
        {
            if (e != null)
                _log.LogDebug("basic nack received for delivery tag: [{0}]", e.DeliveryTag);
        }

        private void _channel_BasicAcks(object sender, BasicAckEventArgs e)
        {
            if (e != null)
                _log.LogDebug("basic ack received for delivery tag: [{0}]", e.DeliveryTag);
        }

        private void CreateExchangeOrQueueActual()
        {
            if (_options.ExchangeOrQueue == Enums.ExchangeOrQueue.Exchange)
            {
                _channel.ExchangeDeclare(
                    exchange: _options.ExchangeName,
                    type: _options.ExchangeType.ToString(),
                    durable: _options.Durable,
                    autoDelete: _options.AutoDelete,
                    arguments: ParseExchangeArgs()
                    );

                // if queue name mentioned
                if (!string.IsNullOrEmpty(_options.QueueName))
                {
                    var result = _channel.QueueDeclare(
                        queue: _options.QueueName,
                        durable: _options.Durable,
                        exclusive: _options.Exclusive,
                        autoDelete: _options.AutoDelete,
                        arguments: ParseQueueArguments()
                    );

                    _channel.QueueBind(
                        queue: _options.QueueName,
                        exchange: _options.ExchangeName,
                        routingKey: _options.RoutingKeys.Count > 0 ? _options.RoutingKeys.First() : "",
                        arguments: null
                    );

                    _queueName = result.QueueName;
                }

                // if no queue name then create temp queue and bind
                else
                {
                    var result = _channel.QueueDeclare(
                        durable: _options.Durable,
                        exclusive: true,
                        autoDelete: true,
                        arguments: ParseQueueArguments()
                    );

                    _channel.QueueBind(
                        queue: result.QueueName,
                        exchange: _options.ExchangeName,
                        routingKey: _options.RoutingKeys.Count > 0 ? _options.RoutingKeys.First() : "",
                        arguments: null
                    );

                    _queueName = result.QueueName;
                }
            }
            else if (_options.ExchangeOrQueue == Enums.ExchangeOrQueue.Queue)
            {
                _channel.QueueDeclare(
                    queue: _options.QueueName,
                    durable: _options.Durable,
                    exclusive: _options.Exclusive,
                    autoDelete: _options.AutoDelete,
                    arguments: ParseQueueArguments()
                    );
            }
        }

        private IDictionary<string, object> ParseExchangeArgs()
        {
            IDictionary<string, object> args = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(_options.AlternateExchange))
            {
                args.Add(ArgumentStrings.AlternateExchange, _options.AlternateExchange);
            }

            return args;
        }

        private IDictionary<string, object> ParseQueueArguments()
        {
            IDictionary<string, object> args = new Dictionary<string, object>();

            if (_options.MessageTTL > 0)
            {
                args.Add(ArgumentStrings.MessageTTL, _options.MessageTTL);
            }

            if (_options.AutoExpires > 0)
            {
                args.Add(ArgumentStrings.AutoExpires, _options.AutoExpires);
            }

            if (_options.MaxLength > 0)
            {
                args.Add(ArgumentStrings.MaxLength, _options.MaxLength);
            }

            if (_options.MaxLengthBytes > 0)
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

            return args;
        }

        /// <summary>
        /// 
        /// </summary>
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

                    if (_options.ExchangeOrQueue == Enums.ExchangeOrQueue.Exchange && !string.IsNullOrWhiteSpace(_options.QueueName))
                    {
                        _channel.QueueDelete(queue: _options.QueueName);
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

        /// <summary>
        /// 
        /// </summary>
        public void Unsubscribe()
        {
            try
            {
                if (_consumer?.ConsumerTags?.Length > 0)
                {
                    _log.LogDebug("unsubscribing");
                    _log.LogDebug("consumer tags count:{0}", _consumer.ConsumerTags.Length);
                    foreach (var tag in _consumer.ConsumerTags)
                    {
                        _log.LogDebug("tags:{0}", tag);
                        _channel.BasicCancel(tag);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unsubscribe");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        public void Subscribe(Action<RabbitMessageInbound> action)
        {
            _onMessage = action;

            string tag = _channel.BasicConsume(_queueName, _options.AutoAck, _consumer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DeliveryTag"></param>
        public void Acknowledge(ulong deliveryTag)
        {
            if (_options.AutoAck)
            {
                return;
            }

            try
            {
                _channel.BasicAck(deliveryTag, false);
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Acknowledge deliverytag:{0}", deliveryTag);
                // todo: throw ex
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DeliveryTag"></param>
        public void NotAcknowledge(ulong deliveryTag)
        {
            if (_options.AutoAck)
            {
                return;
            }

            try
            {
                _channel.BasicNack(deliveryTag, false, _options.RequeueNack);
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "NotAcknowledge deliverytag:{0}", deliveryTag);
                // todo: throw ex
            }
        }

        /// <summary>
        /// 
        /// </summary>
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
                _log.LogDebug(ex, "Subscriber Dispose");
            }
        }
    }
}
