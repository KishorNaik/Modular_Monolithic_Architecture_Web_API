using Microsoft.AspNetCore.Mvc;
using Users.Contracts.Shared.Enums;

namespace Users.Contracts.Features;

public class GetUsersByFiltersRequestDTO
{
    public string? MobileNo { get; set; }

    public string? EmailId { get; set; }
}

public class GetUsersByFiltersResponseDTO
{
    public Guid Identifier { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string EmailId { get; set; }

    public string MobileNo { get; set; }

    public UserType UserType { get; set; }
}

public class GetUsersByFiltersQuery : GetUsersByFiltersRequestDTO, IRequest<DataResponse<List<GetUsersByFiltersResponseDTO>>>
{
}