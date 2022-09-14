using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
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
        protected readonly ILogger<QueueService> _log;


        public RabbitMQCoreOptions Options { get; }


        private IConnection _connection;
        
        public bool IsConnected 
        { 
            get 
            {
                if (_connection != null)
                {
                    return _connection.IsOpen;
                }
                return false;
            } 
        }

        IConnection IQueueService.Connection
        {
            get { return _connection; }
        }

        public event Action OnConnectionShutdown;
        public event Action OnReconnected;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="loggerFactory"></param>
        public QueueService(RabbitMQCoreOptions Options, ILoggerFactory loggerFactory)
        {
            this.Options = Options;

            if (Options.DebugMode)
            {
                _log = loggerFactory.CreateLogger<QueueService>();
            }
            else
            {
                _log = NullLoggerFactory.Instance.CreateLogger<QueueService>();
            }

            if (Options.ConnectOnConstruction)
            {
                Connect();
            }
        }

        public void Connect()
        {
            try
            {
                if (_connection?.IsOpen == true)
                {
                    _log.LogDebug("Connection already open.");
                    return;
                }

                // connection
                _log.LogDebug("Connecting to RabbitMQ endpoint {0}.", Options.HostName);
                var factory = new ConnectionFactory
                {
                    HostName = Options.HostName,
                    UserName = Options.UserName,
                    Password = Options.Password,
                    RequestedHeartbeat = TimeSpan.FromSeconds(Options.RequestedHeartbeat),
                    RequestedConnectionTimeout = TimeSpan.FromMilliseconds(Options.RequestedConnectionTimeout),
                    AutomaticRecoveryEnabled = Options.AutomaticRecoveryEnabled,
                    NetworkRecoveryInterval = Options.NetworkRecoveryInterval,
                    TopologyRecoveryEnabled = Options.TopologyRecoveryEnabled,
                    Port = Options.Port,
                    VirtualHost = Options.VirtualHost,
                    DispatchConsumersAsync = Options.DispatchConsumersAsync, //todo: can only be used with IAsyncBasicConsumer
                    ClientProvidedName = Options.ClientProvidedName
                };
                _connection = factory.CreateConnection();

                // connection events
                _connection.ConnectionShutdown += Connection_ConnectionShutdown;
                _connection.ConnectionBlocked += Connection_ConnectionBlocked;
                _connection.CallbackException += Connection_CallbackException;
                _connection.ConnectionUnblocked += Connection_ConnectionUnblocked;


                _log.LogDebug("Connected to RabbitMQ endpoint {0}", Options.HostName);
            }
            catch (BrokerUnreachableException ex)
            {
                _log.LogDebug(ex, "Error in Connect.");

                if (Options.ThrowIfNotConnected)
                {
                    throw new NotConnectedException("", ex);
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Error in Connect.");
                throw ex;
            }
        }

        

        public void Dispose()
        {
            Cleanup();
        }

        

        /// <summary>
        /// 
        /// </summary>
        private void Cleanup()
        {
            _log.LogDebug("Cleaning up old connection and channels.");
            try
            {
                if (_connection != null)
                {
                    _connection.ConnectionShutdown -= Connection_ConnectionShutdown;
                    _connection.CallbackException -= Connection_CallbackException;
                    _connection.ConnectionBlocked -= Connection_ConnectionBlocked;
                    _connection.ConnectionUnblocked -= Connection_ConnectionUnblocked;
                }

                OnConnectionShutdown?.Invoke();
                
                // Closing connection.
                if (_connection?.IsOpen == true)
                    _connection.Close(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Error closing connection.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connection_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (e != null)
                _log.LogDebug(e.Exception, e.Exception.Message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connection_ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (e != null)
                _log.LogDebug($"Connection blocked! Reason [{e.Reason}]");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (e != null)
                _log.LogDebug($"Connection broke! Reason [{e.ReplyText}] | Initiator [{e.Initiator}]");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connection_ConnectionUnblocked(object sender, EventArgs e)
        {
            if (e != null)
                _log.LogDebug($"Connection unblocked!");
        }

        #region Publisher
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
                var publisher = new Publisher(this, temp, _log);
                publisher.Initialize();
                return publisher;
            }
            catch (OperationInterruptedException ex)
            {
                _log.LogDebug(ex, "Create publisher failed.");
                throw ex;
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Create publisher failed.");
                throw ex;
            }
        }
        #endregion


        #region Subscriber
        public ISubscriber CreateSubscriber(Action<SubscriberOptions> options)
        {
            try
            {
                var temp = new SubscriberOptions();
                options(temp);
                var subscriber = new Subscriber(this, temp, _log);
                subscriber.Initialize();
                return subscriber;
            }
            catch (OperationInterruptedException ex)
            {
                _log.LogDebug(ex, "Create subscriber failed.");
                throw ex;
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Create subscriber failed.");
                throw ex;
            }
        }
        #endregion

    }
}
