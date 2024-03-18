using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Utility.Shared.Cache;

public static class SqlCacheHelper
{
    public static Task SetCacheAsync<TValue>(IDistributedCache distributedCache, string cacheKey, double? cacheTime, TValue value)
    {
        if (distributedCache == null)
            throw new ArgumentNullException(nameof(distributedCache));

        if (cacheKey == null)
            throw new ArgumentNullException(nameof(cacheKey));

        if (cacheTime == null)
            throw new ArgumentNullException(nameof(cacheTime));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var cacheOptions = new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(Convert.ToDouble(cacheTime))
        };

        var jsonData = JsonConvert.SerializeObject(value);
        return distributedCache.SetStringAsync(cacheKey, jsonData, cacheOptions);
    }

    public static Task<string?> GetCacheAsync(IDistributedCache distributedCache, string cacheKey)
    {
        if (distributedCache == null)
            throw new ArgumentNullException(nameof(distributedCache));

        if (cacheKey == null)
            throw new ArgumentNullException(nameof(cacheKey));

        return distributedCache.GetStringAsync(cacheKey);
    }

    public static Task RemoveCacheAsync(IDistributedCache distributedCache, string cacheKey)
    {
        if (distributedCache == null)
            throw new ArgumentNullException(nameof(distributedCache));

        if (cacheKey == null)
            throw new ArgumentNullException(nameof(cacheKey));

        return distributedCache.RemoveAsync(cacheKey);
    }
}