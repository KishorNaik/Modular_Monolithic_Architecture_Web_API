using Coravel;
using Coravel.Queuing.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frameworks.Aspnetcore.Library.Extensions;

public static class CoravelBackgroundJobExtension
{
    public static void AddCustomCoravel(this IServiceCollection services)
    {
        services.AddQueue();

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        ILogger<IQueue> logger = serviceProvider.GetRequiredService<ILogger<IQueue>>();

        serviceProvider
            .ConfigureQueue()
            .LogQueuedTaskProgress(logger)
            .OnError(e =>
            {
                logger.LogCritical(nameof(CoravelBackgroundJobExtension), $"Error Message : {e.Message}");
            });
    }
}