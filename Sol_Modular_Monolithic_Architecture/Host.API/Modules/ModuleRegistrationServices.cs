using Notifications.Application.Modules;
using Organization.Application.Modules;
using Users.Application.Modules;

namespace Host.API.Modules;

public static class ModuleRegistrationServices
{
    public static void AddModules(this IHostApplicationBuilder hostApplicationBuilder)
    {
        Console.WriteLine($"Env: {hostApplicationBuilder.Environment.EnvironmentName}");

        hostApplicationBuilder.Services.AddOrganizationModule(hostApplicationBuilder, hostApplicationBuilder.Configuration);
        hostApplicationBuilder.Services.AddUsersModule(hostApplicationBuilder, hostApplicationBuilder.Configuration);
        hostApplicationBuilder.Services.AddNotificationModule(hostApplicationBuilder, hostApplicationBuilder.Configuration);
    }
}