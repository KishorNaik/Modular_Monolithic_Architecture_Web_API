namespace Users.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public class UserLoginController : UserBaseController
{
    public UserLoginController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("login")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<UserLoginResponseDTO>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<UserLoginResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginCommand userLoginCommand, CancellationToken cancellationToken)
    {
        var response = await this.Mediator.Send(userLoginCommand, cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

public class UserLoginValidation : AbstractValidator<UserLoginCommand>

{
    public UserLoginValidation()
    {
        this.EmailIdValidation();
        this.PasswordValidation();
    }

    private void EmailIdValidation()
    {
        RuleFor(x => x.EmailId)
            .NotEmpty().WithErrorCode("EmailId")
            .EmailAddress().WithErrorCode("EmailId");
    }

    private void PasswordValidation()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("Password")
            .MinimumLength(8).WithErrorCode("Password");
    }
}

#endregion Validation Service

#region Exception Service

public static class UserLoginExceptionHandler
{
    public static Result<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)> DbException(string errorMessage, string stackTrace) =>
      Result.Fail<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)>(new FluentResults.Error(errorMessage).WithMetadata("StackTrace", stackTrace).WithMetadata("StatusCode", HttpStatusCode.InternalServerError));

    public static Result<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)> Argument_Null_Exception_Data_Service =>
        Result.Fail<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)>(new FluentResults.Error("Arguments should not be empty.").WithMetadata("StatusCode", HttpStatusCode.BadRequest));

    public static Result<Tuser> LoginFailed =>
        Result.Fail<Tuser>(new FluentResults.Error("Email id and Password does not match").WithMetadata("StatusCode", HttpStatusCode.NotFound));

    public static DataResponse<UserLoginResponseDTO> CommandHandlerException(string errorMessage) =>
        DataResponse.Response<UserLoginResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<UserLoginResponseDTO> Argument_Null_Exception_Command_Handler =>
        DataResponse.Response<UserLoginResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<UserLoginResponseDTO> LoginFailed_Command_Handler =>
        DataResponse.Response<UserLoginResponseDTO>(false, (int?)HttpStatusCode.NotFound, null, "Email id and Password does not match");

    public static DataResponse<UserLoginResponseDTO> Token_Generation_Failed =>
        DataResponse.Response<UserLoginResponseDTO>(false, (int?)HttpStatusCode.Conflict, null, "Token Generation Failed.");

    public static DataResponse<UserLoginResponseDTO> Validate_Email_Exception(string errorMessage, int statusCode) =>
         DataResponse.Response<UserLoginResponseDTO>(false, Convert.ToInt32(statusCode), null, errorMessage);
}

#endregion Exception Service

#region Data Service

public class UserLoginDataService : IRequest<Result<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)>>
{
    public UserLoginDataService(string? emailId)
    {
        EmailId = emailId;
    }

    public string? EmailId { get; set; }
}

public class UserLoginDataServiceHandler : IRequestHandler<UserLoginDataService, Result<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)>>
{
    private readonly UsersContext usersContext = null;
    private readonly IUserSharedRepository userSharedRepository = null;

    public UserLoginDataServiceHandler(UsersContext usersContext, IUserSharedRepository userSharedRepository)
    {
        this.usersContext = usersContext;
        this.userSharedRepository = userSharedRepository;
    }

    private Task<Result<Tuser>> LoginValidateAsync(string emailId) =>
        this.userSharedRepository.GetUserByEmailIdAsync(emailId);

    private Result CheckEmailConfirmation(bool? isEmailVerified)
    {
        if (isEmailVerified == false)
            return Result.Fail(new FluentResults.Error("The email verification is pending").WithMetadata("StatusCode", HttpStatusCode.Conflict));

        return Result.Ok();
    }

    private Result CheckStatusValid(StatusEnum statusEnum)
    {
        if (statusEnum == StatusEnum.Inactive)
            return Result.Fail(new FluentResults.Error("The user status is pending").WithMetadata("StatusCode", HttpStatusCode.Conflict));

        return Result.Ok();
    }

    private Result<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)> Response(Tuser tuser)
    {
        (UserLoginResponseDTO userLoginResponseDTO, Tuser tuser) tuplesMap = (new UserLoginResponseDTO()
        {
            User = new UserResponseDTO()
            {
                Identifier = tuser.Identifier,
                FirstName = tuser.FirstName,
                LastName = tuser.LastName,
                UserType = (UserType)tuser.UserType,
                Hash = tuser.Hash,
                Salt = tuser.Salt,
                EmailId = tuser.EmailId,
            },
            JwtToken = null!
        },
        tuser
        );

        return Result.Ok<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)>(tuplesMap);
    }

    async Task<Result<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)>> IRequestHandler<UserLoginDataService, Result<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)>>.Handle(UserLoginDataService request, CancellationToken cancellationToken)
    {
        try
        {
            // Check Argument
            var checkRequest = Guard.Against.Null(request).ToResult();

            if (checkRequest.IsFailed)
                return UserLoginExceptionHandler.Argument_Null_Exception_Data_Service;

            // Validate Login
            var loginValidationResult = await this.LoginValidateAsync(request.EmailId);

            if (loginValidationResult.IsFailed)
                return Result.Fail<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)>(new FluentResults.Error(loginValidationResult.Errors[0].Message).WithMetadata("StatusCode", loginValidationResult.Errors[0].Metadata["StatusCode"]));

            // Check Email Verification is Pending.
            var emailVerificationPendingReuslt = this.CheckEmailConfirmation(loginValidationResult.Value.IsEmailVerified);

            if (emailVerificationPendingReuslt.IsFailed)
                return Result.Fail<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)>(new FluentResults.Error(emailVerificationPendingReuslt.Errors[0].Message).WithMetadata("StatusCode", emailVerificationPendingReuslt.Errors[0].Metadata["StatusCode"]));

            Tuser tuser = loginValidationResult.Value;

            // Check User Status
            var checkUserStatusResult = this.CheckStatusValid(tuser.Status ? StatusEnum.Active : StatusEnum.Inactive);

            if (checkUserStatusResult.IsFailed)
                return Result.Fail<(UserLoginResponseDTO userLoginResponseDTO, Tuser tuser)>(new FluentResults.Error(emailVerificationPendingReuslt.Errors[0].Message).WithMetadata("StatusCode", emailVerificationPendingReuslt.Errors[0].Metadata["StatusCode"]));

            return this.Response(tuser: loginValidationResult.Value);
        }
        catch (Exception ex)
        {
            return UserLoginExceptionHandler.DbException(ex.Message, ex.StackTrace);
        }
    }
}

#endregion Data Service

#region Command Service

public class UserLoginCommand : LoginUserRequestDTO, IRequest<DataResponse<UserLoginResponseDTO>>
{
}

public class UserLoginCommandHandler : IRequestHandler<UserLoginCommand, DataResponse<UserLoginResponseDTO>>
{
    private readonly IMediator mediator = null;
    private readonly IJwtTokenService jwtTokenService;
    private readonly IOptions<JwtAppSetting> options;
    private readonly IUserSharedRepository userSharedRepository = null;

    public UserLoginCommandHandler(IMediator mediator, IJwtTokenService jwtTokenService, IOptions<JwtAppSetting> options, IUserSharedRepository userSharedRepository)
    {
        this.mediator = mediator;
        this.jwtTokenService = jwtTokenService;
        this.options = options;
        this.userSharedRepository = userSharedRepository;
    }

    private async Task<(string? JwtToken, string? RefreshToken)> GenerateJwtAndRefreshToken(Guid? identifier, UserType? role, string emailId)
    {
        List<Claim> claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, Convert.ToString(identifier)));
        claims.Add(new Claim(ClaimTypes.Role, role.ToString()));

        var jwtTokenTaskResult = this.jwtTokenService.GenerateJwtTokenAsync(this.options.Value, claims.ToArray(), DateTime.Now.AddDays(1));
        var refreshTokenTaskResult = this.jwtTokenService.GenerateRefreshTokenAsync();

        await Task.WhenAll(jwtTokenTaskResult, refreshTokenTaskResult);

        return (jwtTokenTaskResult.Result, refreshTokenTaskResult.Result);
    }

    private async Task UpdateRefreshTokenAsync(string refreshToken, DateTime tokenExpiryTime, Tuser tuser) =>
        await this.userSharedRepository.UpdateRefreshToken(refreshToken, tokenExpiryTime, tuser: tuser);

    private DataResponse<UserLoginResponseDTO> Response(UserLoginResponseDTO userLoginResponseDTO, (string? JwtToken, string? RefreshToken) tokensTuples)
    {
        userLoginResponseDTO.JwtToken = new UserJwtTokenResponseDTO();
        userLoginResponseDTO.JwtToken.Token = tokensTuples.JwtToken;
        userLoginResponseDTO.JwtToken.RefreshToken = tokensTuples.RefreshToken;

        userLoginResponseDTO.User.Hash = string.Empty;
        userLoginResponseDTO.User.Salt = string.Empty;

        return DataResponse.Response(true, (int)HttpStatusCode.OK, userLoginResponseDTO, "Login Successfully");
    }

    async Task<DataResponse<UserLoginResponseDTO>> IRequestHandler<UserLoginCommand, DataResponse<UserLoginResponseDTO>>.Handle(UserLoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check Command Request is Empty or Not.
            var checkRequestResult = Guard.Against.Null(request).ToResult();

            if (checkRequestResult.IsFailed)
                return UserLoginExceptionHandler.Argument_Null_Exception_Command_Handler;

            // validate Email
            var validateEmailResult = await this.mediator.Send(new UserLoginDataService(request.EmailId));

            if (validateEmailResult.IsFailed)
                return UserLoginExceptionHandler.Validate_Email_Exception(validateEmailResult.Errors[0].Message, Convert.ToInt32(validateEmailResult.Errors[0].Metadata["StatusCode"]));

            UserLoginResponseDTO userLoginResponseDTO = validateEmailResult.Value.userLoginResponseDTO;

            // Validate Password
            var IsValidLogin = await Hash.ValidateAsync(request.Password, userLoginResponseDTO.User.Salt, userLoginResponseDTO.User.Hash, ByteRange.byte256).ConfigureAwait(false);

            if (!IsValidLogin)
                return UserLoginExceptionHandler.LoginFailed_Command_Handler;

            // Generate Jwt Token And Refresh Token
            var tokensTuples = await this.GenerateJwtAndRefreshToken(userLoginResponseDTO.User.Identifier, (UserType?)userLoginResponseDTO.User.UserType, userLoginResponseDTO.User.EmailId);

            if (String.IsNullOrEmpty(tokensTuples.JwtToken) || String.IsNullOrEmpty(tokensTuples.RefreshToken))
                return UserLoginExceptionHandler.Token_Generation_Failed;

            // Update Refresh Token
            await this.UpdateRefreshTokenAsync(tokensTuples.RefreshToken!, DateTime.Now.AddDays(7), validateEmailResult.Value.tuser);

            // Response
            return this.Response(userLoginResponseDTO, tokensTuples);
        }
        catch (Exception ex)
        {
            return UserLoginExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

#endregion Command Service