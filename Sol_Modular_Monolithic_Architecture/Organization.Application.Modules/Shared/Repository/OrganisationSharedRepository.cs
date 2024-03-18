using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Organization.Application.Modules.Shared.Repository;

public interface IOrganizationSharedRepository
{
    Task<Result<TOrganization>> GetOrgByIdentifierAsync(Guid? identifier);
}

public class OrganizationSharedRepository : IOrganizationSharedRepository
{
    private readonly OrganizationContext organizationContext;

    public OrganizationSharedRepository(OrganizationContext organizationContext)
    {
        this.organizationContext = organizationContext;
    }

    async Task<Result<TOrganization>> IOrganizationSharedRepository.GetOrgByIdentifierAsync(Guid? identifier)
    {
        try
        {
            var result = this.organizationContext.TOrganizations.AsNoTracking()
                                                                .AsQueryable()
                                                                .AsParallel()
                                                                .AsSequential()
                                                                .FirstOrDefault((element) => element.Identifier.Equals(identifier));
            if (result is null)
                return Result.Fail<TOrganization>(new FluentResults.Error("The orgnisation is not found").WithMetadata("StatusCode", HttpStatusCode.NotFound));

            return Result.Ok<TOrganization>(result);
        }
        catch (Exception ex)
        {
            return Result.Fail<TOrganization>(new FluentResults.Error(ex.Message).WithMetadata("StackTrace", ex.StackTrace).WithMetadata("StatusCode", HttpStatusCode.InternalServerError));
        }
    }
}