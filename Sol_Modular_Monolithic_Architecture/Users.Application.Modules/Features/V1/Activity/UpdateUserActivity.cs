namespace Users.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
public class UpdateUserController : UserBaseController
{
    private readonly IUserProviderService userProviderService = null;

    public UpdateUserController(IMediator mediator, IUserProviderService userProviderService) : base(mediator)
    {
        this.userProviderService = userProviderService;
    }

    [HttpPut()]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [Authorize]
    [ProducesResponseType<DataResponse<UpdateUserResponseDTO>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<UpdateUserResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateUserCommand updateUserCommand, CancellationToken cancellationToken)
    {
        updateUserCommand.Identifier = this.userProviderService.GetUserIdentifier();
        var response = await Mediator.Send(updateUserCommand, cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Services

public class UpdateUserValidationService : AbstractValidator<UpdateUserCommand>
{
    private readonly IActionContextAccessor actionContextAccessor = null;

    public UpdateUserValidationService(IActionContextAccessor actionContextAccessor)
    {
        this.actionContextAccessor = actionContextAccessor;
        this.FirstNameValidation();
        this.LastNameValidation();
        this.EmailIdValidation();
        this.MobileValidation();
        this.UserTypeValidation();
    }

    private void FirstNameValidation()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithErrorCode("FirstName")
            .Length(0, 50).WithMessage("First Name must be less than 50 characters").WithErrorCode("FirstName")
            .MaximumLength(50).WithErrorCode("FirstName")
            .Matches(new Regex(@"^[a-zA-Z0-9 ]*$")).WithMessage("First Name must not contain special characters.").WithErrorCode("FirstName")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("First Name must not contain HTML tags.").WithErrorCode("FirstName")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("First Name must not contain JavaScript.").WithErrorCode("FirstName");
    }

    private void LastNameValidation()
    {
        RuleFor(x => x.LastName)
           .NotEmpty().WithErrorCode("LastName")
           .Length(0, 50).WithMessage("Last Name must be less than 50 charcters").WithErrorCode("LastName")
           .MaximumLength(50).WithErrorCode("LastName")
           .Matches(new Regex(@"^[a-zA-Z0-9 ]*$")).WithMessage("Last Name must not contain special characters.").WithErrorCode("LastName")
           .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Last Name must not contain HTML tags.").WithErrorCode("LastName")
           .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Last Name must not contain JavaScript.").WithErrorCode("FirstName");
    }

    private void EmailIdValidation()
    {
        RuleFor(x => x.EmailId)
            .NotEmpty().WithErrorCode("EmailId")
            .EmailAddress().WithErrorCode("EmailId")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Email must not contain HTML tags.").WithErrorCode("EmailId")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Email must not contain JavaScript.").WithErrorCode("EmailId");
    }

    private void MobileValidation()
    {
        RuleFor(x => x.MobileNo)
            .NotEmpty().WithErrorCode("MobileNo")
            .Matches(@"^\d{10}$").WithMessage("Mobile no must be 10 digit").WithErrorCode("MobileNo")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Mobile No must not contain HTML tags.").WithErrorCode("MobileNo")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Mobile No Name must not contain JavaScript.").WithErrorCode("MobileNo");
    }

    private void UserTypeValidation()
    {
        RuleFor(x => x.UserType)
            .NotEmpty().WithErrorCode("UserType")
            .IsInEnum().WithMessage("Invalid User Type").WithErrorCode("UserType");
    }
}

#endregion Validation Services

#region Exception Service

public static class UpdateUserExceptionHandler
{
    public static Result<UpdateUserResponseDTO> DbException(string errorMessage, string stackTrace) =>
      Result.Fail<UpdateUserResponseDTO>(new FluentResults.Error(errorMessage).WithMetadata("StackTrace", stackTrace).WithMetadata("StatusCode", HttpStatusCode.InternalServerError));

    public static Result<UpdateUserResponseDTO> Argument_Null_Exception_Data_Service =>
        Result.Fail<UpdateUserResponseDTO>(new FluentResults.Error("Arguments should not be empty.").WithMetadata("StatusCode", HttpStatusCode.BadRequest));

    public static Result<UpdateUserResponseDTO> Get_User_Result_Exception_Data_Service(int statusCode, string errorMessage) =>
        Result.Fail<UpdateUserResponseDTO>(new FluentResults.Error(errorMessage).WithMetadata("StatusCode", statusCode));

    public static DataResponse<UpdateUserResponseDTO> CommandHandlerException(string errorMessage) =>
      DataResponse.Response<UpdateUserResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<UpdateUserResponseDTO> Argument_Null_Exception_Command_Handler =>
       DataResponse.Response<UpdateUserResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");
}

#endregion Exception Service

#region Data Service

public class UpdateUserDataService : IRequest<Result<UpdateUserResponseDTO>>
{
    public UpdateUserDataService(Guid? identifier, string firstName, string lastName, string emailId, string mobileNo, UserType UserType)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
        this.EmailId = emailId;
        this.MobileNo = mobileNo;
        this.UserType = UserType;
        this.Identifier = identifier;
    }

    public Guid? Identifier { get; }

    public string? FirstName { get; }

    public string? LastName { get; }

    public string? EmailId { get; }

    public string? MobileNo { get; }

    public UserType UserType { get; }
}

public class UpdateUserDataServiceHandler : IRequestHandler<UpdateUserDataService, Result<UpdateUserResponseDTO>>
{
    private readonly UsersContext usersContext = null;
    private readonly IUserSharedRepository userSharedRepository = null;
    private readonly IMediator mediator = null;

    public UpdateUserDataServiceHandler(UsersContext usersContext, IUserSharedRepository userSharedRepository, IMediator mediator)
    {
        this.usersContext = usersContext;
        this.userSharedRepository = userSharedRepository;
        this.mediator = mediator;
    }

    private void Map(UpdateUserDataService request, Tuser tuser)
    {
        tuser.FirstName = (request.FirstName != tuser.FirstName) ? request.FirstName : tuser.FirstName;
        tuser.LastName = (request.LastName != tuser.LastName) ? request.LastName : tuser.LastName;
        tuser.EmailId = (request.EmailId != tuser.EmailId) ? request.EmailId : tuser.EmailId;
        tuser.MobileNo = (request.MobileNo != tuser.MobileNo) ? request.MobileNo : tuser.MobileNo;
        tuser.ModifiedDate = DateTime.Now;
    }

    private async Task UpdateAsync(Tuser tuser)
    {
        this.usersContext.Update<Tuser>(tuser);
        await this.usersContext.SaveChangesAsync();
    }

    private Result<UpdateUserResponseDTO> Response(Guid? identifier)
    {
        return Result.Ok<UpdateUserResponseDTO>(new UpdateUserResponseDTO()
        {
            Identifier = identifier
        });
    }

    public Task OnPublishUserUpdatedDomainEvent(Guid identifier) =>
        this.mediator.Publish(new UserUpdatedDomainEvent(identifier));

    async Task<Result<UpdateUserResponseDTO>> IRequestHandler<UpdateUserDataService, Result<UpdateUserResponseDTO>>.Handle(UpdateUserDataService request, CancellationToken cancellationToken)
    {
        try
        {
            // Check Request is Empty Or Not
            if (request is null)
                return UpdateUserExceptionHandler.Argument_Null_Exception_Data_Service;

            // Get User Data By Identifier.
            var getUserResult = await this.userSharedRepository.GetUserByIdentifierAsync(request.Identifier);

            if (getUserResult.IsFailed)
                return UpdateUserExceptionHandler.Get_User_Result_Exception_Data_Service(Convert.ToInt32(getUserResult.Errors[0].Metadata["StatusCode"]), getUserResult.Errors[0].Message);

            Tuser tuser = getUserResult.Value.User!;

            // Map
            this.Map(request, tuser);

            //Update
            await this.UpdateAsync(tuser);

            // Event
            BackgroundJob.Enqueue(() => this.OnPublishUserUpdatedDomainEvent(tuser.Identifier));

            // Response
            return this.Response(tuser.Identifier);
        }
        catch (Exception ex)
        {
            return UpdateUserExceptionHandler.DbException(ex.Message, ex.StackTrace);
        }
    }
}

#endregion Data Service

#region Command Service Handler

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, DataResponse<UpdateUserResponseDTO>>
{
    private readonly IMediator mediator = null;

    public UpdateUserCommandHandler(IMediator mediator)
    {
        this.mediator = mediator;
    }

    async Task<DataResponse<UpdateUserResponseDTO>> IRequestHandler<UpdateUserCommand, DataResponse<UpdateUserResponseDTO>>.Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is not null)
                return UpdateUserExceptionHandler.Argument_Null_Exception_Command_Handler;

            var updateUserResult = await this.mediator.Send(new UpdateUserDataService(request.Identifier, request.FirstName, request.LastName, request.EmailId, request.MobileNo, request.UserType));

            if (updateUserResult.IsFailed)
                return DataResponse.Response<UpdateUserResponseDTO>(false, Convert.ToInt32(updateUserResult.Errors[0].Metadata["StatusCode"]),
                    null, updateUserResult.Errors[0].Message);

            return DataResponse.Response<UpdateUserResponseDTO>(true, Convert.ToInt32(HttpStatusCode.OK), updateUserResult.Value, "User Details Updated");
        }
        catch (Exception ex)
        {
            return UpdateUserExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

#endregion Command Service Handler

#region Event Service

public class UserUpdatedDomainEvent : INotification
{
    public UserUpdatedDomainEvent(Guid? identifier)
    {
        this.Identifier = identifier;
    }

    public Guid? Identifier { get; }
}

public class UserUpdatedDomainEventHandler : INotificationHandler<UserUpdatedDomainEvent>
{
    private readonly IMediator mediator;
    private readonly ILogger<UserUpdatedDomainEventHandler> logger;

    public UserUpdatedDomainEventHandler(IMediator mediator, ILogger<UserUpdatedDomainEventHandler> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    async Task INotificationHandler<UserUpdatedDomainEvent>.Handle(UserUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await this.mediator.Publish(new UserSharedCacheService(notification.Identifier));
        }
        catch (Exception ex)
        {
            logger.LogCritical($" {nameof(UserUpdatedDomainEvent)} - {ex.Message}", ex);
        }
    }
}

#endregion Event Service