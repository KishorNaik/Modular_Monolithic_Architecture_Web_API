namespace Users.Application.Modules.Features.V1.Activity.ForgetPassword;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
public class UpdateForgetPasswordController : UserBaseController
{
    public UpdateForgetPasswordController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPatch("forgetPassword/newPassword")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<AddUserResponseDTO>>((int)HttpStatusCode.Created)]
    [ProducesResponseType<DataResponse<AddUserResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> UpdateForgerPasswordAsync([FromBody] UpdateForgetPasswordCommand updateForgetPasswordCommand, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(updateForgetPasswordCommand, cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation

public class UpdateForgetPasswordValidation : AbstractValidator<UpdateForgetPasswordCommand>
{
    public UpdateForgetPasswordValidation()
    {
        this.NewPasswordValidation();
        this.ResetPasswordTokenValidation();
    }

    private void NewPasswordValidation()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithErrorCode("Password")
            .MinimumLength(8).WithErrorCode("Password");
    }

    private void ResetPasswordTokenValidation()
    {
        RuleFor(x => x.ResetPasswordToken)
            .NotEmpty()
            .WithMessage("token should not be empty")
            .WithErrorCode("ResetPasswordToken")
            .Must((token) =>
            {
                Guid tokenGuid;
                var flag = Guid.TryParse(token.ToString(), out tokenGuid);

                return flag;
            })
            .WithMessage("ResetPasswordToken should be guid")
            .WithErrorCode("ResetPasswordToken");
    }
}

#endregion Validation

#region Exception Handler

public static class UpdateForgetPasswordExceptionHandler
{
    public static DataResponse<UpdateForgetPasswordResponseDTO> CommandHandlerException(string errorMessage) =>
   DataResponse.Response<UpdateForgetPasswordResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<UpdateForgetPasswordResponseDTO> Argument_Null_Exception_Command_Handler =>
       DataResponse.Response<UpdateForgetPasswordResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<UpdateForgetPasswordResponseDTO> InValidPasswordResetToken(int statusCode, string errorMessage) =>
     DataResponse.Response<UpdateForgetPasswordResponseDTO>(false, statusCode, null, errorMessage);
}

#endregion Exception Handler

#region Command Handler

public class UpdateForgetPasswordCommandHandler : IRequestHandler<UpdateForgetPasswordCommand, DataResponse<UpdateForgetPasswordResponseDTO>>
{
    private readonly UsersContext usersContext = null;
    private readonly IGenerateHashPasswordService generateHashPasswordService = null;
    private readonly IMediator mediator = null;

    public UpdateForgetPasswordCommandHandler(UsersContext usersContext, IGenerateHashPasswordService generateHashPasswordService, IMediator mediator)
    {
        this.usersContext = usersContext;
        this.generateHashPasswordService = generateHashPasswordService;
        this.mediator = mediator;
    }

    private async Task<Result<Tuser>> IsPasswordResendTokenValid(Guid? passwordResetToken)
    {
        var passwordResetTokenResult = await this.usersContext.Tusers.AsNoTracking().FirstOrDefaultAsync((e) => e.PasswordResetToken == passwordResetToken);

        if (passwordResetTokenResult is null)
            return Result.Fail(new FluentResults.Error("Invalid Password Reset Token").WithMetadata("StatusCode", HttpStatusCode.NotAcceptable));

        return Result.Ok<Tuser>(passwordResetTokenResult);
    }

    private Task<(string salt, string hash)> GenerateHashPasswordAsync(string? password) =>
       this.generateHashPasswordService.GenerateAsync(password!);

    private async Task UpdateAsync(string salt, string hash, Tuser tuser)
    {
        tuser.Salt = salt;
        tuser.Hash = hash;
        tuser.PasswordResetToken = null;

        this.usersContext.Update<Tuser>(tuser);
        await this.usersContext.SaveChangesAsync();
    }

    private DataResponse<UpdateForgetPasswordResponseDTO> Response()
    {
        return DataResponse.Response<UpdateForgetPasswordResponseDTO>(true, Convert.ToInt32(HttpStatusCode.OK), new UpdateForgetPasswordResponseDTO()
        {
            GenerateDateTime = DateTime.Now
        }, "New Password Updated");
    }

    public Task OnPublishNewPasswordUpdatedDomainEvent(string? emailId, Guid identifer) =>
        this.mediator.Publish(new NewPasswordUpdatedDomainEvent(emailId!, identifer));

    async Task<DataResponse<UpdateForgetPasswordResponseDTO>> IRequestHandler<UpdateForgetPasswordCommand, DataResponse<UpdateForgetPasswordResponseDTO>>.Handle(UpdateForgetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check request is null or not.
            var isNUllRequestResult = Guard.Against.Null(request).ToResult();

            if (isNUllRequestResult.IsFailed)
                return UpdateForgetPasswordExceptionHandler.Argument_Null_Exception_Command_Handler;

            // Check Password Resent Token Valid Or Not
            var passwordResetTokenValid = await this.IsPasswordResendTokenValid(request.ResetPasswordToken);

            if (passwordResetTokenValid.IsFailed)
                return UpdateForgetPasswordExceptionHandler.InValidPasswordResetToken(Convert.ToInt32(passwordResetTokenValid.Errors[0].Metadata["StatusCode"]), passwordResetTokenValid.Errors[0].Message);

            // Generate Hash and Salt based on new Password.
            (string salt, string hash) tuplesHashPassword = await this.GenerateHashPasswordAsync(request.NewPassword);

            // Update new Password
            Tuser tuser = passwordResetTokenValid.Value;
            await UpdateAsync(salt: tuplesHashPassword.salt, hash: tuplesHashPassword.hash, tuser: tuser);

            // ToDo: Event
            BackgroundJob.Enqueue(() => this.OnPublishNewPasswordUpdatedDomainEvent(tuser.EmailId, tuser.Identifier));

            // Response
            return this.Response();
        }
        catch (Exception ex)
        {
            return UpdateForgetPasswordExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

#endregion Command Handler

#region Event Service

public class NewPasswordUpdatedDomainEvent : INotification
{
    public NewPasswordUpdatedDomainEvent(string emailId, Guid identifier)
    {
        EmailId = emailId;
        Identifier = identifier;
    }

    public string EmailId { get; }

    public Guid Identifier { get; }
}

public class NewPasswordUpdatedDomainEventHandler : INotificationHandler<NewPasswordUpdatedDomainEvent>
{
    private readonly ILogger<NewPasswordUpdatedDomainEventHandler> logger;
    private readonly IMediator mediator = null;

    public NewPasswordUpdatedDomainEventHandler(ILogger<NewPasswordUpdatedDomainEventHandler> logger, IMediator mediator)
    {
        this.logger = logger;
        this.mediator = mediator;
    }

    private Task OnPublishNewPasswordUpdatedEmailIntegrationEvent(string? emailid) =>
         this.mediator.Publish(new NewPasswordUpdatedEmailIntegrationEvent(emailid));

    private Task OnPublishCacheDomainEvent(Guid? identifier) =>
        mediator.Publish(new UserSharedCacheService(identifier));

    async Task INotificationHandler<NewPasswordUpdatedDomainEvent>.Handle(NewPasswordUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            //Cache Event
            await this.OnPublishCacheDomainEvent(notification.Identifier);

            // New Password Updated Email
            await this.OnPublishNewPasswordUpdatedEmailIntegrationEvent(notification.EmailId);
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{nameof(NewPasswordUpdatedDomainEventHandler)} => Message: {ex.Message}");
        }
    }
}

#endregion Event Service