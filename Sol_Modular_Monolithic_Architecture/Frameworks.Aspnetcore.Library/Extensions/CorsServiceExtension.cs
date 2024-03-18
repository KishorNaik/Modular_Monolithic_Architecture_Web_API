using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frameworks.Aspnetcore.Library.Extensions
{
    public static class CorsServiceExtension
    {
        public static void AddCustomCors(this IServiceCollection services, string policyName, string[]? orginiList = default(string[]))
        {
            services.AddCors((options) =>
            {
                options.AddPolicy(name: policyName, (policy) =>
                {
                    if (orginiList is null || orginiList?.Length == 0)
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                    else
                    {
                        policy.WithOrigins(orginiList)
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                });
            });
        }
    }
}