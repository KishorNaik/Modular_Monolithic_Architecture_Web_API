using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frameworks.Aspnetcore.Library.Extensions;

public static class HangfireBackgroundJobExtension
{
    public static void AddHangFireBackgroundJob(this IServiceCollection services, IConfiguration configuration, string? name)
    {
        string? connectionString = configuration?.GetSecretConnectionString(name);
        Console.WriteLine($"ConnetionString : {connectionString}");

        services.AddHangfire(x => x.UseSqlServerStorage(connectionString).UseRecommendedSerializerSettings().UseSimpleAssemblyNameTypeSerializer());
        services.AddHangfireServer();
    }
}