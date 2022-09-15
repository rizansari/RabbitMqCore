using RabbitMqCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public interface IRpcServer : IDisposable
    {
        void Subscribe(Action<RabbitMessageInbound> action);
        void Respond(RabbitMessageOutbound @object);
    }
}
