using Utility.Shared.Cache;

namespace Organization.Application.Modules.Shared.Cache;

#region Cache Service

public class OrganizationSharedCacheService : INotification
{
    public OrganizationSharedCacheService(Guid? identifier)
    {
        Identifier = identifier;
    }

    public Guid? Identifier { get; }
}

public class OrgnizationSharedCacheServiceHandler : INotificationHandler<OrganizationSharedCacheService>
{
    private readonly IDistributedCache distributedCache = null;
    private readonly IOrganizationSharedRepository organizationSharedRepository;

    public OrgnizationSharedCacheServiceHandler(IDistributedCache distributedCache, IOrganizationSharedRepository organizationSharedRepository, ILogger<OrgnizationSharedCacheServiceHandler> logger)
    {
        this.distributedCache = distributedCache;
        this.organizationSharedRepository = organizationSharedRepository;
    }

    async Task INotificationHandler<OrganizationSharedCacheService>.Handle(OrganizationSharedCacheService notification, CancellationToken cancellationToken)
    {
        try
        {
            string cacheKeyName = $"Org_{notification.Identifier}";

            await this.distributedCache.RemoveAsync(cacheKeyName);

            var org = await this.organizationSharedRepository.GetOrgByIdentifierAsync(notification.Identifier);

            if (org.IsFailed)
                throw new NotFoundException("GetOrgByIdentifierAsync", "Not Found");

            if (org.IsSuccess)
            {
                await SqlCacheHelper.SetCacheAsync(distributedCache, cacheKeyName, ConstantValue.CacheTime, org.Value);
            }
        }
        catch
        {
            throw;
        }
    }
}

#endregion Cache Service