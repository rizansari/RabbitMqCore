using Microsoft.Extensions.DependencyInjection;
using System;
using RabbitMqCore.DependencyInjectionExtensions;
using RabbitMqCore;
using Microsoft.Extensions.Logging;
using Log4NetCore;
using Newtonsoft.Json;
using RabbitMqCore.Events;
using RabbitMqCore.Common;
using System.Collections.Generic;
using System.Threading;
using RabbitMqCore.Exceptions;

namespace RabbitMqCoreConsole
{
    class Program
    {
        private static IQueueService rmq;

        private static CancellationTokenSource _source = new CancellationTokenSource();
        private static CancellationToken _token = _source.Token;

        private static ISubscriber _subscriber;

        private static Queue<string> _queue;

        #region Cluster
        static void Main(string[] args)
        {
            try
            {

                var hostnames = new List<string>();
                hostnames.Add("rabbit01");
                hostnames.Add("rabbit02");
                //setup our DI
                var serviceProvider = new ServiceCollection()
                    .AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                        loggingBuilder.AddLog4Net();
                    })
                    .AddRabbitMQCore(options =>
                    {
                        options.HostName = "localhost";
                        //options.HostNames = hostnames;
                        options.UserName = "client";
                        options.Password = "client";
                        options.AutomaticRecoveryEnabled = true;
                        options.TopologyRecoveryEnabled = true;
                        options.NetworkRecoveryInterval = TimeSpan.FromSeconds(5);
                        options.DebugMode = true;
                        options.ClientProvidedName = "rabbitmq-console";
                    })
                    .BuildServiceProvider();

                var logger = serviceProvider.GetService<ILoggerFactory>()
                    .CreateLogger<Program>();

                // get QueueService
                rmq = serviceProvider.GetRequiredService<IQueueService>();

                rmq.OnConnectionShutdown += Rmq_OnConnectionShutdown;

                if (args[0] == "p")
                {
                    Thread thread = new Thread(new ThreadStart(RunPublisher));
                    thread.Start();

                    Console.ReadLine();

                    _source.Cancel();
                }

                if (args[0] == "s")
                {
                    _subscriber = rmq.CreateSubscriber(options =>
                    {
                        options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                        options.ExchangeName = "exchange.1";
                        options.QueueName = "queue.1";
                        options.AutoAck = true;
                    });
                    _subscriber.Subscribe(opt =>
                    {
                        Console.WriteLine("sub called: {0} redelivered: {1}", opt.ToString(), opt.Redelivered);
                    });

                    Console.ReadLine();

                    _subscriber.Unsubscribe();
                }


                Console.ReadLine();

                _source.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void RunPublisher()
        {
            try
            {
                var pub = rmq.CreatePublisher(options =>
                {
                    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                    options.ExchangeName = "exchange.1";
                    options.ExchangeType = RabbitMqCore.Enums.ExchangeType.fanout;
                    options.MandatoryPublish = true;
                    options.RecreateIfFailed = true;
                });

                pub.OnMessageFailed += Pub_OnMessageFailedOrReturned;
                pub.OnMessageReturn += Pub_OnMessageFailedOrReturned;

                SimpleObject obj = null;
                RabbitMessageOutbound message = null;

                int count = 1;
                while (!_token.IsCancellationRequested)
                {
                    try
                    {
                        obj = new SimpleObject() { ID = count++, Name = "One" };
                        message = new RabbitMessageOutbound()
                        {
                            CorrelationId = $"CorrelationId:{obj.ID}",
                            Message = JsonConvert.SerializeObject(obj)
                        };
                        pub.SendMessage(message);
                        Console.WriteLine("pub called: {0}", message.Message);
                        Thread.Sleep(2000);
                    }
                    catch (OutboundMessageFailedException ex)
                    {
                        Console.WriteLine("OutboundMessageFailedException Message failed {0}:{1}", obj.ID, ex.RabbitMessageOutbound.Message);
                        Thread.Sleep(2000);
                    }
                    catch (NotConnectedException ex)
                    {
                        Console.WriteLine("NotConnectedException Message failed {0}", obj.ID);
                        Thread.Sleep(2000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Message failed {0}", obj.ID);
                        Thread.Sleep(2000);
                    }
                }

                pub.Dispose();

                Console.WriteLine("pub count: {0}", count - 1);

                Console.WriteLine("token cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine("create publisher failed");
            }
        }
        #endregion

        #region rpc
        //static void MainRPC(string[] args)
        //{
        //    try
        //    {
        //        //setup our DI
        //        var serviceProvider = new ServiceCollection()
        //            .AddLogging(loggingBuilder =>
        //            {
        //                loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        //                loggingBuilder.AddLog4Net();
        //            })
        //            .AddRabbitMQCore(options =>
        //            {
        //                options.HostName = "localhost";
        //                options.UserName = "client";
        //                options.Password = "client";
        //                options.AutomaticRecoveryEnabled = true;
        //                options.TopologyRecoveryEnabled = true;
        //                options.NetworkRecoveryInterval = TimeSpan.FromSeconds(5);
        //                options.DebugMode = true;
        //                options.ClientProvidedName = "rabbitmq-console";
        //            })
        //            .BuildServiceProvider();

        //        var logger = serviceProvider.GetService<ILoggerFactory>()
        //            .CreateLogger<Program>();

        //        // get QueueService
        //        rmq = serviceProvider.GetRequiredService<IQueueService>();

        //        rmq.OnConnectionShutdown += Rmq_OnConnectionShutdown;

        //        Thread thread = new Thread(new ThreadStart(RunRpcClient));
        //        thread.Start();

        //        var rpcServer = rmq.CreateRpcServer(options =>
        //        {
        //            options.RpcName = "TEST_RPC";
        //        });

        //        rpcServer.Subscribe(request =>
        //        {
        //            var obj = JsonConvert.DeserializeObject<SimpleObject>(request.Message);
        //            obj.Name = "Response";
        //            rpcServer.Respond(new RabbitMessageOutbound() { CorrelationId = request.CorrelationId, Message = JsonConvert.SerializeObject(obj) });
        //            Console.WriteLine("message:{0}", request.Message);
        //        });

        //        Console.ReadLine();

        //        rpcServer.Dispose();

        //        _source.Cancel();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex);
        //    }
        //}

        //private static void RunRpcClient()
        //{
        //    try
        //    {
        //        var rpcClient = rmq.CreateRpcClient(options =>
        //        {
        //            options.RpcName = "TEST_RPC";
        //        });

        //        SimpleObject obj = null;
        //        RabbitMessageOutbound message = null;

        //        int count = 1;
        //        while (!_token.IsCancellationRequested)
        //        {
        //            try
        //            {
        //                obj = new SimpleObject() { ID = count++, Name = "Request" };
        //                message = new RabbitMessageOutbound()
        //                {
        //                    CorrelationId = $"CorrelationId:{obj.ID}",
        //                    Message = JsonConvert.SerializeObject(obj)
        //                };
        //                rpcClient.Call(message, response => {
        //                    Console.WriteLine("rpc response: {0}", response.Message);
        //                }, 10000);
        //                Console.WriteLine("rpc request: {0}", message.Message);
        //                Thread.Sleep(2000);
        //            }
        //            catch (OutboundMessageFailedException ex)
        //            {
        //                Console.WriteLine("OutboundMessageFailedException Message failed {0}:{1}", obj.ID, ex.RabbitMessageOutbound.Message);
        //                Thread.Sleep(2000);
        //            }
        //            catch (NotConnectedException ex)
        //            {
        //                Console.WriteLine("NotConnectedException Message failed {0}", obj.ID);
        //                Thread.Sleep(2000);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine("Message failed {0}", obj.ID);
        //                Thread.Sleep(2000);
        //            }
        //        }

        //        rpcClient.Dispose();

        //        Console.WriteLine("rpc count: {0}", count - 1);

        //        Console.WriteLine("token cancelled");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("create rpc client failed");
        //    }
        //}
        #endregion

        #region revamped library
        static void MainRevamped(string[] args)
        {
            try
            {
                //setup our DI
                var serviceProvider = new ServiceCollection()
                    .AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                        loggingBuilder.AddLog4Net();
                    })
                    .AddRabbitMQCore(options =>
                    {
                        options.HostName = "localhost";
                        options.UserName = "client";
                        options.Password = "client";
                        options.AutomaticRecoveryEnabled = true;
                        options.TopologyRecoveryEnabled = true;
                        options.NetworkRecoveryInterval = TimeSpan.FromSeconds(5);
                        options.DebugMode = true;
                        options.ClientProvidedName = "rabbitmq-console";
                    })
                    .BuildServiceProvider();

                var logger = serviceProvider.GetService<ILoggerFactory>()
                    .CreateLogger<Program>();

                // get QueueService
                rmq = serviceProvider.GetRequiredService<IQueueService>();

                rmq.OnConnectionShutdown += Rmq_OnConnectionShutdown;

                Thread thread = new Thread(new ThreadStart(Run));
                thread.Start();

                _subscriber = rmq.CreateSubscriber(options =>
                {
                    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                    options.ExchangeName = "exchange.1";
                    options.ExchangeType = RabbitMqCore.Enums.ExchangeType.fanout;
                    options.QueueName = "queue.1";
                    options.AutoAck = false;
                    //options.MaxLength = 5;
                    options.RecreateIfFailed = true;
                    options.PrefetchCount = 10;
                });

                _subscriber.Subscribe(response => 
                {
                    Console.WriteLine("message:{0}", response.Message);
                    _subscriber.Acknowledge(response.DeliveryTag);
                });

                Console.ReadLine();

                _subscriber.Unsubscribe();
                _subscriber.Dispose();

                _source.Cancel();
            }
            catch (NotConnectedException ex)
            {
                Console.WriteLine("Not Connected");
            }
            catch (ReconnectAttemptsExceededException ex)
            {
                Console.WriteLine("ReconnectAttemptsExceededException");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void Rmq_OnReconnected()
        {
            Console.WriteLine("reconnected");
        }

        private static void Rmq_OnConnectionShutdown()
        {
            Console.WriteLine("disconnected");
        }

        private static void Run()
        {
            try
            {
                var pub = rmq.CreatePublisher(options =>
                    {
                        options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                        options.ExchangeName = "exchange.1";
                        options.ExchangeType = RabbitMqCore.Enums.ExchangeType.fanout;
                        options.MandatoryPublish = true;
                        options.RecreateIfFailed = true;

                        //options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Queue;
                        //options.QueueName = "queue.1";
                        //options.MessageTTL = 5000;
                        //options.DeadLetterExchange = "exchange.2";
                        //options.RecreateIfFailed = true;
                    });

                pub.OnMessageFailed += Pub_OnMessageFailedOrReturned;
                pub.OnMessageReturn += Pub_OnMessageFailedOrReturned;

                SimpleObject obj = null;
                RabbitMessageOutbound message = null;

                int count = 1;
                while (!_token.IsCancellationRequested)
                {
                    try
                    {
                        obj = new SimpleObject() { ID = count++, Name = "One" };
                        message = new RabbitMessageOutbound()
                        {
                            CorrelationId = $"CorrelationId:{obj.ID}",
                            Message = JsonConvert.SerializeObject(obj)
                        };
                        pub.SendMessage(message);
                        Console.WriteLine("pub called: {0}", message.Message);
                        Thread.Sleep(2000);
                    }
                    catch (OutboundMessageFailedException ex)
                    {
                        Console.WriteLine("OutboundMessageFailedException Message failed {0}:{1}", obj.ID, ex.RabbitMessageOutbound.Message);
                        Thread.Sleep(2000);
                    }
                    catch (NotConnectedException ex)
                    {
                        Console.WriteLine("NotConnectedException Message failed {0}", obj.ID);
                        Thread.Sleep(2000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Message failed {0}", obj.ID);
                        Thread.Sleep(2000);
                    }
                }

                pub.Dispose();

                Console.WriteLine("pub count: {0}", count - 1);

                Console.WriteLine("token cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine("create publisher failed");
            }
        }

        private static void Pub_OnMessageFailedOrReturned(object sender, PublisherMessageReturnEventArgs e)
        {
            Console.WriteLine("Failed: reason:[{0}] message:[{1}]", e.Reason, e.Message.Message);
        }
        #endregion

        #region with args s and p
        static void MainWIthArgs(string[] args)
        {
            try
            {
                //setup our DI
                var serviceProvider = new ServiceCollection()
                    .AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                        loggingBuilder.AddLog4Net();
                    })
                    .AddRabbitMQCore(options =>
                    {
                        options.HostName = "localhost";
                        options.UserName = "guest";
                        options.Password = "guest";
                        //options.ReconnectionAttemptsCount = 5;
                        //options.ReconnectionTimeout = 1000;
                        //options.PrefetchCount = 5;
                    })
                    .BuildServiceProvider();

                var logger = serviceProvider.GetService<ILoggerFactory>()
                    .CreateLogger<Program>();

                // get QueueService
                rmq = serviceProvider.GetRequiredService<IQueueService>();

                //rmq.OnConnectionShutdown += Rmq_OnConnectionShutdown;
                //rmq.OnReconnected += Rmq_OnReconnected;

                if (args[0] == "p")
                {
                    Thread thread = new Thread(new ThreadStart(Run));
                    thread.Start();

                    Console.ReadLine();
                    
                    _source.Cancel();
                }

                if (args[0] == "s")
                {
                    _subscriber = rmq.CreateSubscriber(options =>
                    {
                        options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                        options.ExchangeName = "exchange.1";
                        options.QueueName = "queue.1";
                        options.AutoAck = false;
                    });
                    _subscriber.Subscribe(opt =>
                    {
                        Console.WriteLine("sub called: {0} redelivered: {1}", opt.ToString(), opt.Redelivered);

                        if (args.Length > 1 && args[1] == "a")
                        {
                            Console.WriteLine("acknowledging: {0}", opt.DeliveryTag);
                            _subscriber.Acknowledge(opt.DeliveryTag);
                        }
                        else
                        {
                            Console.WriteLine("not acknowledging: {0}", opt.DeliveryTag);
                            _subscriber.NotAcknowledge(opt.DeliveryTag);
                        }
                    });

                    Console.ReadLine();

                    _subscriber.Unsubscribe();
                }
            }
            catch (ReconnectAttemptsExceededException ex)
            {
                Console.WriteLine("ReconnectAttemptsExceededException");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        #endregion

        #region auto ack false
        static void MainAckFalse(string[] args)
        {
            try
            {
                //setup our DI
                var serviceProvider = new ServiceCollection()
                    .AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                        loggingBuilder.AddLog4Net();
                    })
                    .AddRabbitMQCore(options =>
                    {
                        options.HostName = "localhost";
                        options.UserName = "guest";
                        options.Password = "guest";
                        //options.ReconnectionAttemptsCount = 5;
                        //options.ReconnectionTimeout = 1000;
                        //options.PrefetchCount = 5;
                    })
                    .BuildServiceProvider();

                var logger = serviceProvider.GetService<ILoggerFactory>()
                    .CreateLogger<Program>();

                // get QueueService
                rmq = serviceProvider.GetRequiredService<IQueueService>();

                //rmq.OnConnectionShutdown += Rmq_OnConnectionShutdown;
                //rmq.OnReconnected += Rmq_OnReconnected;

                _subscriber = rmq.CreateSubscriber(options =>
                {
                    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                    options.ExchangeName = "exchange.1";
                    options.QueueName = "queue.1";
                    options.AutoAck = false;
                });
                _subscriber.Subscribe(opt =>
                {
                    Console.WriteLine("sub called: {0}", opt.ToString());
                    Console.WriteLine("acknowledging: {0}", opt.DeliveryTag);
                    _subscriber.Acknowledge(opt.DeliveryTag);
                });

                var _subscriber2 = rmq.CreateSubscriber(options =>
                {
                    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                    options.ExchangeName = "exchange.2";
                    options.QueueName = "queue.2";
                    options.AutoAck = false;
                });
                _subscriber2.Subscribe(opt =>
                {
                    Console.WriteLine("sub called: {0}", opt.ToString());
                    Console.WriteLine("not acknowledging: {0}", opt.DeliveryTag);
                    //_subscriber.Acknowledge(opt.DeliveryTag);
                });

                Thread thread = new Thread(new ThreadStart(Run));
                thread.Start();

                Thread thread2 = new Thread(new ThreadStart(Run2));
                thread2.Start();

                Console.ReadLine();

                _source.Cancel();

                _subscriber.Unsubscribe();
                _subscriber2.Unsubscribe();
            }
            catch (ReconnectAttemptsExceededException ex)
            {
                Console.WriteLine("ReconnectAttemptsExceededException");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        

        private static void Run2()
        {
            var pub = rmq.CreatePublisher(options =>
            {
                options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                options.ExchangeName = "exchange.2";
                options.ExchangeType = RabbitMqCore.Enums.ExchangeType.direct;
            });

            SimpleObject obj = null;
            RabbitMessageOutbound message = null;

            int count = 1;
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    obj = new SimpleObject() { ID = count++, Name = "Two" };
                    message = new RabbitMessageOutbound()
                    {
                        Message = JsonConvert.SerializeObject(obj)
                    };
                    pub.SendMessage(message);
                    Console.WriteLine("pub called: {0}", message.Message);
                    Thread.Sleep(2000);
                }
                catch (OutboundMessageFailedException ex)
                {
                    Console.WriteLine("OutboundMessageFailedException Message failed {0}:{1}", obj.ID, ex.RabbitMessageOutbound.Message);
                    Thread.Sleep(2000);
                }
                catch (NotConnectedException ex)
                {
                    Console.WriteLine("NotConnectedException Message failed {0}", obj.ID);
                    Thread.Sleep(2000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Message failed {0}", obj.ID);
                    Thread.Sleep(2000);
                }
            }

            Console.WriteLine("pub count: {0}", count - 1);

            Console.WriteLine("token cancelled");
        }
        #endregion

        #region Queue Limit
        static void MainWithQueue(string[] args)
        {
            try
            {
                //setup our DI
                var serviceProvider = new ServiceCollection()
                    .AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                        loggingBuilder.AddLog4Net();
                    })
                    .AddRabbitMQCore(options =>
                    {
                        options.HostName = "localhost";
                        options.UserName = "guest";
                        options.Password = "guest";
                        //options.ReconnectionAttemptsCount = 5;
                        //options.ReconnectionTimeout = 1000;
                    })
                    .BuildServiceProvider();

                var logger = serviceProvider.GetService<ILoggerFactory>()
                    .CreateLogger<Program>();

                // get QueueService
                rmq = serviceProvider.GetRequiredService<IQueueService>();

                rmq.OnConnectionShutdown += Rmq_OnConnectionShutdown;
                //rmq.OnReconnected += Rmq_OnReconnected;

                _queue = new Queue<string>();

                var argsEx = new Dictionary<string, string>();
                argsEx.Add("x-max-length", "int:50");
                _subscriber = rmq.CreateSubscriber(options =>
                {
                    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                    options.ExchangeName = "exchange.1";
                    options.QueueName = "queue.1";
                    //options.ArgumentsEx = argsEx;
                });
                _subscriber.Subscribe(opt =>
                {
                    //_subscriber.Suspend();
                    //Console.WriteLine("sub called: {0}", opt.ToString());
                    _queue.Enqueue(opt.Message);


                    if (_queue.Count > 10)
                    {
                        //_subscriber.Suspend();
                    }
                });

                Thread thread = new Thread(new ThreadStart(Run));
                thread.Start();

                Thread thread2 = new Thread(new ThreadStart(RunQueueProcessor));
                thread2.Start();

                Console.ReadLine();

                _source.Cancel();

                _subscriber.Unsubscribe();

                while (true)
                {
                    try
                    {
                        string message = _queue.Dequeue();
                        if (message != null)
                        {
                            Console.WriteLine("queued: {0}", message.ToString());
                        }
                        else
                        {
                            //Console.WriteLine("processed: null");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        break;
                    }
                }
            }
            catch (ReconnectAttemptsExceededException ex)
            {
                Console.WriteLine("ReconnectAttemptsExceededException");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void RunQueueProcessor()
        {
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    //Console.WriteLine("queue count: {0}", _queue.Count);
                    string message = _queue.Dequeue();
                    if (message != null)
                    {   
                        Console.WriteLine("processed: {0}", message.ToString());
                    }
                    else
                    {
                        //Console.WriteLine("processed: null");
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("processed: ex null");
                }

                if (_queue.Count < 2)
                {
                    //_subscriber.Resume();
                }


                Thread.Sleep(3000);
                
                //_subscriber.Resume();
            }
        }

        //private static void Run()
        //{
        //    var pub = rmq.CreatePublisher(options =>
        //    {
        //        options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
        //        options.ExchangeName = "exchange.1";
        //        options.ExchangeType = RabbitMqCore.Enums.ExchangeType.direct;
        //    });
        //    var obj = new SimpleObject() { ID = 1, Name = "One" };
        //    var message = new RabbitMessageOutbound()
        //    {
        //        Message = JsonConvert.SerializeObject(obj)
        //    };

        //    int count = 1;
        //    while (!_token.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            obj = new SimpleObject() { ID = count++, Name = "One" };
        //            message = new RabbitMessageOutbound()
        //            {
        //                Message = JsonConvert.SerializeObject(obj)
        //            };
        //            pub.SendMessage(message);
        //            //Console.WriteLine("pub called: {0}", message.Message);
        //            Thread.Sleep(1000);
        //        }
        //        catch (OutboundMessageFailedException ex)
        //        {
        //            Console.WriteLine("OutboundMessageFailedException Message failed {0}:{1}", obj.ID, ex.RabbitMessageOutbound.Message);
        //            Thread.Sleep(2000);
        //        }
        //        catch (NotConnectedException ex)
        //        {
        //            Console.WriteLine("NotConnectedException Message failed {0}", obj.ID);
        //            Thread.Sleep(2000);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("Message failed {0}", obj.ID);
        //            Thread.Sleep(2000);
        //        }
        //    }

        //    Console.WriteLine("pub count: {0}", count-1);

        //    Console.WriteLine("token cancelled");
        //}
        #endregion

        

        static void MainOld(string[] args)
        {
            //setup our DI
            var serviceProvider = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddLog4Net();
                })
                .AddRabbitMQCore(options =>
                {
                    options.HostName = "localhost";
                    options.UserName = "guest";
                    options.Password = "guest";
                    //options.ReconnectionAttemptsCount = 2;
                    //options.ReconnectionTimeout = 1000;

                })
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();

            // get QueueService
            rmq = serviceProvider.GetRequiredService<IQueueService>();

            rmq.OnConnectionShutdown += Rmq_OnConnectionShutdown;
            //rmq.OnReconnected += Rmq_OnReconnected;

            // publisher examples

            // publish on exchange
            //var pub1 = rmq.CreatePublisher(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
            //    options.ExchangeName = "exchange.1";
            //    options.ExchangeType = RabbitMqCore.Enums.ExchangeType.direct;
            //});
            //var obj = new SimpleObject() { ID = 1, Name = "One" };
            //var message = new RabbitMessageOutbound()
            //{
            //    Message = JsonConvert.SerializeObject(obj)
            //};
            //pub1.SendMessage(message);

            // publish on exchange with routing key
            //var pub2 = rmq.CreatePublisher(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
            //    options.ExchangeName = "exchange.1";
            //    options.ExchangeType = RabbitMqCore.Enums.ExchangeType.direct;
            //    options.RoutingKeys.Add("routing.key");
            //});
            //var obj2 = new SimpleObject() { ID = 2, Name = "Two" };
            //var message2 = new RabbitMessageOutbound()
            //{
            //    Message = JsonConvert.SerializeObject(obj2)
            //};
            //pub2.SendMessage(message2);

            // publish on queue
            //var pub3 = rmq.CreatePublisher(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Queue;
            //    options.QueueName = "queue.3";
            //    options.Arguments.Add(ArgumentStrings.XMessageTTL, 8000);
            //});
            //pub3.SendMessage(message);



            // subscriber examples

            // subscriber with exchange queue
            var a = new Dictionary<string, string>();
            a.Add("x-max-length", "int:50");
            var sub1 = rmq.CreateSubscriber(options =>
            {
                options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                options.ExchangeName = "exchange.1";
                options.QueueName = "queue.1";
                //options.ArgumentsEx = a;
            });
            sub1.Subscribe(opt =>
            {
                Console.WriteLine("sub 1 called: {0}", opt.ToString());
            });

            // subscriber with exchange queue and routing key
            //var sub1 = rmq.CreateSubscriber(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
            //    options.ExchangeName = "exchange.1";
            //    options.QueueName = "queue.1";
            //    options.RoutingKeys.Add("routing.key.1");
            //});
            //sub1.Subscribe(opt => { Console.WriteLine("sub 1 called: {0}", opt.ToString()); });

            // subscriber with exchange queue and routing key
            //var sub2 = rmq.CreateSubscriber(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
            //    options.ExchangeName = "exchange.1";
            //    options.QueueName = "queue.2";
            //    options.RoutingKeys.Add("routing.key.2");
            //});
            //sub2.Subscribe(opt => { Console.WriteLine("sub 2 called: {0}", opt.ToString()); });

            // subscribe with queue only

            //var a = new Dictionary<string, string>();
            //a.Add("x-max-length", "int:50");
            //var sub3 = rmq.CreateSubscriber(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Queue;
            //    options.QueueName = "eventsqueue";
            //    options.ArgumentsEx = a;
            //});
            //sub3.Subscribe(opt => { Console.WriteLine("sub 3 message:{0}", opt.Message); });

            // subscribe with exchange only
            //var sub4 = rmq.CreateSubscriber(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
            //    options.ExchangeName = "exchange.1";
            //});
            //sub4.Subscribe(opt =>
            //{
            //    Console.WriteLine("sub 4 called: {0}", opt.ToString());
            //    //System.Threading.Thread.Sleep(1000);
            //});

            Thread thread = new Thread(new ThreadStart(Run));
            thread.Start();

            // subscribe with exchange and routing key
            //var sub5 = rmq.CreateSubscriber(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
            //    options.ExchangeName = "exchange.1";
            //    options.RoutingKeys.Add("routing.key.2");
            //});
            //sub5.Subscribe(opt => { Console.WriteLine("sub 5 called: {0}", opt.ToString()); });

            // subscriber with exchange and queue with ttl
            //var sub6 = rmq.CreateSubscriber(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
            //    options.ExchangeName = "exchange.1";
            //    options.QueueName = "queue.4";
            //    options.Arguments.Add(ArgumentStrings.XMessageTTL, 5000);
            //});
            //sub6.Subscribe(opt => { Console.WriteLine("sub 6 called: {0}", opt.ToString()); });

            Console.ReadLine();

            //sub1.Unsubscribe();
            //Console.ReadLine();
            //sub2.Unsubscribe();
            //Console.ReadLine();

            rmq.Dispose();
        }

        

        //private static void Rmq_OnReconnected()
        //{
        //    Console.WriteLine("reconnected");
        //    //throw new NotImplementedException();

        //    var sub4 = rmq.CreateSubscriber(options =>
        //    {
        //        options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
        //        options.ExchangeName = "exchange.1";
        //    });
        //    sub4.Subscribe(opt => { Console.WriteLine("sub 4 called: {0}", opt.ToString()); });
        //}

        //private static void Rmq_OnConnectionShutdown()
        //{
        //    Console.WriteLine("disconnected");
        //    //throw new NotImplementedException();
        //}
    }

    public class SimpleObject
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
