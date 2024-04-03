using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Users.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public class UpdateUserPasswordController : UserBaseController
{
    private readonly IUserProviderService userProviderService = null;

    public UpdateUserPasswordController(IMediator mediator, IUserProviderService userProviderService) : base(mediator)
    {
        this.userProviderService = userProviderService;
    }

    [HttpPatch("new-password")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [Authorize]
    [ProducesResponseType<DataResponse<UpdateUserPasswordResponseDTO>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<UpdateUserPasswordResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> UpdatePasswordAsync([FromBody] UpdateUserPasswordCommand updateUserPasswordCommand, CancellationToken cancellationToken)
    {
        // Get User Identifier from JWT Token.
        updateUserPasswordCommand.Identifier = this.userProviderService.GetUserIdentifier();

        // Call Command
        var response = await this.Mediator.Send(updateUserPasswordCommand);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

public class UpdateUserPasswordValidationHandler : AbstractValidator<UpdateUserPasswordCommand>
{
    public UpdateUserPasswordValidationHandler()
    {
        this.PasswordValidation();
    }

    private void PasswordValidation()
    {
        base.RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("Password")
            .MinimumLength(8).WithErrorCode("Password")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Password must not contain HTML tags.").WithErrorCode("Password")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Password must not contain JavaScript.").WithErrorCode("Password");
    }
}

#endregion Validation Service

#region Exception Service

public static class UpdateUserPasswordExceptionHandler
{
    public static DataResponse<UpdateUserPasswordResponseDTO> CommandHandlerException(string errorMessage) =>
        DataResponse.Response<UpdateUserPasswordResponseDTO>(success: false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<UpdateUserPasswordResponseDTO> Argument_Null_Exception_Command_Handler =>
        DataResponse.Response<UpdateUserPasswordResponseDTO>(success: false, (int?)HttpStatusCode.BadRequest, null, "Argument should not be empty.");

    public static DataResponse<UpdateUserPasswordResponseDTO> Hash_Password_Exception(string errorMessage, int statusCode) =>
        DataResponse.Response<UpdateUserPasswordResponseDTO>(success: false, statusCode, null, errorMessage);

    public static DataResponse<UpdateUserPasswordResponseDTO> User_Not_Found(string errorMessage, int statusCode) =>
        DataResponse.Response<UpdateUserPasswordResponseDTO>(success: false, statusCode, null, errorMessage);
}

#endregion Exception Service

#region Command Service

public class UpdateUserPasswordCommand : UpdateUserPasswordRequestDTO, IRequest<DataResponse<UpdateUserPasswordResponseDTO>>
{
}

public class UpdateUserPasswordCommandHandler : IRequestHandler<UpdateUserPasswordCommand, DataResponse<UpdateUserPasswordResponseDTO>>
{
    private readonly IGenerateHashPasswordService generateHashPasswordService = null;
    private readonly UsersContext usersContext = null;
    private readonly IUserSharedRepository userSharedRepository = null;
    private readonly IMediator mediator = null;

    public UpdateUserPasswordCommandHandler(IGenerateHashPasswordService generateHashPasswordService, IUserSharedRepository userSharedRepository, UsersContext usersContext, IMediator mediator)
    {
        this.generateHashPasswordService = generateHashPasswordService;
        this.userSharedRepository = userSharedRepository;
        this.usersContext = usersContext;
        this.mediator = mediator;
    }

    private async Task<Result<(string salt, string hash)>> GenerateHashPasswordAsync(string password)
    {
        try
        {
            var hashPasswordResultSet = await this.generateHashPasswordService.GenerateAsync(password);

            if (hashPasswordResultSet.hash is null)
                return Result.Fail(new FluentResults.Error("Hash is empty").WithMetadata("StatusCode", HttpStatusCode.Conflict));

            if (hashPasswordResultSet.salt is null)
                return Result.Fail(new FluentResults.Error("salt is empty").WithMetadata("StatusCode", HttpStatusCode.Conflict));

            return Result.Ok<(string salt, string hash)>(hashPasswordResultSet);
        }
        catch (Exception ex)
        {
            return Result.Fail(new FluentResults.Error(ex.Message).WithMetadata("StatusCode", HttpStatusCode.InternalServerError));
        }
    }

    private async Task UpdateAsync(Tuser tuser, (string salt, string hash) passwordTuples)
    {
        tuser.Salt = passwordTuples.salt;
        tuser.Hash = passwordTuples.hash;

        this.usersContext.Update<Tuser>(tuser);
        await this.usersContext.SaveChangesAsync();
    }

    private DataResponse<UpdateUserPasswordResponseDTO> Response()
    {
        return DataResponse.Response<UpdateUserPasswordResponseDTO>(true, Convert.ToInt32(HttpStatusCode.OK), new UpdateUserPasswordResponseDTO()
        {
            UpdatedTime = DateTime.Now
        }, "New password update successfully");
    }

    public Task OnPublishUserPasswordUpdatedDomainEvent(Guid? identifier) =>
        this.mediator.Publish(new UserPasswordUpdatedDomainEvent(identifier!));

    async Task<DataResponse<UpdateUserPasswordResponseDTO>> IRequestHandler<UpdateUserPasswordCommand, DataResponse<UpdateUserPasswordResponseDTO>>.Handle(UpdateUserPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // check request is empty or not.
            if (request is null)
                return UpdateUserPasswordExceptionHandler.Argument_Null_Exception_Command_Handler;

            // Generate Hash and Salt
            var generateHashResult = await this.GenerateHashPasswordAsync(request.Password);

            if (generateHashResult.IsFailed)
                return UpdateUserPasswordExceptionHandler.Hash_Password_Exception(generateHashResult.Errors[0].Message, Convert.ToInt32(generateHashResult.Errors[0].Metadata["StatusCode"]));

            (string salt, string hash) hashPasswordTuples = generateHashResult.Value;

            // Get User By Identifier
            var getUserByIdentifierResult = await this.userSharedRepository.GetUserByIdentifierAsync(request.Identifier);
            if (getUserByIdentifierResult.IsFailed)
                return UpdateUserPasswordExceptionHandler.User_Not_Found(getUserByIdentifierResult.Errors[0].Message, Convert.ToInt32(getUserByIdentifierResult.Errors[0].Metadata["StatusCode"]));

            // Update
            Tuser tuser = getUserByIdentifierResult.Value.User;
            await this.UpdateAsync(tuser, hashPasswordTuples);

            // Call Domain Event
            BackgroundJob.Enqueue(() => this.OnPublishUserPasswordUpdatedDomainEvent(request.Identifier));

            // Response
            return this.Response();
        }
        catch (Exception ex)
        {
            return UpdateUserPasswordExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

#endregion Command Service

#region Event Service

public class UserPasswordUpdatedDomainEvent : INotification
{
    public UserPasswordUpdatedDomainEvent(Guid? identifier)
    {
        this.Identifier = identifier;
    }

    public Guid? Identifier { get; }
}

public class UserPasswordUpdatedDomainEventHandler : INotificationHandler<UserPasswordUpdatedDomainEvent>
{
    private readonly IMediator mediator = null;
    private readonly ILogger<UserPasswordUpdatedDomainEventHandler> logger = null;

    public UserPasswordUpdatedDomainEventHandler(IMediator mediator, ILogger<UserPasswordUpdatedDomainEventHandler> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    async Task INotificationHandler<UserPasswordUpdatedDomainEvent>.Handle(UserPasswordUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await this.mediator.Publish(new UserSharedCacheService(notification.Identifier));
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{nameof(UserPasswordUpdatedDomainEventHandler)} - {ex.Message}");
        }
    }
}

#endregion Event Service