using Frameworks.Aspnetcore.Library.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Organization.Application.Modules.Infrastructures;
using Organization.Application.Modules.Shared.Repository;
using System.Configuration;

namespace Organization.Application.Modules;

public static class Program
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services, IHostApplicationBuilder hostApplicationBuilder, IConfiguration configuration)
    {
        services.AddControllers()
                .AddCustomJson(hostApplicationBuilder.Environment, isPascalCase: true)
                .AddFluentValidationException(typeof(Program), services);

        services.AddMediatR((config) =>
        {
            config.RegisterServicesFromAssemblyContaining(typeof(Program));
        });

        // RabbitMQ Config
        //RabbitMQCredentials rabbitMqCredentails = configuration.GetSection("RabbitMQ").Get<RabbitMQCredentials>();
        //services.AddRabbitMQ(new RabbitMQCredentials()
        //{
        //    Url = rabbitMqCredentails.Url,
        //    UserName = rabbitMqCredentails.UserName,
        //    Password = rabbitMqCredentails.Password,
        //}, (config) =>
        //{
        //    config.AddConsumer<GetOrganizationByIdentifierMessagingConsumerHandler>();
        //},
        //(configEndPoint, busFactory) =>
        //{
        //    configEndPoint.ReceiveEndpoint("rr-get-orgnanization-by-identifier", (configReceiveEndPoint) =>
        //    {
        //        configReceiveEndPoint.ConfigureConsumer<GetOrganizationByIdentifierMessagingConsumerHandler>(busFactory);
        //    });
        //});

        //Get Secret Connection String
        string? connectionString = configuration?.GetSecretConnectionString(ConstantValue.DbName);

        services.AddDbContext<OrganizationContext>((config) =>
        {
            config.UseSqlServer(connectionString);
            config.EnableDetailedErrors(true);
            config.EnableSensitiveDataLogging(true);
        });

        services.AddScoped<IOrganizationSharedRepository, OrganizationSharedRepository>();

        return services;
    }
}