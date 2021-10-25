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
        public ExchangeType ExchangeType { get; set; } = ExchangeType.direct;
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
        public IDictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
        /// <summary>
        /// 
        /// </summary>
        public HashSet<string> RoutingKeys { get; set; } = new HashSet<string>();
        /// <summary>
        /// 
        /// </summary>
        public string ConsumerTag { get; internal set; }
    }
}
