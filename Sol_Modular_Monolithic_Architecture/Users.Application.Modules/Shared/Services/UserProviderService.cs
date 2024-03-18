using Users.Contracts.Shared.Enums;

namespace Users.Application.Modules.Shared.Services;

public class UserProviderService : IUserProviderService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public UserProviderService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    Guid IUserProviderService.GetUserIdentifier()
    {
        return Guid.Parse(httpContextAccessor.HttpContext.User.Claims
                       .First(i => i.Type == ClaimTypes.NameIdentifier).Value);
    }

    UserType IUserProviderService.GetUserRole()
    {
        var userRole = httpContextAccessor.HttpContext.User.Claims.First(i => i.Type == ClaimTypes.Role).Value;

        UserType userType;
        Enum.TryParse(userRole, out userType);

        return userType;
    }
}