using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frameworks.Aspnetcore.Library.Extensions
{
    public static class ResponseCompressionServiceExtension
    {
        public static void AddGzipResponseCompression(this IServiceCollection services, CompressionLevel compressionLevel)
        {
            services.Configure<GzipCompressionProviderOptions>((option) => option.Level = compressionLevel);
            services.AddResponseCompression((option) =>
            {
                option.Providers.Add<GzipCompressionProvider>();
            });
        }
    }
}