using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Events
{
    public class PublisherMessageReturnEventArgs : EventArgs
    {
        public RabbitMessageOutbound Message { get; set; }
        public string Reason { get; set; }

        public PublisherMessageReturnEventArgs(RabbitMessageOutbound Message, string Reason)
        {
            this.Message = Message;
            this.Reason = Reason;
        }
    }
}
