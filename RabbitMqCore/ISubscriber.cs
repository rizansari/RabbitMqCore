using RabbitMqCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public interface ISubscriber : IDisposable
    {
        void Subscribe(Action<RabbitMessageInbound> action);
        void Unsubscribe();
        void Acknowledge(ulong DeliveryTag);
        void NotAcknowledge(ulong DeliveryTag);
    }
}
