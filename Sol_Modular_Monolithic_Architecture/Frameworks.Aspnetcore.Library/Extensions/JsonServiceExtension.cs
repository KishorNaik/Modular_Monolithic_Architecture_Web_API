using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frameworks.Aspnetcore.Library.Extensions
{
    public static class JsonServiceExtension
    {
        public static IMvcBuilder AddCustomJson(this IMvcBuilder mvcBuilder, IHostEnvironment hostEnvironment, bool isPascalCase = false)
        {
            return mvcBuilder.AddJsonOptions((option) =>
            {
                if (isPascalCase)
                {
                    option.JsonSerializerOptions.PropertyNamingPolicy = null;

                    if (hostEnvironment.IsDevelopment())
                    {
                        option.JsonSerializerOptions.WriteIndented = true;
                    }
                }
            });
        }
    }
}