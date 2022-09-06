using RabbitMqCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public interface ISubscriber
    {
        void Subscribe(Action<RabbitMessageInbound> action);
        void Unsubscribe();
        void Resume();
        void Suspend();
    }
}
