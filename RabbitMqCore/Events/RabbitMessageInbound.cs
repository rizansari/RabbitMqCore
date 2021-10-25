using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Events
{
    public class RabbitMessageInbound
    {
        /// <summary>
        /// 
        /// </summary>
        public string Message { get; internal set; }
        /// <summary>
        /// 
        /// </summary>
        public string ConsumerTag { get; internal set; }
        /// <summary>
        /// 
        /// </summary>
        public ulong DeliveryTag { get; internal set; }
        /// <summary>
        /// 
        /// </summary>
        public string Exchange { get; internal set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Redelivered { get; internal set; }
        /// <summary>
        /// 
        /// </summary>
        public string RoutingKey { get; internal set; }
        /// <summary>
        /// 
        /// </summary>
        public string CorrelationId { get; internal set; }
        
        //public IBasicProperties BasicProperties { get; internal set; }
        //public byte[] Bytes { get; internal set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
