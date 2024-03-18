using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frameworks.Aspnetcore.Library.Extensions
{
    public static class RabbitMQExtension
    {
        public static void AddRabbitMQ(this IServiceCollection services,
            RabbitMQCredentials rabbitMqCredentails,
            Action<IBusRegistrationConfigurator>? addConsumers = null,
            Action<IRabbitMqBusFactoryConfigurator, IBusRegistrationContext>? addRabbitMqRecceiveEndpoints = null)
        {
            if (rabbitMqCredentails == null)
                throw new RabbitMQCredentialsNotFoundException();

            _ = services.AddMassTransit((config) =>
            {
                // Add Consumers
                addConsumers?.Invoke(config);

                // Add RabbitMq Host
                config.AddBus((busFactory) => Bus.Factory.CreateUsingRabbitMq((configRabbitMq) =>
                {
                    configRabbitMq.Host(new Uri(uriString: rabbitMqCredentails.Url!), (configHost) =>
                    {
                        configHost.Username(rabbitMqCredentails.UserName);
                        configHost.Password(rabbitMqCredentails.Password);
                    });

                    // Add RabbitMq Endpoint
                    addRabbitMqRecceiveEndpoints?.Invoke(configRabbitMq, busFactory);
                }));
            });
        }
    }

    public class RabbitMQCredentials
    {
        public string? Url { get; set; }
        public string? UserName { get; set; }

        public string? Password { get; set; }
    }

    public class RabbitMQCredentialsNotFoundException : Exception
    {
        public override string Message => "RabbitMq Credentials is not found";
    }
}