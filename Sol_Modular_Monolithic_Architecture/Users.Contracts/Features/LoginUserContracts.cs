using Users.Contracts.Shared.Enums;

namespace Users.Contracts.Features;

public class LoginUserRequestDTO
{
    public string? EmailId { get; set; }

    public string? Password { get; set; }
}

public class UserLoginResponseDTO
{
    public UserResponseDTO User { get; set; }

    public UserJwtTokenResponseDTO JwtToken { get; set; }
}

public class UserResponseDTO
{
    public Guid? Identifier { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public UserType UserType { get; set; }

    [JsonIgnore]
    public string? EmailId { get; set; }

    [JsonIgnore]
    public string? Salt { get; set; }

    [JsonIgnore]
    public string? Hash { get; set; }
}

public class UserJwtTokenResponseDTO
{
    public string? Token { get; set; }

    public string? RefreshToken { get; set; }
}

public class UserLoginCommand : LoginUserRequestDTO, IRequest<DataResponse<UserLoginResponseDTO>>
{
}