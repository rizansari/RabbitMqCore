using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Events
{
    public class RabbitMessageEventArgs : EventArgs
    {
        public string Message { get; internal set; }
        public string ConsumerTag { get; internal set; }
        public ulong DeliveryTag { get; internal set; }
        public string Exchange { get; internal set; }
        public bool Redelivered { get; internal set; }
        public string RoutingKey { get; internal set; }
        public string CorrelationId { get; internal set; }
        
        //public IBasicProperties BasicProperties { get; internal set; }
        //public byte[] Bytes { get; internal set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
