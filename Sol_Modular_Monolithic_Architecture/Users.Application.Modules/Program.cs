using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Users.Application.Modules.Shared.Policy;
using Users.Application.Modules.Shared.Services;
using Users.Contracts.Shared.Services;

namespace Users.Application.Modules;

public static class Program
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IHostApplicationBuilder hostApplicationBuilder, IConfiguration configuration)
    {
        services.AddControllers()
                .AddCustomJson(hostApplicationBuilder.Environment, isPascalCase: true)
                .AddFluentValidationException(typeof(Program), services);

        // Get JWT Configuration
        //JwtAppSetting jwtAppSetting = configuration.GetSection("JWT").Get<JwtAppSetting>();
        //services.AddJwtToken(jwtAppSetting);

        services.AddMediatR((config) =>
        {
            config.RegisterServicesFromAssemblyContaining(typeof(Program));
        });

        //Get Secret Connection String
        string? connectionString = configuration?.GetSecretConnectionString(ConstantValue.DbName);

        services.AddDbContext<UsersContext>((config) =>
        {
            config.UseSqlServer(connectionString);
            config.EnableDetailedErrors(true);
            config.EnableSensitiveDataLogging(true);
        });

        services.AddScoped<IUserProviderService, UserProviderService>();
        services.AddScoped<IUserSharedRepository, UserSharedRepository>();
        services.AddScoped<IGenerateHashPasswordService, GenerateHashPasswordService>();

        //Register Auth Policy Handler
        services.AddSingleton<IAuthorizationHandler, BuyerOnlyAuthRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, SellerOnlyAuthRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, BuyerSellerOnlyAuthRequirementHandler>();

        return services;
    }
}