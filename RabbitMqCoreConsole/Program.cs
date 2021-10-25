using Microsoft.Extensions.DependencyInjection;
using System;
using RabbitMqCore.DependencyInjectionExtensions;
using RabbitMqCore;
using Microsoft.Extensions.Logging;
using Log4NetCore;

namespace RabbitMqCoreConsole
{
    class Program
    {
        static void Main(string[] args)
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
                })
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();

            logger.LogInformation("hello world");

            var rmq = serviceProvider.GetRequiredService<IQueueService>();


            //var pub = rmq.CreatePublisher(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
            //    options.ExchangeName = "exchange.fanout";
            //    options.ExchangeType = RabbitMqCore.Enums.ExchangeType.fanout;
            //});

            //var obj = new SimpleObject() { ID = 1, Name = "One" };
            //pub.SendMessage(obj);

            //var pub2 = rmq.CreatePublisher(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Queue;
            //    options.QueueName = "queue.test";
            //});
            //pub2.SendMessage(obj);

            //var pub3 = rmq.CreatePublisher(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
            //    options.ExchangeType = RabbitMqCore.Enums.ExchangeType.direct;
            //    options.ExchangeName = "exchange.withrouting";
            //    options.RoutingKeys.Add("routing.key");
            //});
            //pub3.SendMessage(obj);

            //logger.LogDebug("message published");


            // subscriber with exchange queue and routing key
            var sub1 = rmq.CreateSubscriber(options =>
            {
                options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                options.ExchangeName = "exchange.1";
                options.QueueName = "queue.1";
                options.RoutingKeys.Add("routing.key.1");
            });
            sub1.Subscribe(opt => { Console.WriteLine("sub 1 called: {0}", opt.ToString()); });

            // subscriber with exchange queue and routing key
            var sub2 = rmq.CreateSubscriber(options =>
            {
                options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                options.ExchangeName = "exchange.1";
                options.QueueName = "queue.2";
                options.RoutingKeys.Add("routing.key.2");
            });
            sub2.Subscribe(opt => { Console.WriteLine("sub 2 called: {0}", opt.ToString()); });

            // subscribe with queue only
            //var sub3 = rmq.CreateSubscriber(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Queue;
            //    options.QueueName = "queue.3";
            //});
            //sub3.Subscribe(opt => { Console.WriteLine("sub 3 message:{0}", opt.Message); });

            // subscribe with exchange only
            //var sub4 = rmq.CreateSubscriber(options =>
            //{
            //    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
            //    options.ExchangeName = "exchange.1";
            //});
            //sub4.Subscribe(opt => { Console.WriteLine("sub 4 called: {0}", opt.ToString()); });

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
            //    options.Arguments.Add("x-message-ttl", 5000);
            //});
            //sub6.Subscribe(opt => { Console.WriteLine("sub 6 called: {0}", opt.ToString()); });

            Console.ReadLine();

            //sub1.Unsubscribe();
            //Console.ReadLine();
            //sub2.Unsubscribe();
            //Console.ReadLine();

            rmq.Dispose();
        }


    }

    public class SimpleObject
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
