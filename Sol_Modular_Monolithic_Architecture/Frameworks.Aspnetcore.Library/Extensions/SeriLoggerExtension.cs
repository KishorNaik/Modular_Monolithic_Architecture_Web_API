using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace Frameworks.Aspnetcore.Library.Extensions
{
    public static class SeriLoggerExtension
    {
        public static void AddSeriLogger(this WebApplicationBuilder webApplicationBuilder, string? dbName)
        {
            // Get Connection String from Secret Conection String Extension.
            var connectionString = webApplicationBuilder.Configuration.GetSecretConnectionString(dbName);

            // Add Logger Setting
            var logger = new LoggerConfiguration()
                            .MinimumLevel.Information()
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Error)
                            .MinimumLevel.Override("Serilog", LogEventLevel.Error)
                            .Enrich.FromLogContext()
                            .Enrich.WithClientIp()
                            .WriteTo.Console()
                            .WriteTo.MSSqlServer(connectionString: connectionString, tableName: "Logs", autoCreateSqlTable: true)
                            .CreateLogger();

            Log.Logger = logger;
            webApplicationBuilder.Logging.AddSerilog(logger);

            webApplicationBuilder.Host.UseSerilog();
        }
    }
}