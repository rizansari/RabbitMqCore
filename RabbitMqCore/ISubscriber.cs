﻿using RabbitMqCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore
{
    public interface ISubscriber
    {
        void Subscribe(Action<RabbitMessageEventArgs> action);
        void Unsubscribe();
    }
}
