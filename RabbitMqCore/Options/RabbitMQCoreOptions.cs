using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Options
{
    public class RabbitMQCoreOptions
    {
        public string HostName { get; set; } = "127.0.0.1";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public int Port { get; set; } = 5672;
        public int RequestedConnectionTimeout { get; set; } = 30000;
        public ushort RequestedHeartbeat { get; set; } = 60;
        public ushort PrefetchCount { get; set; } = 1;
        public int ReconnectionTimeout { get; set; } = 3000;
        public int ReconnectionAttemptsCount { get; set; } = 20;

    }
}
