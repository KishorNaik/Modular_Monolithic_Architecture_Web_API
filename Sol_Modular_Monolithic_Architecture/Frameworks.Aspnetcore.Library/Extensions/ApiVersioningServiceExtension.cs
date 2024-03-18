using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Frameworks.Aspnetcore.Library.Extensions;

public static class ApiVersioningServiceExtension
{
    public static void AddCustomApiVersion(this IServiceCollection services)
    {
        services.AddApiVersioning((config) =>
        {
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.DefaultApiVersion = new ApiVersion(1);
            config.ReportApiVersions = true;
            config.ApiVersionReader = ApiVersionReader.Combine(
                new QueryStringApiVersionReader("api-version"),
                new HeaderApiVersionReader("X-Version"),
                new MediaTypeApiVersionReader("ver"));
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'V";
            options.SubstituteApiVersionInUrl = true;
        });
    }
}