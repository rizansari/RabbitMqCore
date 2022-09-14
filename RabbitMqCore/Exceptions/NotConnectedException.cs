using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Exceptions
{
    public class NotConnectedException : Exception
    {
        public NotConnectedException(string message) : base(message)
        {

        }

        public NotConnectedException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}
