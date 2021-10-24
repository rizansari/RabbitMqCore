using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMqCore.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.DependencyInjectionExtensions
{
    public static class RabbitMqCoreExtensions
    {
        public static IServiceCollection AddRabbitMQCore(this IServiceCollection services)
        {
            var temp = new RabbitMQCoreOptions();
            services.AddSingleton<RabbitMQCoreOptions>(temp);
            services.TryAddSingleton<IQueueService, QueueService>();
            return AddRabbitMQCore(services);
        }

        public static IServiceCollection AddRabbitMQCore(this IServiceCollection services, Action<RabbitMQCoreOptions> options)
        {
            var temp = new RabbitMQCoreOptions();
            options(temp);
            services.AddSingleton<RabbitMQCoreOptions>(temp);
            services.TryAddSingleton<IQueueService, QueueService>();
            return services;
        }
    }
}
