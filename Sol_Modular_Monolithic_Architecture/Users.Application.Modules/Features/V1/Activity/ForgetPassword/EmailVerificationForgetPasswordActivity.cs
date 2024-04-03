namespace Users.Application.Modules.Features.V1.Activity.ForgetPassword;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public class EmailVerificationForgetPasswordController : UserBaseController
{
    public EmailVerificationForgetPasswordController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("forget-password/email-verifications")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<UserJwtTokenResponseDTO>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<UserJwtTokenResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> EmailVerificationAsync(EmailVerificationForgetPasswordCommand emailVerificationForgetPasswordCommand, CancellationToken cancellationToken)
    {
        var response = await this.Mediator.Send(emailVerificationForgetPasswordCommand, cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

public class EmailVerificationForgetPasswordValidation : AbstractValidator<EmailVerificationForgetPasswordCommand>
{
    private readonly IActionContextAccessor actionContextAccessor = null;

    public EmailVerificationForgetPasswordValidation(IActionContextAccessor actionContextAccessor)
    {
        this.actionContextAccessor = actionContextAccessor;

        //this.TokenValidation();
        this.EmailIdValidation();
    }

    //private void TokenValidation()
    //{
    //    RuleFor(x => x.Token)
    //        .Must((context, id, propertyValidatorContext) =>
    //        {
    //            var token = (string)actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("Token");

    //            if (token is null)
    //                return false;

    //            return true;
    //        })
    //        .WithMessage("token should not be empty")
    //        .WithErrorCode("Token")
    //        .Must((context, id, propertyValidatorContext) =>
    //        {
    //            var token = (string)actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("Token");

    //            Guid tokenGuid;
    //            var flag = Guid.TryParse(token, out tokenGuid);

    //            return flag;
    //        })
    //        .WithMessage("Token should be guid")
    //        .WithErrorCode("Token");
    //}

    private void EmailIdValidation()
    {
        RuleFor(x => x.Body.EmailId)
            .NotEmpty().WithErrorCode("EmailId")
            .EmailAddress().WithErrorCode("EmailId");
    }
}

#endregion Validation Service

#region Exception Helper

public static class EmailVerificationForgetPasswordExceptionHandler
{
    public static DataResponse<EmailVerificationForgetPasswordResponseDTO> CommandHandlerException(string errorMessage) =>
      DataResponse.Response<EmailVerificationForgetPasswordResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<EmailVerificationForgetPasswordResponseDTO> Argument_Null_Exception_Command_Handler =>
       DataResponse.Response<EmailVerificationForgetPasswordResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<EmailVerificationForgetPasswordResponseDTO> Email_Id_Not_Found =>
       DataResponse.Response<EmailVerificationForgetPasswordResponseDTO>(false, (int?)HttpStatusCode.NotFound, null, "Email id doesn't exists.");
}

#endregion Exception Helper

#region Command Service

public class EmailVerificationForgetPasswordCommand : EmailVerificationForgetPasswordApiRequestDTO, IRequest<DataResponse<EmailVerificationForgetPasswordResponseDTO>>
{
}

public class EmailVerificationForgetPasswordCommandHandler : IRequestHandler<EmailVerificationForgetPasswordCommand, DataResponse<EmailVerificationForgetPasswordResponseDTO>>
{
    private readonly IUserSharedRepository userSharedRepository;
    private readonly UsersContext usersContext;
    private readonly IMediator mediator = null;

    public EmailVerificationForgetPasswordCommandHandler(IUserSharedRepository userSharedRepository, UsersContext usersContext, IMediator mediator)
    {
        this.userSharedRepository = userSharedRepository;
        this.usersContext = usersContext;
        this.mediator = mediator;
    }

    private Guid? GeneratePasswordResetToken => Guid.NewGuid();

    private async Task UpdateGeneratedPasswordResetToken(Guid? resetToken, Tuser tuser)
    {
        tuser.PasswordResetToken = resetToken;
        this.usersContext.Update<Tuser>(tuser);
        await this.usersContext.SaveChangesAsync();
    }

    private DataResponse<EmailVerificationForgetPasswordResponseDTO> Response(Guid? generatePasswordResetToken)
    {
        return DataResponse.Response<EmailVerificationForgetPasswordResponseDTO>(true, Convert.ToInt32(HttpStatusCode.OK), new EmailVerificationForgetPasswordResponseDTO()
        {
            GenerateDateTime = DateTime.Now,
            PasswordResetToken = generatePasswordResetToken
        }, "Generate Password Reset Token");
    }

    public Task OnPublishGeneratePasswordResetTokenUpdatedDomainEvent(string? emailId, Guid? passwordResetToken, Guid? identifier) =>
        this.mediator.Publish(new GeneratePasswordResetToneUpdatedDomainEvent(emailId, passwordResetToken, identifier));

    async Task<DataResponse<EmailVerificationForgetPasswordResponseDTO>> IRequestHandler<EmailVerificationForgetPasswordCommand, DataResponse<EmailVerificationForgetPasswordResponseDTO>>.Handle(EmailVerificationForgetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check Request is EMpty Or Not
            var isNUllRequestResult = Guard.Against.Null(request).ToResult();

            if (isNUllRequestResult.IsFailed)
                return EmailVerificationForgetPasswordExceptionHandler.Argument_Null_Exception_Command_Handler;

            // Email Verification
            var emailVerificationResult = await this.userSharedRepository.GetUserByEmailIdAsync(request.Body.EmailId);

            if (emailVerificationResult.IsFailed)
                return EmailVerificationForgetPasswordExceptionHandler.Email_Id_Not_Found;

            Tuser tuser = emailVerificationResult.Value;

            // Generate Password Reset Token
            Guid? generatePasswordResetToken = this.GeneratePasswordResetToken;

            // Update Reset Password Token
            await this.UpdateGeneratedPasswordResetToken(generatePasswordResetToken, tuser);

            //Send Password Reset Link to Registered Email Id.
            BackgroundJob.Enqueue(() => this.OnPublishGeneratePasswordResetTokenUpdatedDomainEvent(tuser.EmailId, generatePasswordResetToken, tuser.Identifier));

            // Response
            return this.Response(generatePasswordResetToken);
        }
        catch (Exception ex)
        {
            return EmailVerificationForgetPasswordExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

#endregion Command Service

#region Event Service

public class GeneratePasswordResetToneUpdatedDomainEvent : INotification
{
    public GeneratePasswordResetToneUpdatedDomainEvent(string? emaiId, Guid? passwordResetToken, Guid? identifier)
    {
        EmailId = emaiId;
        PasswordResetToken = passwordResetToken;
        Identifier = identifier;
    }

    public string? EmailId { get; }

    public Guid? PasswordResetToken { get; }

    public Guid? Identifier { get; }
}

public class GeneratePasswordResetTokenUpdatedDomainEventHandler : INotificationHandler<GeneratePasswordResetToneUpdatedDomainEvent>
{
    private readonly IMediator mediator = null;
    private readonly ILogger<GeneratePasswordResetTokenUpdatedDomainEventHandler> logger = null;

    public GeneratePasswordResetTokenUpdatedDomainEventHandler(IMediator mediator, ILogger<GeneratePasswordResetTokenUpdatedDomainEventHandler> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    private Task OnPublishForgetPasswordIntegrationEvent(string? emailid, Guid? passwordResetToken) =>
        this.mediator.Publish(new ForgetPasswordEmailVerificationIntegrationEvent(emailid, passwordResetToken));

    private Task OnPublishCacheDomainEvent(Guid? identifier) =>
        mediator.Publish(new UserSharedCacheService(identifier));

    async Task INotificationHandler<GeneratePasswordResetToneUpdatedDomainEvent>.Handle(GeneratePasswordResetToneUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            //Cache Event
            await this.OnPublishCacheDomainEvent(notification.Identifier);

            // Publish Forget Password integration Event.
            await this.OnPublishForgetPasswordIntegrationEvent(notification.EmailId, notification.PasswordResetToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{nameof(GeneratePasswordResetTokenUpdatedDomainEventHandler)} => Message: {ex.Message}");
        }
    }
}

#endregion Event Service