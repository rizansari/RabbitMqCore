using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Exceptions
{
    public class ReconnectAttemptsExceededException : Exception
    {
        public ReconnectAttemptsExceededException(string message) : base(message)
        {

        }
    }
}
