using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

//https://tohidhaghighi.medium.com/rate-limiting-middleware-in-asp-net-core-a86b98cb43f5#:~:text=Using%20Rate%20Limiting%20In%20Your%20API&text=EnableRateLimiting%20can%20be%20applied%20on%20the%20controller%20or%20on%20the%20individual%20endpoints.&text=In%20the%20previous%20example%3A,use%20a%20sliding%20window%20policy

namespace Frameworks.Aspnetcore.Library.Extensions;

public record RateLimitOptions(int? PermitLimit, TimeSpan? WindowTime, QueueProcessingOrder? Order, int? QueueLimit, int? SegmentsPerWindow = default(int?));

public enum RateLimitAlgorithmsEnum
{
    FixedWindow = 0,
    SlidingWindow = 1
}

public static class RateLimitServiceExtension
{
    public static void AddCustomRateLimit(this IServiceCollection services, RateLimitAlgorithmsEnum? rateLimitAlgorithmsEnum, string policyName, RateLimitOptions rateLimitOptions)
    {
        if (rateLimitAlgorithmsEnum is null)
            throw new ArgumentNullException(nameof(rateLimitAlgorithmsEnum));

        if (policyName is null || string.IsNullOrEmpty(policyName))
            throw new ArgumentNullException(nameof(policyName));

        if (rateLimitOptions is null)
            throw new ArgumentException(nameof(rateLimitOptions));
        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            if (rateLimitAlgorithmsEnum == RateLimitAlgorithmsEnum.FixedWindow)
            {
                rateLimiterOptions.AddFixedWindowLimiter(policyName, options =>
                {
                    options.PermitLimit = rateLimitOptions.PermitLimit ?? 10;
                    options.Window = rateLimitOptions.WindowTime ?? TimeSpan.FromSeconds(10);
                    options.QueueProcessingOrder = rateLimitOptions.Order ?? QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = rateLimitOptions.QueueLimit ?? 5;
                });
            }
            else if (rateLimitAlgorithmsEnum == RateLimitAlgorithmsEnum.SlidingWindow)
            {
                rateLimiterOptions.AddSlidingWindowLimiter(policyName, options =>
                {
                    options.PermitLimit = rateLimitOptions.PermitLimit ?? 10;
                    options.Window = rateLimitOptions.WindowTime ?? TimeSpan.FromSeconds(10);
                    options.QueueProcessingOrder = rateLimitOptions.Order ?? QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = rateLimitOptions.QueueLimit ?? 5;
                    options.SegmentsPerWindow = rateLimitOptions.SegmentsPerWindow ?? 2;
                });
            }
        });
    }
}