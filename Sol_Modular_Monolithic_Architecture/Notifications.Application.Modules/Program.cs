namespace Notifications.Application.Modules;

public static class Program
{
    public static void AddNotificationModule(this IServiceCollection services, IHostApplicationBuilder hostApplicationBuilder, IConfiguration configuration)
    {
        services.AddControllers()
                .AddCustomJson(hostApplicationBuilder.Environment, isPascalCase: true)
                .AddFluentValidationException(typeof(Program), services);

        services.AddMediatR((config) =>
        {
            config.RegisterServicesFromAssemblyContaining(typeof(Program));
        });
    }
}