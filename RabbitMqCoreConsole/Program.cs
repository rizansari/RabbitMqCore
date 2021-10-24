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
            var pub = rmq.CreatePublisher(options =>
            {
                options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                options.ExchangeName = "exchange.fanout";
                options.ExchangeType = RabbitMqCore.Enums.ExchangeType.fanout;
            });

            var obj = new SimpleObject() { ID = 1, Name = "One" };
            pub.SendMessage(obj);

            var pub2 = rmq.CreatePublisher(options =>
            {
                options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Queue;
                options.QueueName = "queue.test";
            });
            pub2.SendMessage(obj);

            var pub3 = rmq.CreatePublisher(options =>
            {
                options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
                options.ExchangeType = RabbitMqCore.Enums.ExchangeType.direct;
                options.ExchangeName = "exchange.withrouting";
                options.RoutingKeys.Add("routing.key");
            });
            pub3.SendMessage(obj);

            logger.LogDebug("message published");
        }


    }

    public class SimpleObject
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
