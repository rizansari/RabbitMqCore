using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqCore.Events;
using RabbitMqCore.Exceptions;
using RabbitMqCore.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMqCore
{
    public class QueueService : IQueueService
    {
        readonly ILogger<QueueService> _log;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="loggerFactory"></param>
        public QueueService(RabbitMQCoreOptions Options, ILoggerFactory loggerFactory)
        {
            this.Options = Options;
            _log = loggerFactory.CreateLogger<QueueService>();

            Reconnect();
        }

        IConnection _connection;
        IModel _sendChannel;
        IModel _consumeChannel;
        AsyncEventingBasicConsumer _consumer;
        bool _connectionBlocked = false;

        public event Action OnConnectionShutdown;
        public event Action OnReconnected;
        //public event Action<object> onMessage;

        public Dictionary<string, Action<RabbitMessageEventArgs>> _consumers;

        int _reconnectAttemptsCount = 0;

        public IConnection Connection
        {
            get { return _connection; }
        }

        public IModel SendChannel
        {
            get { return _sendChannel; }
        }

        public RabbitMQCoreOptions Options { get; }

        /// <summary>
        /// 
        /// </summary>
        public void Cleanup()
        {
            _log.LogDebug("Cleaning up old connection and channels.");
            try
            {
                if (_connection != null)
                {
                    _connection.ConnectionShutdown -= Connection_ConnectionShutdown;
                    _connection.CallbackException -= Connection_CallbackException;
                    _connection.ConnectionBlocked -= Connection_ConnectionBlocked;
                }
                // Closing send channel.
                if (_sendChannel != null)
                {
                    _sendChannel.CallbackException -= Channel_CallbackException;
                }

                OnConnectionShutdown?.Invoke();
                // Closing connection.
                if (_connection?.IsOpen == true)
                    _connection.Close(TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error closing connection.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Connect()
        {
            try
            {
                if (_connection?.IsOpen == true)
                {
                    _log.LogWarning("Connection already open.");
                    return;
                }

                // connection
                _log.LogInformation("Connecting to RabbitMQ endpoint {0}.", Options.HostName);
                var factory = new ConnectionFactory
                {
                    HostName = Options.HostName,
                    UserName = Options.UserName,
                    Password = Options.Password,
                    RequestedHeartbeat = TimeSpan.FromSeconds(Options.RequestedHeartbeat),
                    RequestedConnectionTimeout = TimeSpan.FromMilliseconds(Options.RequestedConnectionTimeout),
                    AutomaticRecoveryEnabled = false,
                    TopologyRecoveryEnabled = false,
                    Port = Options.Port,
                    VirtualHost = Options.VirtualHost,
                    DispatchConsumersAsync = true
                };
                _connection = factory.CreateConnection();

                // connection events
                _connection.ConnectionShutdown += Connection_ConnectionShutdown;
                _connection.ConnectionBlocked += Connection_ConnectionBlocked;
                _connection.CallbackException += Connection_CallbackException;

                _log.LogDebug("Connection opened.");

                // send channel
                _sendChannel = Connection.CreateModel();
                _sendChannel.CallbackException += Channel_CallbackException;
                _sendChannel.BasicQos(0, Options.PrefetchCount, false);

                // consume channel
                _consumeChannel = Connection.CreateModel();
                _consumeChannel.CallbackException += Channel_CallbackException;
                _consumeChannel.BasicQos(0, Options.PrefetchCount, false);

                _consumer = new AsyncEventingBasicConsumer(_consumeChannel);
                _consumer.Received += _consumer_Received;

                _consumers = new Dictionary<string, Action<RabbitMessageEventArgs>>();

                _connectionBlocked = false;

                CheckSendChannelOpened();

                _log.LogInformation("Connected to RabbitMQ endpoint {0}", Options.HostName);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error closing connection.");
            }
        }

        

        private void Channel_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (e != null)
                _log.LogError(e.Exception, string.Join(Environment.NewLine, e.Detail.Select(x => $"{x.Key} - {x.Value}")));
        }

        private void Connection_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (e != null)
                _log.LogError(e.Exception, e.Exception.Message);
        }

        private void Connection_ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (e != null)
                _log.LogError($"Connection blocked! Reason: {0}", e.Reason);
            _connectionBlocked = true;
            Reconnect();
        }

        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (e != null)
                _log.LogError($"Connection broke! Reason: {0}", e.ReplyText);

            Reconnect();
        }

        void CheckSendChannelOpened()
        {
            if (_sendChannel is null || _sendChannel.IsClosed)
                throw new NotConnectedException("Channel not opened.");

            if (_connectionBlocked)
                throw new NotConnectedException("Connection is blocked.");
        }

        /// <summary>
        /// 
        /// </summary>
        void Reconnect()
        {
            _log.LogDebug("Reconnect requested");
            Cleanup();

            var mres = new ManualResetEventSlim(false); // state is initially false

            while (!mres.Wait(Options.ReconnectionTimeout)) // loop until state is true, checking every Options.ReconnectionTimeout
            {
                if (_reconnectAttemptsCount > Options.ReconnectionAttemptsCount)
                    throw new ReconnectAttemptsExceededException($"Max reconnect attempts {Options.ReconnectionAttemptsCount} reached.");

                try
                {
                    _log.LogDebug($"Trying to connect with reconnect attempt {0}", _reconnectAttemptsCount);
                    Connect();
                    _reconnectAttemptsCount = 0;
                    OnReconnected?.Invoke();
                    break;
                    //mres.Set(); // state set to true - breaks out of loop
                }
                catch (Exception e)
                {
                    _reconnectAttemptsCount++;
                    Thread.Sleep(Options.ReconnectionTimeout);
                    _log.LogCritical(e, $"Connection failed. Detais: {e.Message}. Reconnect attempts: {_reconnectAttemptsCount}", e);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Cleanup();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="options"></param>
        public void SendMessage(string payload, PublisherOptions options)
        {
            try
            {
                if (options.ExchangeOrQueue == Enums.ExchangeOrQueue.Exchange)
                {
                    _sendChannel.BasicPublish(options.ExchangeName, options.RoutingKeys.Count > 0 ? options.RoutingKeys.First() : "", null, new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(payload)));
                }
                else if (options.ExchangeOrQueue == Enums.ExchangeOrQueue.Queue)
                {
                    _sendChannel.BasicPublish("", options.QueueName, null, new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(payload)));
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Send message failed. {options.ExchangeName}/{options.QueueName}");
                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public IPublisher CreatePublisher(Action<PublisherOptions> options)
        {

            try
            {
                var temp = new PublisherOptions();
                options(temp);
                return new Publisher(this, temp);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Create publisher failed.");
                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public void CreateExchangeOrQueue(PublisherOptions options)
        {
            try
            {
                if (options.ExchangeOrQueue == Enums.ExchangeOrQueue.Exchange)
                {
                    _sendChannel.ExchangeDeclare(
                        exchange: options.ExchangeName,
                        type: options.ExchangeType.ToString(),
                        durable: options.Durable,
                        autoDelete: options.AutoDelete,
                        arguments: options.Arguments
                        );
                }
                else if (options.ExchangeOrQueue == Enums.ExchangeOrQueue.Queue)
                {
                    _sendChannel.QueueDeclare(
                        queue: options.QueueName,
                        durable: options.Durable,
                        exclusive: options.Exclusive,
                        autoDelete: options.AutoDelete,
                        arguments: options.Arguments
                        );
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Create Exchange or Queue failed. {options.ExchangeName}/{options.QueueName}");
                throw ex;
            }
        }

        public ISubscriber CreateSubscriber(Action<SubscriberOptions> options)
        {
            try
            {
                var temp = new SubscriberOptions();
                options(temp);
                return new Subscriber(this, temp);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Create subscriber failed.");
                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public void CreateExchangeOrQueue(SubscriberOptions options)
        {
            try
            {
                if (options.ExchangeOrQueue == Enums.ExchangeOrQueue.Exchange)
                {
                    _sendChannel.ExchangeDeclare(
                        exchange: options.ExchangeName,
                        type: options.ExchangeType.ToString(),
                        durable: options.Durable,
                        autoDelete: options.AutoDelete,
                        arguments: options.Arguments
                        );

                    // if queue name mentioned
                    if (!string.IsNullOrEmpty(options.QueueName))
                    {
                        _sendChannel.QueueDeclare(
                            queue: options.QueueName,
                            durable: options.Durable,
                            exclusive: options.Exclusive,
                            autoDelete: options.AutoDelete,
                            arguments: options.Arguments
                        );

                        _sendChannel.QueueBind(
                            queue: options.QueueName,
                            exchange: options.ExchangeName,
                            routingKey: options.RoutingKeys.Count > 0 ? options.RoutingKeys.First() : "",
                            arguments: options.Arguments
                        );
                    }
                    // if no queue name then create temp queue and bind
                    else
                    {
                        var result = _sendChannel.QueueDeclare(
                            durable: options.Durable,
                            exclusive: true,
                            autoDelete: true,
                            arguments: options.Arguments
                        );

                        _sendChannel.QueueBind(
                            queue: result.QueueName,
                            exchange: options.ExchangeName,
                            routingKey: options.RoutingKeys.Count > 0 ? options.RoutingKeys.First() : "",
                            arguments: options.Arguments
                        );

                        options.QueueName = result.QueueName;
                    }
                }
                else if (options.ExchangeOrQueue == Enums.ExchangeOrQueue.Queue)
                {
                    _sendChannel.QueueDeclare(
                        queue: options.QueueName,
                        durable: options.Durable,
                        exclusive: options.Exclusive,
                        autoDelete: options.AutoDelete,
                        arguments: options.Arguments
                        );
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Create Exchange or Queue failed. {options.ExchangeName}/{options.QueueName}");
                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="onMessage"></param>
        public void Subscribe(SubscriberOptions options, Action<RabbitMessageEventArgs> onMessage)
        {
            if (options == null)
                throw new ArgumentException($"{nameof(options)} is null.", nameof(options));

            if (onMessage == null)
                throw new ArgumentException($"{nameof(onMessage)} is null.", nameof(onMessage));

            string tag = _consumeChannel.BasicConsume(options.QueueName, true, _consumer);
            options.ConsumerTag = tag;
            _consumers.Add(tag, onMessage);
        }

        private Task _consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            var onMessage = _consumers[@event.ConsumerTag];

            var message = new RabbitMessageEventArgs();
            message.Message = Encoding.UTF8.GetString(@event.Body.ToArray());
            message.ConsumerTag = @event.ConsumerTag;
            message.DeliveryTag = @event.DeliveryTag;
            message.Exchange = @event.Exchange;
            message.Redelivered = @event.Redelivered;
            message.RoutingKey = @event.RoutingKey;
            //message.Bytes = @event.Body.ToArray();
            //message.BasicProperties = @event.BasicProperties;
            message.CorrelationId = @event.BasicProperties.CorrelationId;

            onMessage.Invoke(message);

            return Task.CompletedTask;
        }

        public void Unsubscribe(SubscriberOptions options)
        {
            if (options == null)
                throw new ArgumentException($"{nameof(options)} is null.", nameof(options));

            _consumeChannel.BasicCancel(options.ConsumerTag);
        }
    }
}
