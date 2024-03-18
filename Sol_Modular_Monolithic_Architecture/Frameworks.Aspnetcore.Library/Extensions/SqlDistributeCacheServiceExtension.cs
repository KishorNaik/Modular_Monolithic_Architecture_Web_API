using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frameworks.Aspnetcore.Library.Extensions;

public static class SqlDistributeCacheServiceExtension
{
    public static void AddCustomSqlDistributedCache(this IServiceCollection services, IConfiguration configuration, string? dbSectionName, string? schemaName, string? tableName)
    {
        services.AddDistributedSqlServerCache(options =>
        {
            options.ConnectionString = configuration.GetSecretConnectionString(dbSection: dbSectionName);
            options.SchemaName = schemaName ?? "dbo";
            options.TableName = tableName;
        });
    }
}