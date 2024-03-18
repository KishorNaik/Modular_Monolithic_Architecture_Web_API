using Users.Contracts.Shared.Enums;

namespace Users.Contracts.Shared.Services;

public interface IUserProviderService
{
    Guid GetUserIdentifier();

    UserType GetUserRole();
}