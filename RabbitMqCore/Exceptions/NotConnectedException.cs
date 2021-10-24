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
    }
}
