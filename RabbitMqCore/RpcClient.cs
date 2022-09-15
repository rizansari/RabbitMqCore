using Microsoft.Extensions.Logging;
using RabbitMqCore.Common;
using RabbitMqCore.Common.Queues;
using RabbitMqCore.Events;
using RabbitMqCore.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RabbitMqCore
{
    public class RpcClient : IRpcClient
    {
        protected IQueueService _queueService;

        protected RpcOptions _options;

        readonly ILogger<IQueueService> _log;

        protected IPublisher _publisher;
        protected ISubscriber _subscriber;

        protected Thread _th = null;
        protected CancellationTokenSource _source = new CancellationTokenSource();
        protected CancellationToken _token;

        public RpcClient(
            IQueueService queueService,
            RpcOptions options,
            ILogger<IQueueService> log)
        {
            _queueService = queueService;
            _options = options;
            _log = log;
        }

        private RabbitMessageInbound _replyMessage = null;

        private object _lock = new object();

        private Dictionary<string, Action<RabbitMessageInbound>> _actions = new Dictionary<string, Action<RabbitMessageInbound>>();

        private DelayQueue<string> _ids = new DelayQueue<string>();

        internal void Initialize()
        {
            _token = _source.Token;

            _th = new Thread(Run);
            _th.Name = string.Format("[{0}]", GetType());
            _th.IsBackground = true;
            _th.Start();


            _publisher = _queueService.CreatePublisher(options =>
            {
                options.ExchangeOrQueue = Enums.ExchangeOrQueue.Queue;
                options.QueueName = string.Format(ArgumentStrings.RpcQueueNameRequest, _options.RpcName);
            });


            _subscriber = _queueService.CreateSubscriber(options =>
            {
                options.ExchangeOrQueue = Enums.ExchangeOrQueue.Queue;
                options.QueueName = string.Format(ArgumentStrings.RpcQueueNameResponse, _options.RpcName);
            });

            _subscriber.Subscribe(msg =>
            {
                try
                {
                    Action<RabbitMessageInbound>action;
                    if (_actions.TryGetValue(msg.CorrelationId, out action))
                    {
                        action.Invoke(msg);
                        _actions.Remove(msg.CorrelationId);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "subscribe");
                }
            });
        }

        private void Run()
        {
            _log.LogDebug("starting delay thread");
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    try
                    {
                        string id = "";
                        lock (_lock)
                        {
                            id = _ids.Dequeue();
                        }

                        if (!string.IsNullOrEmpty(id))
                        {
                            Action<RabbitMessageInbound> action;
                            if (_actions.TryGetValue(id, out action))
                            {
                                action.Invoke(new RabbitMessageInbound() { CorrelationId = id, Message = "FAILED" });
                                _actions.Remove(id);
                            }
                        }

                        Thread.Sleep(1000); // todo: get from config
                    }
                    catch (OperationCanceledException)
                    {
                        _log.LogDebug("Operation Cancelled delay thread {0} thread", Thread.CurrentThread.Name);
                    }
                    catch (Exception ex)
                    {
                        _log.LogDebug(ex, string.Format("error in delay thread {0} thread", Thread.CurrentThread.Name));
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "error in delay thread");
            }
        }

        public void Call(RabbitMessageOutbound @object, Action<RabbitMessageInbound> action, int Delay = 5000)
        {
            try
            {
                _actions.Add(@object.CorrelationId, action);
                lock (_lock)
                {
                    _ids.Enqueue(@object.CorrelationId, Delay);
                }
                _publisher.SendMessage(@object);
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Call");
            }
        }

        public void Dispose()
        {
            try
            {
                _source.Cancel();
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Dispose 1");
            }

            try
            {
                _subscriber.Unsubscribe();
                _subscriber.Dispose();
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Dispose 2");
            }

            try
            {
                _publisher.Dispose();
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Dispose 3");
            }
        }
    }
}
