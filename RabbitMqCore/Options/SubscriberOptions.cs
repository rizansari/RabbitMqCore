using RabbitMqCore.Common;
using RabbitMqCore.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Options
{
    public class SubscriberOptions
    {
        /// <summary>
        /// 
        /// </summary>
        public ExchangeOrQueue ExchangeOrQueue { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ExchangeName { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public string QueueName { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public ExchangeType ExchangeType { get; set; } = ExchangeType.fanout;
        /// <summary>
        /// 
        /// </summary>
        public bool Durable { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public bool Exclusive { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public bool AutoDelete { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public HashSet<string> RoutingKeys { get; set; } = new HashSet<string>();
        /// <summary>
        /// 
        /// </summary>
        public bool AutoAck { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public bool RequeueNack { get; set; } = false;
        /// <summary>
        /// if set to true then exchange or queue will be deleted and created again if PRECONDITION fail
        /// </summary>
        public bool RecreateIfFailed { get; set; } = false;
        /// <summary>
        /// If yes, clients cannot publish to this exchange directly. It can only be used with exchange to exchange bindings.
        /// </summary>
        public bool Internal { get; set; } = false;
        /// <summary>
        /// If messages to this exchange cannot otherwise be routed, send them to the alternate exchange named here.
        /// </summary>
        public string AlternateExchange { get; set; }
        /// <summary>
        /// How long a message published to a queue can live before it is discarded (milliseconds).
        /// </summary>
        public long MessageTTL { get; set; } = -1;
        /// <summary>
        /// How long a queue can be unused for before it is automatically deleted (milliseconds).
        /// </summary>
        public long AutoExpires { get; set; } = -1;
        /// <summary>
        /// How many (ready) messages a queue can contain before it starts to drop them from its head.
        /// </summary>
        public long MaxLength { get; set; } = -1;
        /// <summary>
        /// Total body size for ready messages a queue can contain before it starts to drop them from its head.
        /// </summary>
        public long MaxLengthBytes { get; set; } = -1;
        /// <summary>
        /// Sets the queue overflow behaviour. This determines what happens to messages when the maximum length of a queue is reached. Valid values are drop-head or reject-publish.
        /// </summary>
        public OverflowBehavior OverflowBehavior { get; set; } = OverflowBehavior.None;
        /// <summary>
        /// Optional name of an exchange to which messages will be republished if they are rejected or expire.
        /// </summary>
        public string DeadLetterExchange { get; set; } = "";
        /// <summary>
        /// Optional replacement routing key to use when a message is dead-lettered. If this is not set, the message's original routing key will be used.
        /// </summary>
        public string DeadLetterRoutingKey { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public uint PrefetchSize { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public ushort PrefetchCount { get; set; } = 0;
    }
}
