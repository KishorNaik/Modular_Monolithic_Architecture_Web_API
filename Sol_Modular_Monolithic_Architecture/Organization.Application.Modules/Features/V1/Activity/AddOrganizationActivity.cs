using Microsoft.AspNetCore.Http;
using Models.Shared.Response;

namespace Organization.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/organizations")]
[Tags("Organizations")]
public class AddOrganizationController : OrganizationBaseController
{
    public AddOrganizationController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<AddOrganizationResponseDTO>>((int)HttpStatusCode.Created)]
    [ProducesResponseType<DataResponse<AddOrganizationResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> AddAsync([FromBody] AddOrganizationCommand addOrganizationCommand, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(addOrganizationCommand, cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

public class AddOrganizationValidation : AbstractValidator<AddOrganizationCommand>
{
    public AddOrganizationValidation()
    {
        NameValidation();
    }

    private void NameValidation()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode("Name")
            .Length(0, 100).WithMessage("Name must be less than 100 characters.").WithErrorCode("Name")
            .Matches(new Regex(@"^[a-zA-Z0-9 ]*$")).WithMessage("Name must not contain special characters.").WithErrorCode("Name")
            .Must(name => !Regex.IsMatch(name, "<.*>|<.*|.*>")).WithMessage("Name must not contain HTML tags.").WithErrorCode("Name")
            .Must(name => !Regex.IsMatch(name, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>")).WithMessage("Name must not contain JavaScript.").WithErrorCode("Name");
    }
}

#endregion Validation Service

#region Exceptions Service

public static class AddOrganizationExceptionHandler
{
    public static Result<AddOrganizationResponseDTO> Argument_Null_Exception_Data_Service =>
        Result.Fail<AddOrganizationResponseDTO>(new FluentResults.Error("Arguments should not be empty.").WithMetadata("StatusCode", HttpStatusCode.BadRequest));

    public static Result<AddOrganizationResponseDTO> Organisation_Already_Exists =>
         Result.Fail<AddOrganizationResponseDTO>(new FluentResults.Error("The organization is already exists.").WithMetadata("StatusCode", HttpStatusCode.Conflict));

    public static Result<AddOrganizationResponseDTO> DbException(string errorMessage, string stackTrace) =>
       Result.Fail<AddOrganizationResponseDTO>(new FluentResults.Error(errorMessage).WithMetadata("StackTrace", stackTrace).WithMetadata("StatusCode", HttpStatusCode.InternalServerError));

    public static DataResponse<AddOrganizationResponseDTO> Argument_Null_Exception_Command_Handler =>
        DataResponse.Response<AddOrganizationResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<AddOrganizationResponseDTO> CommandHandlerException(string errorMessage) =>
        DataResponse.Response<AddOrganizationResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<AddOrganizationResponseDTO> Data_Service_Failed(int statusCode, string errorMessage) =>
        DataResponse.Response<AddOrganizationResponseDTO>(false, statusCode, null, errorMessage);
}

#endregion Exceptions Service

#region Data Service

public class AddOrganizationDataService : IRequest<Result<AddOrganizationResponseDTO>>
{
    public AddOrganizationDataService(string? name)
    {
        Name = name;
    }

    public string? Name { get; }
}

public class AddOrganizationDataServiceHandler : IRequestHandler<AddOrganizationDataService, Result<AddOrganizationResponseDTO>>
{
    private readonly OrganizationContext organizationContext;

    public AddOrganizationDataServiceHandler(OrganizationContext organizationContext)
    {
        this.organizationContext = organizationContext;
    }

    private TOrganization Map(AddOrganizationDataService? addOrganizationDataService)
    {
        StatusEnum status = StatusEnum.Active;

        return new TOrganization()
        {
            Identifier = Guid.NewGuid(),
            Name = addOrganizationDataService.Name,
            Status = status == StatusEnum.Active,
            CreatedDate = DateTime.UtcNow
        };
    }

    private async Task Save(TOrganization organization, CancellationToken cancellationToken)
    {
        await organizationContext.TOrganizations.AddAsync(organization, cancellationToken);
        await organizationContext.SaveChangesAsync(cancellationToken);
    }

    private Result<AddOrganizationResponseDTO> Response(TOrganization organization)
    {
        return Result.Ok(new AddOrganizationResponseDTO()
        {
            Identifier = organization.Identifier
        });
    }

    async Task<Result<AddOrganizationResponseDTO>> IRequestHandler<AddOrganizationDataService, Result<AddOrganizationResponseDTO>>.Handle(AddOrganizationDataService request, CancellationToken cancellationToken)
    {
        try
        {
            // Check request is empty or not
            if (request is null)
                return AddOrganizationExceptionHandler.Argument_Null_Exception_Data_Service;

            // Map
            TOrganization organization = Map(request);

            // Save
            await Save(organization, cancellationToken).ConfigureAwait(false); ;

            // Response
            return Response(organization);
        }
        catch (Exception ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            // Organizaation already exists
            return AddOrganizationExceptionHandler.Organisation_Already_Exists;
        }
        catch (Exception ex)
        {
            return AddOrganizationExceptionHandler.DbException(ex.Message, ex.StackTrace);
        }
    }
}

#endregion Data Service

#region Command Service

public class AddOrganizationCommand : AddOrganizationRequestDTO, IRequest<DataResponse<AddOrganizationResponseDTO>>
{
}

public class AddOrganizationCommandHandler : IRequestHandler<AddOrganizationCommand, DataResponse<AddOrganizationResponseDTO>>
{
    private readonly IMediator mediator;

    public AddOrganizationCommandHandler(IMediator mediator)
    {
        this.mediator = mediator;
    }

    private async Task<DataResponse<AddOrganizationResponseDTO>> AddAsync(AddOrganizationCommand addOrganizationCommand, CancellationToken cancellationToken)
    {
        var addOrgDataServiceResult = await mediator.Send(new AddOrganizationDataService(addOrganizationCommand.Name), cancellationToken);

        if (addOrgDataServiceResult.IsFailed)
            return AddOrganizationExceptionHandler.Data_Service_Failed(Convert.ToInt32(addOrgDataServiceResult.Errors[0].Metadata["StatusCode"]), addOrgDataServiceResult.Errors[0].Message);

        return DataResponse.Response(true, (int?)HttpStatusCode.Created, addOrgDataServiceResult.Value, "Organization created successfully");
    }

    public async Task PublishOrganizationCreatedEvent(Guid? identifier) => await mediator.Publish(new OrganizationCreatedDomainEvent(identifier));

    async Task<DataResponse<AddOrganizationResponseDTO>> IRequestHandler<AddOrganizationCommand, DataResponse<AddOrganizationResponseDTO>>.Handle(AddOrganizationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check Command object is empty or not
            if (request is null)
                return AddOrganizationExceptionHandler.Argument_Null_Exception_Command_Handler;

            // Add Org Data
            var response = await AddAsync(request, cancellationToken);

            if (response is not null && response.Success == true)
            {
                // Domain Event
                _ = BackgroundJob.Enqueue(() => this.PublishOrganizationCreatedEvent(response.Data.Identifier));
            }

            return response!;
        }
        catch (Exception ex)
        {
            return AddOrganizationExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

#endregion Command Service

#region Domain Event

public class OrganizationCreatedDomainEvent : INotification
{
    public OrganizationCreatedDomainEvent(Guid? identifier)
    {
        Identifier = identifier;
    }

    public Guid? Identifier { get; }
}

public class OrganizationCreatedDomainEventHandler : INotificationHandler<OrganizationCreatedDomainEvent>
{
    private readonly IMediator mediator;
    private readonly ILogger<OrganizationCreatedDomainEventHandler> logger;

    public OrganizationCreatedDomainEventHandler(IMediator mediator, ILogger<OrganizationCreatedDomainEventHandler> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    async Task INotificationHandler<OrganizationCreatedDomainEvent>.Handle(OrganizationCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Publish Organization Cache service.
            await mediator.Publish(new OrganizationSharedCacheService(notification.Identifier), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{nameof(OrganizationCreatedDomainEventHandler)} => Message: {ex.Message}");
        }
    }
}

#endregion Domain Event