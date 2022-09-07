using RabbitMqCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public interface ISubscriber
    {
        bool IsSuspended { get; set; }
        void Subscribe(Action<RabbitMessageInbound> action);
        void Unsubscribe();
        void Resume();
        void Suspend();
        void Acknowledge(ulong DeliveryTag);
    }
}
