namespace Users.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public class RefreshTokenController : UserBaseController
{
    public RefreshTokenController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("refresh-token")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<UserJwtTokenResponseDTO>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<UserJwtTokenResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand refreshTokenCommand, CancellationToken cancellationToken)
    {
        var response = await this.Mediator.Send(refreshTokenCommand, cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

public class UserRefreshTokenValidation : AbstractValidator<RefreshTokenCommand>
{
    public UserRefreshTokenValidation()
    {
        this.AccessTokenValidation();
        this.RefreshTokenValidation();
    }

    private void AccessTokenValidation()
    {
        this.RuleFor(x => x.AccessToken)
            .NotEmpty()
            .WithErrorCode("AccessToken");
    }

    private void RefreshTokenValidation()
    {
        this.RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithErrorCode("RefreshToken");
    }
}

#endregion Validation Service

#region Exception Service

public static class RefreshTokenExceptionHandler
{
    public static DataResponse<UserJwtTokenResponseDTO> CommandHandlerException(string errorMessage) =>
     DataResponse.Response<UserJwtTokenResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<UserJwtTokenResponseDTO> Argument_Null_Exception_Command_Handler =>
        DataResponse.Response<UserJwtTokenResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<UserJwtTokenResponseDTO> User_Identifier_Not_Found =>
        DataResponse.Response<UserJwtTokenResponseDTO>(false, (int?)HttpStatusCode.Unauthorized, null, "UnAuthorized Access");

    public static DataResponse<UserJwtTokenResponseDTO> User_Not_Found =>
       DataResponse.Response<UserJwtTokenResponseDTO>(false, (int?)HttpStatusCode.Unauthorized, null, "UnAuthorized Access");

    public static DataResponse<UserJwtTokenResponseDTO> InValid_Client_Request =>
       DataResponse.Response<UserJwtTokenResponseDTO>(false, (int?)HttpStatusCode.Unauthorized, null, "UnAuthorized Access");

    public static DataResponse<UserJwtTokenResponseDTO> Token_Generation_Failed =>
       DataResponse.Response<UserJwtTokenResponseDTO>(false, (int?)HttpStatusCode.Unauthorized, null, "UnAuthorized Access");
}

#endregion Exception Service

#region Command Service

public class RefreshTokenCommand : RefreshTokenRequestDTO, IRequest<DataResponse<UserJwtTokenResponseDTO>>
{
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, DataResponse<UserJwtTokenResponseDTO>>
{
    private readonly IJwtTokenService jwtTokenService;

    private readonly IUserSharedRepository userSharedRepository;

    private readonly IOptions<JwtAppSetting> options;

    public RefreshTokenCommandHandler(IJwtTokenService jwtTokenService, IUserSharedRepository userSharedRepository, IOptions<JwtAppSetting> options)
    {
        this.jwtTokenService = jwtTokenService;
        this.userSharedRepository = userSharedRepository;
        this.options = options;
    }

    private async Task<(string? AccessToken, string? RefreshToken)> GenerateNewAccessTokenAndRefreshTokenAsync(JwtAppSetting jwtAppSetting, Claim[] claims, DateTime? expires)
    {
        var newAccessTokenTask = jwtTokenService.GenerateJwtTokenAsync(this.options.Value, claims, expires);
        var newRefreshTokenTask = jwtTokenService.GenerateRefreshTokenAsync();

        await Task.WhenAll(newAccessTokenTask, newRefreshTokenTask);

        return (newAccessTokenTask: newAccessTokenTask.Result, newRefreshTokenTask: newRefreshTokenTask.Result);
    }

    private async Task UpdateRefreshTokenAsync(string refreshToken, DateTime tokenExpiryTime, Tuser tuser) =>
       await this.userSharedRepository.UpdateRefreshToken(refreshToken, tokenExpiryTime, tuser: tuser);

    private DataResponse<UserJwtTokenResponseDTO> Response((string? AccesToken, string? RefreshToken) tokensTuples)
    {
        var userJwtTokenRequestDTO = new UserJwtTokenResponseDTO()
        {
            Token = tokensTuples.AccesToken,
            RefreshToken = tokensTuples.RefreshToken
        };

        return DataResponse.Response<UserJwtTokenResponseDTO>(true, (int)HttpStatusCode.OK, userJwtTokenRequestDTO, "Generate new Refresh Token.");
    }

    async Task<DataResponse<UserJwtTokenResponseDTO>> IRequestHandler<RefreshTokenCommand, DataResponse<UserJwtTokenResponseDTO>>.Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check Null Request
            var isNUllRequestResult = Guard.Against.Null(request).ToResult();

            if (isNUllRequestResult.IsFailed)
                return RefreshTokenExceptionHandler.Argument_Null_Exception_Command_Handler;

            // Get Old Access Token and Refresh Token
            string accessToken = request.AccessToken;
            string refreshToken = request.RefreshToken;

            // Get Principle Claim by passing Old Access Token
            var principal = await jwtTokenService.GetPrincipalFromExpiredTokenAsync(this.options.Value.SecretKey, accessToken);

            // Get User Identifier
            var identifier = principal.Claims.First(i => i.Type == ClaimTypes.NameIdentifier).Value;

            if (String.IsNullOrEmpty(identifier))
                return RefreshTokenExceptionHandler.User_Identifier_Not_Found;

            // Get User Data By Identifier
            var userResult = await this.userSharedRepository.GetUserByIdentifierAsync(Guid.Parse(identifier));

            if (userResult.IsFailed)
                return RefreshTokenExceptionHandler.User_Not_Found;

            // Extract TUser Table Data
            Tuser tuser = userResult.Value.User;

            if (tuser.RefreshToken != refreshToken || tuser.RefreshTokenExpiryTime <= DateTime.Now)
                return RefreshTokenExceptionHandler.InValid_Client_Request;

            // Generate New Access Token And Refresh Token
            var tokensTuples = await this.GenerateNewAccessTokenAndRefreshTokenAsync(this.options.Value, principal.Claims.ToArray(), DateTime.Now.AddDays(1));
            if (String.IsNullOrEmpty(tokensTuples.AccessToken) || String.IsNullOrEmpty(tokensTuples.RefreshToken))
                return RefreshTokenExceptionHandler.Token_Generation_Failed;

            // Update Refresh Token
            await this.UpdateRefreshTokenAsync(tokensTuples.RefreshToken!, DateTime.Now.AddDays(7), tuser);

            // Response
            return this.Response(tokensTuples);
        }
        catch (Exception ex)
        {
            return RefreshTokenExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

#endregion Command Service