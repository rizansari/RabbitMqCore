﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Enums
{
    public enum ExchangeType
    {
        direct = 0,
        fanout = 1,
        headers = 2,
        topic = 3
    }
}
