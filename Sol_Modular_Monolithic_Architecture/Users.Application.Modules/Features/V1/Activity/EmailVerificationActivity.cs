namespace Users.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
public class EmailVerificationSignUpController : UserBaseController
{
    public EmailVerificationSignUpController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("emailVerificationsSignUp/{Token}")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<UserJwtTokenResponseDTO>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<UserJwtTokenResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> EmailVerificationAsync([FromRoute] EmailVerificationSignUpCommand emailVerificationCommand, CancellationToken cancellationToken)
    {
        var response = await this.Mediator.Send(emailVerificationCommand, cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

public class EmailVerificationSignUpValidation : AbstractValidator<EmailVerificationSignUpCommand>
{
    private readonly IActionContextAccessor actionContextAccessor = null;

    public EmailVerificationSignUpValidation(IActionContextAccessor actionContextAccessor)
    {
        this.actionContextAccessor = actionContextAccessor;

        this.TokenValidation();
    }

    private void TokenValidation()
    {
        RuleFor(x => x.Token)
            .Must((context, id, propertyValidatorContext) =>
            {
                var token = (string)actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("Token");

                if (token is null)
                    return false;

                return true;
            })
            .WithMessage("token should not be empty")
            .WithErrorCode("Token")
            .Must((context, id, propertyValidatorContext) =>
            {
                var token = (string)actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("Token");

                Guid tokenGuid;
                var flag = Guid.TryParse(token, out tokenGuid);

                return flag;
            })
            .WithMessage("Token should be guid")
            .WithErrorCode("Token");
    }
}

#endregion Validation Service

#region Exception Service

public static class EmailVerificationSignUpExceptionHandler
{
    public static DataResponse<EmailVerificationSignUpResponseDTO> CommandHandlerException(string errorMessage) =>
    DataResponse.Response<EmailVerificationSignUpResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<EmailVerificationSignUpResponseDTO> Argument_Null_Exception_Command_Handler =>
       DataResponse.Response<EmailVerificationSignUpResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<EmailVerificationSignUpResponseDTO> Email_Verification_Already_Done(int statusCode, string errorMessage) =>
      DataResponse.Response<EmailVerificationSignUpResponseDTO>(false, statusCode, null, errorMessage);

    public static DataResponse<EmailVerificationSignUpResponseDTO> Email_Verification_Failed(int statusCode, string errorMessage) =>
     DataResponse.Response<EmailVerificationSignUpResponseDTO>(false, statusCode, null, errorMessage);
}

#endregion Exception Service

#region Command Service

public class EmailVerificationSignUpCommandHandler : IRequestHandler<EmailVerificationSignUpCommand, DataResponse<EmailVerificationSignUpResponseDTO>>
{
    private readonly UsersContext usersContext;

    public EmailVerificationSignUpCommandHandler(UsersContext usersContext)
    {
        this.usersContext = usersContext;
    }

    private async Task<Result<Tuser>> EmailTokenValidAsync(Guid? emailToken)
    {
        var tUserResult = await this.usersContext.Tusers.AsNoTracking().FirstOrDefaultAsync(e => e.EmailToken == emailToken && e.IsEmailVerified == false);

        if (tUserResult is null)
            return Result.Fail(new FluentResults.Error("The email verification already done.").WithMetadata("StatusCode", HttpStatusCode.NotAcceptable));

        return Result.Ok<Tuser>(tUserResult);
    }

    private async Task<Result> UpdateEmailVerificationAsync(Tuser tuser)
    {
        Map(tuser);

        this.usersContext.Update<Tuser>(tuser);

        int flag = await this.usersContext.SaveChangesAsync();

        if (flag < 0)
            return Result.Fail(new FluentResults.Error("Email Verification update failed").WithMetadata("StatusCode", HttpStatusCode.InternalServerError));

        return Result.Ok();
    }

    private void Map(Tuser tuser)
    {
        tuser.EmailToken = null;
        tuser.IsEmailVerified = true;
        tuser.Status = Convert.ToBoolean((int)StatusEnum.Active);
    }

    private DataResponse<EmailVerificationSignUpResponseDTO> Response()
    {
        return DataResponse.Response<EmailVerificationSignUpResponseDTO>(true, (int)HttpStatusCode.OK, new EmailVerificationSignUpResponseDTO()
        {
            GenerateDateTime = DateTime.Now
        }, "The Email Verification done");
    }

    async Task<DataResponse<EmailVerificationSignUpResponseDTO>> IRequestHandler<EmailVerificationSignUpCommand, DataResponse<EmailVerificationSignUpResponseDTO>>.Handle(EmailVerificationSignUpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check request is null or not.
            var isNullRequestResult = Guard.Against.Null(request).ToResult();

            if (isNullRequestResult.IsFailed)
                return EmailVerificationSignUpExceptionHandler.Argument_Null_Exception_Command_Handler;

            // Check Token is Valid Or Not.
            var emailTokenValidResult = await this.EmailTokenValidAsync(request.Token);

            if (emailTokenValidResult.IsFailed)
                return EmailVerificationSignUpExceptionHandler.Email_Verification_Already_Done(Convert.ToInt32(emailTokenValidResult.Errors[0].Metadata["StatusCode"]), emailTokenValidResult.Errors[0].Message);

            // Update Email Verification Flag
            Tuser tuser = emailTokenValidResult.Value;
            var updateEmailVerificationResult = await this.UpdateEmailVerificationAsync(tuser);

            if (updateEmailVerificationResult.IsFailed)
                return EmailVerificationSignUpExceptionHandler.Email_Verification_Failed(Convert.ToInt32(emailTokenValidResult.Errors[0].Metadata["StatusCode"]), emailTokenValidResult.Errors[0].Message);

            return this.Response();
        }
        catch (Exception ex)
        {
            return EmailVerificationSignUpExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

#endregion Command Service