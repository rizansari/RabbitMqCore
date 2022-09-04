using RabbitMqCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Exceptions
{
    public class OutboundMessageFailedException
        : Exception
    {
        public RabbitMessageOutbound RabbitMessageOutbound { get; set; }

        public OutboundMessageFailedException(string message, RabbitMessageOutbound outboundMessage) : base(message)
        {
            RabbitMessageOutbound = outboundMessage;
        }

        public OutboundMessageFailedException(string message, RabbitMessageOutbound outboundMessage, Exception inner) : base(message, inner)
        {
            RabbitMessageOutbound = outboundMessage;
        }
    }
}
