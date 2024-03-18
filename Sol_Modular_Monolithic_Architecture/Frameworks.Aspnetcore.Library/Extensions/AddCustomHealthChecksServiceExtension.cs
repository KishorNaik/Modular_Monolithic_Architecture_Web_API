using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frameworks.Aspnetcore.Library.Extensions;

public static class AddCustomHealthChecksServiceExtension
{
    public static void AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // Main Db ConnectionString
        string mainDb = configuration.GetSecretConnectionString(dbSection: "CarbonCredit");
        string sqlCache = configuration.GetSecretConnectionString(dbSection: "SQLCache");
        string seriLogs = configuration.GetSecretConnectionString(dbSection: "SeriLogs");
        string hangFire = configuration.GetSecretConnectionString(dbSection: "HangFireDB");

        services.AddHealthChecks()
                .AddSqlServer(mainDb, name: "MainDB")
                .AddSqlServer(sqlCache, name: "SQLCache")
                .AddSqlServer(seriLogs, name: "SeriLogs")
                .AddSqlServer(hangFire, name: "HangFire");
    }
}