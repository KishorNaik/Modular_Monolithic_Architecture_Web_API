using Models.Shared.Response;
using Users.Contracts.Shared.Enums;

namespace Users.Contracts.Features;

public class AddUserRequestDTO
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? EmailId { get; set; }

    public string? MobileNo { get; set; }

    public UserType UserType { get; set; }

    public string? Password { get; set; }

    public Guid? OrgId { get; set; }
}

public class AddUserResponseDTO
{
    public Guid? Identifier { get; set; }

#if DEBUG
    public Guid? EmailToken { get; set; } = null;

#endif
}