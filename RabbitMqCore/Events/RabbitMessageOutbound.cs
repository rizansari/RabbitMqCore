using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Events
{
    public class RabbitMessageOutbound
    {
        /// <summary>
        /// 
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string CorrelationId { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
