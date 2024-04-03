using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using System.Net;

namespace Frameworks.Aspnetcore.Library.Extensions;

public class RedisEndPoints
{
    public string? Host { get; set; }

    public int Port { get; set; }
}

public class RedisConfig
{
    public List<RedisEndPoints>? EndPoints { get; set; }

    public string? Password { get; set; }

    public int DefaultDatabase { get; set; }
}

public static class RedisDistributeCacheServiceExtension
{
    public static void AddDistributedRedisCache(this IServiceCollection services, IHostEnvironment hostEnvironment, string? instanceName, RedisConfig redisConfig = default)
    {
        services.AddStackExchangeRedisCache((options) =>
        {
            options.InstanceName = instanceName;

            if (hostEnvironment.IsDevelopment())
            {
                options.Configuration = "localhost:6379";
            }
            else if (hostEnvironment.IsProduction())
            {
                EndPointCollection endPoints = new EndPointCollection();

                foreach (var endPoint in redisConfig.EndPoints)
                {
                    endPoints.Add(endPoint.Host, endPoint.Port);
                }

                options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
                {
                    Password = redisConfig.Password,
                    EndPoints = endPoints,
                    DefaultDatabase = redisConfig.DefaultDatabase
                };
            }
        });
    }
}