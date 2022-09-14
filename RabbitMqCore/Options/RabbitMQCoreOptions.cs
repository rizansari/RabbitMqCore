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
        /// <summary>
        /// Not being used
        /// </summary>
        public int ReconnectionTimeout { get; set; } = 3000;
        /// <summary>
        /// Not being used
        /// </summary>
        public int ReconnectionAttemptsCount { get; set; } = 20;
        public bool AutomaticRecoveryEnabled { get; set; } = false;
        public bool TopologyRecoveryEnabled { get; set; } = false;
        public bool DispatchConsumersAsync { get; set; } = true;
        public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(5);
        public bool ThrowIfNotConnected { get; set; } = true;
        public bool ConnectOnConstruction { get; set; } = true;
        public string ClientProvidedName { get; set; } = "RabbitMQCore";
        public bool DebugMode { get; set; } = false;
    }
}
