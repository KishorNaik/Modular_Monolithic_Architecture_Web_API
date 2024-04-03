using Users.Contracts.Shared.Enums;

namespace Users.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public class AddUserController : UserBaseController
{
    public AddUserController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<AddUserResponseDTO>>((int)HttpStatusCode.Created)]
    [ProducesResponseType<DataResponse<AddUserResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> AddAsync([FromBody] AddUserCommand addUserCommand, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(addUserCommand, cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

public class AddUserValidation : AbstractValidator<AddUserCommand>
{
    public AddUserValidation()
    {
        FirstNameValidation();
        LastNameValidation();
        EmailIdValidation();
        MobileValidation();
        UserTypeValidation();
        PasswordValidation();
        OrgIdValidation();
    }

    private void FirstNameValidation()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithErrorCode("FirstName")
            .Length(0, 50).WithMessage("First Name must be less than 50 charcters").WithErrorCode("FirstName")
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

    private void PasswordValidation()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("Password")
            .MinimumLength(8).WithErrorCode("Password")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Password must not contain HTML tags.").WithErrorCode("Password")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Password must not contain JavaScript.").WithErrorCode("Password");
    }

    private void OrgIdValidation()
    {
        RuleFor(x => x.OrgId)
            .Empty().WithErrorCode("OrgId")
            .When(x => !x.OrgId.HasValue)
            .NotEmpty().WithErrorCode("OrgId")
            .When(x => x.OrgId.HasValue)
            .Must(x =>
            {
                Guid identifierGuid;
                var flag = Guid.TryParse(x.ToString(), out identifierGuid);

                return flag;
            })
            .When(x => x.OrgId.HasValue)
            .WithMessage("Invalid Org id")
            .WithErrorCode("OrgId");
    }
}

#endregion Validation Service

#region Exception Service

public static class AddUserExceptionHandler
{
    public static Result<AddUserResponseDTO> Users_Already_Exists =>
         Result.Fail<AddUserResponseDTO>(new FluentResults.Error("The User is already exists.").WithMetadata("StatusCode", HttpStatusCode.Conflict));

    public static Result<AddUserResponseDTO> DbException(string errorMessage, string stackTrace) =>
      Result.Fail<AddUserResponseDTO>(new FluentResults.Error(errorMessage).WithMetadata("StackTrace", stackTrace).WithMetadata("StatusCode", HttpStatusCode.InternalServerError));

    public static Result<AddUserResponseDTO> Argument_Null_Exception_Data_Service =>
        Result.Fail<AddUserResponseDTO>(new FluentResults.Error("Arguments should not be empty.").WithMetadata("StatusCode", HttpStatusCode.BadRequest));

    public static DataResponse<AddUserResponseDTO> Argument_Null_Exception_Command_Handler =>
        DataResponse.Response<AddUserResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<AddUserResponseDTO> CommandHandlerException(string errorMessage) =>
       DataResponse.Response<AddUserResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<AddUserResponseDTO> Data_Service_Failed(int statusCode, string errorMessage) =>
       DataResponse.Response<AddUserResponseDTO>(false, statusCode, null, errorMessage);

    public static DataResponse<AddUserResponseDTO> Data_Messaging_Failed(int statusCode, string errorMessage) =>
      DataResponse.Response<AddUserResponseDTO>(false, statusCode, null, errorMessage);
}

#endregion Exception Service

#region Data Service

public class AddUserDataService : IRequest<Result<AddUserResponseDTO>>
{
    public AddUserDataService(
        string firstName,
        string lastName,
        string emailId,
        string mobileNo,
        UserType userType,
        string salt,
        string hash,
        Guid? orgId
        )
    {
        FirstName = firstName;
        LastName = lastName;
        EmailId = emailId;
        MobileNo = mobileNo;
        UserType = userType;
        Salt = salt;
        Hash = hash;
        OrgId = orgId;
    }

    public string? FirstName { get; }

    public string? LastName { get; }

    public string? EmailId { get; }

    public string? MobileNo { get; }

    public UserType UserType { get; }

    public string Hash { get; }

    public string Salt { get; }

    public Guid? OrgId { get; }
}

public class AddUserDataServiceHandler : IRequestHandler<AddUserDataService, Result<AddUserResponseDTO>>
{
    private readonly UsersContext usersContext = null;

    public AddUserDataServiceHandler(UsersContext usersContext)
    {
        this.usersContext = usersContext;
    }

    private Tuser MapUser(AddUserDataService addUserDataService)
    {
        return new Tuser()
        {
            Identifier = Guid.NewGuid(),
            FirstName = addUserDataService.FirstName,
            LastName = addUserDataService.LastName,
            EmailId = addUserDataService.EmailId,
            MobileNo = addUserDataService.MobileNo,
            Salt = addUserDataService.Salt,
            Hash = addUserDataService.Hash,
            UserType = (int)addUserDataService.UserType,
            Status = Convert.ToBoolean((int)StatusEnum.Inactive),
            CreatedDate = DateTime.UtcNow,
            IsEmailVerified = Convert.ToBoolean((int)VerifiedEnum.No),
            EmailToken = GenerateEmailVerificationToken
        };
    }

    private async Task SaveUser(Tuser user, CancellationToken cancellationToken)
    {
        await usersContext.Tusers.AddAsync(user, cancellationToken);
        await usersContext.SaveChangesAsync(cancellationToken);
    }

    private TusersOrganization MapUserOrganization(Guid userId, Guid orgId)
    {
        return new TusersOrganization()
        {
            UserId = userId,
            OrgId = orgId,
            CreatedDate = DateTime.UtcNow
        };
    }

    private async Task SaveUserOrganisation(TusersOrganization userOrg, CancellationToken cancellationToken)
    {
        await usersContext.TusersOrganizations.AddAsync(userOrg, cancellationToken);
        await usersContext.SaveChangesAsync(cancellationToken);
    }

    private bool IsOrgIdExists(Guid? orgId) => orgId is not null ? true : false;

    private Guid? GenerateEmailVerificationToken => Guid.NewGuid();

    private Result<AddUserResponseDTO> Response(Tuser tuser)
    {
        return Result.Ok(new AddUserResponseDTO()
        {
            Identifier = tuser.Identifier,
            EmailToken = tuser.EmailToken
        });
    }

    async Task<Result<AddUserResponseDTO>> IRequestHandler<AddUserDataService, Result<AddUserResponseDTO>>.Handle(AddUserDataService request, CancellationToken cancellationToken)
    {
        try
        {
            // Check Argument
            var checkRequest = Guard.Against.Null(request).ToResult();

            if (checkRequest.IsFailed)
                return AddUserExceptionHandler.Argument_Null_Exception_Data_Service;

            // Map
            Tuser tuser = MapUser(request);

            using var transaction = await usersContext.Database.BeginTransactionAsync(cancellationToken);

            // Save User
            await SaveUser(tuser, cancellationToken).ConfigureAwait(false);

            // Check if Org ID exists
            if (IsOrgIdExists(request.OrgId))
            {
                TusersOrganization tusersOrganization = MapUserOrganization(tuser.Identifier, (Guid)request.OrgId);
                await SaveUserOrganisation(tusersOrganization, cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync();

            return Response(tuser);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            return AddUserExceptionHandler.Users_Already_Exists;
        }
        catch (Exception ex)
        {
            return AddUserExceptionHandler.DbException(ex.Message, ex.StackTrace);
        }
    }
}

#endregion Data Service

#region Command Service

public class AddUserCommand : AddUserRequestDTO, IRequest<DataResponse<AddUserResponseDTO>>
{
}

public class AddUserCommandHandler : IRequestHandler<AddUserCommand, DataResponse<AddUserResponseDTO>>
{
    private readonly IMediator mediator = null;
    private readonly IGenerateHashPasswordService generateHashPasswordService = null;

    public AddUserCommandHandler(IMediator mediator, IGenerateHashPasswordService generateHashPasswordService)
    {
        this.mediator = mediator;
        this.generateHashPasswordService = generateHashPasswordService;
    }

    private Task<(string salt, string hash)> GenerateHashPasswordAsync(string? password) =>
        this.generateHashPasswordService.GenerateAsync(password!);

    private async Task<DataResponse<AddUserResponseDTO>> AddAsync(AddUserCommand addUserCommand, string salt, string hash, Guid? orgId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddUserDataService(
            addUserCommand.FirstName,
            addUserCommand.LastName,
            addUserCommand.EmailId,
            addUserCommand.MobileNo,
            addUserCommand.UserType,
            salt,
            hash,
            orgId
            ), cancellationToken);

        if (result.IsFailed)
            return AddUserExceptionHandler.Data_Service_Failed(Convert.ToInt32(result.Errors[0].Metadata["StatusCode"]), result.Errors[0].Message);

        return DataResponse.Response(true, (int?)HttpStatusCode.Created, result.Value, "User created successfully");
    }

    private async Task<DataResponse<AddUserResponseDTO>> IsValidOrgIdAsync(Guid? orgId)
    {
        // Get Org data by Messaging queue
        var response = await this.mediator.Send(new GetOrganizationByIdentifierIntegrationService()
        {
            Identifier = orgId
        });

        if (response is not null && response?.Success == false)
            return AddUserExceptionHandler.Data_Messaging_Failed(Convert.ToInt32(response.StatusCode), response.Message);

        return DataResponse.Response<AddUserResponseDTO>(true, (int)HttpStatusCode.OK, null, null);
    }

    public Task OnPublishUserCreatedEvent(AddUserCommand request, Guid? identifier, Guid? emailToken, CancellationToken cancellationToken)
    => mediator.Publish(new UserCreatedDomainEvent(identifier, request.FirstName, request.LastName, request.EmailId, emailToken), cancellationToken);

    async Task<DataResponse<AddUserResponseDTO>> IRequestHandler<AddUserCommand, DataResponse<AddUserResponseDTO>>.Handle(AddUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check Command Object is empty or not
            var checkRequest = Guard.Against.Null(request, nameof(request)).ToResult();

            if (checkRequest.IsFailed)
                return AddUserExceptionHandler.Argument_Null_Exception_Command_Handler;

            // Validate Org Id
            if (request?.OrgId is not null)
            {
                var orgResult = await this.IsValidOrgIdAsync(request.OrgId);

                if (orgResult.Success == false)
                    return orgResult;
            }

            // Generate Hash Password
            var generateHashResult = await this.GenerateHashPasswordAsync(request.Password);

            // Save
            var addUserResult = await this.AddAsync(request, generateHashResult.salt, generateHashResult.hash, request.OrgId, cancellationToken);

            if (addUserResult is not null && addUserResult.Success == true)
            {
                // Publish User Created Event
                // 1. Send Welcome Email
                // 2. Send Email Verification Token
                // 3. Cache Service
                _ = BackgroundJob.Enqueue(() => this.OnPublishUserCreatedEvent(request, addUserResult.Data.Identifier, addUserResult.Data.EmailToken, cancellationToken));
            }

            return addUserResult!;
        }
        catch (Exception ex)
        {
            return AddUserExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

#endregion Command Service

#region Domain Event Service

public class UserCreatedDomainEvent : INotification
{
    public UserCreatedDomainEvent(Guid? identifier, string firstName, string lastName, string emailId, Guid? emailToken)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = emailId;
        Identifier = identifier;
        EmailToken = emailToken;
    }

    public Guid? Identifier { get; }

    public string? FirstName { get; }

    public string? LastName { get; }

    public string? Email { get; }

    public Guid? EmailToken { get; set; }
}

public class UserCreatedDomainEventHandler : INotificationHandler<UserCreatedDomainEvent>
{
    private readonly IMediator mediator = null;
    private readonly ILogger<UserCreatedDomainEventHandler> logger;

    public UserCreatedDomainEventHandler(IMediator mediator, ILogger<UserCreatedDomainEventHandler> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    private Task OnPublishCacheDomainEvent(Guid? identifier) =>
        mediator.Publish(new UserSharedCacheService(identifier));

    private Task OnPublishWelcomeUserEmailIntegrationEvent(string emailId, string firstName, string lastName) =>
        mediator.Publish(new WelcomeUserEmailIntegrationEvent(emailId, firstName, lastName));

    private Task OnPublishUserEmailVerificationIntegrationEvent(string emailId, Guid? emailToken)
        => mediator.Publish(new UserEmailVerificationIntegrationEvent(emailId, emailToken));

    async Task INotificationHandler<UserCreatedDomainEvent>.Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Cache Event
            await OnPublishCacheDomainEvent(notification.Identifier);

            //Welcome Event
            await OnPublishWelcomeUserEmailIntegrationEvent(notification.Email, notification.FirstName, notification.LastName);

            //Email Verification Event
            await OnPublishUserEmailVerificationIntegrationEvent(notification.Email, notification.EmailToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{nameof(UserCreatedDomainEventHandler)} => Message: {ex.Message}");
        }
    }
}

#endregion Domain Event Service