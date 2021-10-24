using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public interface IPublisher
    {
        void SendMessage(object @object);
    }
}
