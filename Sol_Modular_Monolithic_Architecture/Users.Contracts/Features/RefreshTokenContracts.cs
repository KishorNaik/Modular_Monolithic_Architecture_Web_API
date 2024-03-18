namespace Users.Contracts.Features
{
    public class RefreshTokenRequestDTO
    {
        public string? AccessToken { get; set; }

        public string? RefreshToken { get; set; }
    }

    public class RefreshTokenCommand : RefreshTokenRequestDTO, IRequest<DataResponse<UserJwtTokenResponseDTO>>
    {
    }
}