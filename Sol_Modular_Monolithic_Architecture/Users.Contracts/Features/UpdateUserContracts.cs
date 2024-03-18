using Microsoft.AspNetCore.Mvc;
using Users.Contracts.Shared.Enums;

namespace Users.Contracts.Features;

public class UpdateUserRequestBodyDTO
{
    [JsonIgnore]
    public Guid? Identifier { get; set; }

    public string FirstName { get; set; }

    public string? LastName { get; set; }

    public string? EmailId { get; set; }

    public string? MobileNo { get; set; }

    public UserType UserType { get; set; }
}

public class UpdateUserResponseDTO
{
    public Guid? Identifier { get; set; }
}

public class UpdateUserCommand : UpdateUserRequestBodyDTO, IRequest<DataResponse<UpdateUserResponseDTO>>
{
}