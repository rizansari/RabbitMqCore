using RabbitMqCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public interface IPublisher : IDisposable
    {
        event EventHandler<PublisherMessageReturnEventArgs> OnMessageReturn;
        event EventHandler<PublisherMessageReturnEventArgs> OnMessageFailed;

        void SendMessage(RabbitMessageOutbound @object);
    }
}
