using RabbitMqCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public interface IRpcClient : IDisposable
    {
        void Call(RabbitMessageOutbound @object, Action<RabbitMessageInbound> action, int Delay = 5000);
    }
}
