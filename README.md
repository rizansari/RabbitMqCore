# RabbitMQ Client library for .NET Core

Library Version: v2.0.0

## Installation

```powershell
Install-Package RabbitMQ.NET
```

## Usage

RabbitMQ.NET is a simple library to Publish and Subscribe easily in .NET Core applications.

### Setup DI
```
var serviceProvider = new ServiceCollection()
        .AddLogging(loggingBuilder =>
        {
            loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        })
        .AddRabbitMQCore(options =>
        {
            options.HostName = "localhost";
        })
        .BuildServiceProvider();
```

### Get QueueService
```
var rmq = serviceProvider.GetRequiredService<IQueueService>();
```

### Publisher Examples

#### Publish on Exchange

```
var pub1 = rmq.CreatePublisher(options =>
{
    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
    options.ExchangeName = "exchange.1";
    options.ExchangeType = RabbitMqCore.Enums.ExchangeType.direct;
});
var obj = new SimpleObject() { ID = 1, Name = "One" };
var message = new RabbitMessageOutbound()
{
    Message = JsonConvert.SerializeObject(obj)
};
pub1.SendMessage(message);
```

#### Publish on Exchange with Routing Key

```
var pub2 = rmq.CreatePublisher(options =>
{
    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
    options.ExchangeName = "exchange.1";
    options.ExchangeType = RabbitMqCore.Enums.ExchangeType.direct;
    options.RoutingKeys.Add("routing.key");
});
var obj2 = new SimpleObject() { ID = 2, Name = "Two" };
var message2 = new RabbitMessageOutbound()
{
    Message = JsonConvert.SerializeObject(obj2)
};
pub2.SendMessage(message2);
```

#### Publish on Queue
```
var pub3 = rmq.CreatePublisher(options =>
{
    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Queue;
    options.QueueName = "queue.3";
});
pub3.SendMessage(message);
```

### Subscriber Examples
#### Subscribe with Exchange, Queue and with Routing Key
```
var sub1 = rmq.CreateSubscriber(options =>
{
    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
    options.ExchangeName = "exchange.1";
    options.QueueName = "queue.1";
    options.RoutingKeys.Add("routing.key.1");
});
sub1.Subscribe(opt => { Console.WriteLine("sub 1 called: {0}", opt.ToString()); });
```
#### Subscribe with Queue
```
var sub3 = rmq.CreateSubscriber(options =>
{
    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Queue;
    options.QueueName = "queue.3";
});
sub3.Subscribe(opt => { Console.WriteLine("sub 3 message:{0}", opt.Message); });
```

#### Subscribe with Exchange. Temporary queue will be created automatically
```
var sub4 = rmq.CreateSubscriber(options =>
{
    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
    options.ExchangeName = "exchange.1";
});
sub4.Subscribe(opt => { Console.WriteLine("sub 4 called: {0}", opt.ToString()); });
```

#### Subscribe to Exchange with Routing key
```
var sub5 = rmq.CreateSubscriber(options =>
{
    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
    options.ExchangeName = "exchange.1";
    options.RoutingKeys.Add("routing.key.2");
});
sub5.Subscribe(opt => { Console.WriteLine("sub 5 called: {0}", opt.ToString()); });
```

#### Subscribe to Exchange and Queue with TTL
```
var sub6 = rmq.CreateSubscriber(options =>
{
    options.ExchangeOrQueue = RabbitMqCore.Enums.ExchangeOrQueue.Exchange;
    options.ExchangeName = "exchange.1";
    options.QueueName = "queue.4";
    options.Arguments.Add(ArgumentStrings.XMessageTTL, 5000);
});
sub6.Subscribe(opt => { Console.WriteLine("sub 6 called: {0}", opt.ToString()); });
```

## License

This library licensed under the MIT license.
