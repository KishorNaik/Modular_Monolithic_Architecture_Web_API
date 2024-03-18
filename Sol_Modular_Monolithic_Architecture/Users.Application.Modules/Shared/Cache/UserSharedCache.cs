namespace Users.Application.Modules.Shared.Cache;

public class UserSharedCacheService : INotification
{
    public UserSharedCacheService(Guid? identifier)
    {
        this.Identifier = identifier;
    }

    public Guid? Identifier { get; }
}

public class UserSharedCacheServiceHandler : INotificationHandler<UserSharedCacheService>
{
    private readonly IDistributedCache distributedCache = null;
    private readonly IUserSharedRepository userSharedRepository = null;

    public UserSharedCacheServiceHandler(IDistributedCache distributedCache, IUserSharedRepository userSharedRepository, ILogger<UserSharedCacheServiceHandler> logger)
    {
        this.distributedCache = distributedCache;
        this.userSharedRepository = userSharedRepository;
    }

    async Task INotificationHandler<UserSharedCacheService>.Handle(UserSharedCacheService notification, CancellationToken cancellationToken)
    {
        try
        {
            string cacheKeyName = $"User_{notification.Identifier}";

            await SqlCacheHelper.RemoveCacheAsync(distributedCache, cacheKeyName);

            var userResult = await this.userSharedRepository.GetUserByIdentifierAsync(notification.Identifier);

            if (userResult.IsFailed)
                throw new NotFoundException("GetUserByIdentifierAsync", "Not Found");

            await SqlCacheHelper.SetCacheAsync(distributedCache, cacheKeyName, ConstantValue.CacheTime, userResult.Value);
        }
        catch
        {
            throw;
        }
    }
}