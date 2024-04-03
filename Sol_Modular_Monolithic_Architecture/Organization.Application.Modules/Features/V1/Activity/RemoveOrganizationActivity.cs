using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Users.Contracts.Features;

namespace Organization.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/organizations")]
[Tags("Organizations")]
public class RemoveOrganizationController : OrganizationBaseController
{
    public RemoveOrganizationController(IMediator mediator) : base(mediator)
    {
    }

    [HttpDelete()]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [Authorize]
    [ProducesResponseType<DataResponse<AddUserResponseDTO>>((int)HttpStatusCode.Created)]
    [ProducesResponseType<DataResponse<AddUserResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> RemoveAsync([FromRoute] RemoveOrganizationCommand removeOrganizationCommand, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(removeOrganizationCommand, cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

public class RemoveOrganizationValidation : AbstractValidator<RemoveOrganizationCommand>
{
    private readonly IActionContextAccessor actionContextAccessor;

    public RemoveOrganizationValidation(IActionContextAccessor actionContextAccessor)
    {
        this.actionContextAccessor = actionContextAccessor;
        this.IdentifierValidation();
    }

    private void IdentifierValidation()
    {
        RuleFor(x => x.Identifier)
            .Must((context, id, propertyValidatorContext) =>
            {
                var identifier = (string)actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("Identifier");

                if (identifier is null)
                    return false;

                return true;
            })
            .WithMessage("id should not be empty")
            .WithErrorCode("Identifier")
            .Must((context, id, propertyValidatorContext) =>
            {
                var identifier = (string)actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("Identifier");

                Guid identifierGuid;
                var flag = Guid.TryParse(identifier, out identifierGuid);

                return flag;
            })
            .WithMessage("id should be guid")
            .WithErrorCode("Identifier");
    }
}

#endregion Validation Service

#region Exception Service

public static class RemoveOrganizationExceptionHandler
{
    public static DataResponse<RemoveOrganizationResponseDTO> CommandHandlerException(string errorMessage) =>
     DataResponse.Response<RemoveOrganizationResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<RemoveOrganizationResponseDTO> Argument_Null_Exception_Command_Service =>
        DataResponse.Response<RemoveOrganizationResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<RemoveOrganizationResponseDTO> Not_Found_Org(int statusCode, string errorMessage) =>
     DataResponse.Response<RemoveOrganizationResponseDTO>(false, statusCode, null, errorMessage);

    public static DataResponse<RemoveOrganizationResponseDTO> Org_Failed_To_Update_Status(int statusCode, string errorMessage) =>
     DataResponse.Response<RemoveOrganizationResponseDTO>(false, statusCode, null, errorMessage);
}

#endregion Exception Service

#region Command Service

public class RemoveOrganizationCommand : RemoveOrganizationRequestDTO, IRequest<DataResponse<RemoveOrganizationResponseDTO>>
{
}

public class RemoveOrganizationCommandHandler : IRequestHandler<RemoveOrganizationCommand, DataResponse<RemoveOrganizationResponseDTO>>
{
    private readonly OrganizationContext organizationContext = null;
    private readonly IOrganizationSharedRepository organizationSharedRepository = null;
    private readonly IMediator mediator = null;

    public RemoveOrganizationCommandHandler(OrganizationContext organizationContext, IOrganizationSharedRepository organizationSharedRepository, IMediator mediator)
    {
        this.organizationContext = organizationContext;
        this.organizationSharedRepository = organizationSharedRepository;
        this.mediator = mediator;
    }

    private void Map(TOrganization organization)
    {
        //StatusEnum status = StatusEnum.Inactive;
        //organization.Status = status == StatusEnum.Inactive;
        organization.Status = Convert.ToBoolean((int)StatusEnum.Inactive);
    }

    private async Task<Result> DeleteAsync(TOrganization organization, CancellationToken cancellationToken)
    {
        this.organizationContext.Update<TOrganization>(organization);
        var flag = await this.organizationContext.SaveChangesAsync(cancellationToken);

        if (flag <= 0)
            return Result.Fail(new FluentResults.Error("Failed to remove Organization").WithMetadata("StatusCode", HttpStatusCode.InternalServerError));

        return Result.Ok();
    }

    private DataResponse<RemoveOrganizationResponseDTO> Response()
    {
        var removeOrgResponse = new RemoveOrganizationResponseDTO()
        {
            UpdatedTime = DateTime.UtcNow,
        };

        return DataResponse.Response(true, Convert.ToInt32(HttpStatusCode.OK), removeOrgResponse, "Org Removed succesfully");
    }

    public Task OnPublishOrganizationRemovedDomainEvent(Guid? identifier) =>
        this.mediator.Publish(new OrganizationCreatedDomainEvent(identifier));

    async Task<DataResponse<RemoveOrganizationResponseDTO>> IRequestHandler<RemoveOrganizationCommand, DataResponse<RemoveOrganizationResponseDTO>>.Handle(RemoveOrganizationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check Request is Empty Or not
            if (request is not null)
                return RemoveOrganizationExceptionHandler.Argument_Null_Exception_Command_Service;

            // Get org Data by Identifier
            var getOrgByIdentifierResult = await this.organizationSharedRepository.GetOrgByIdentifierAsync(request.Identifier);

            if (getOrgByIdentifierResult.IsFailed)
                return RemoveOrganizationExceptionHandler.Not_Found_Org(Convert.ToInt32(getOrgByIdentifierResult.Errors[0].Metadata["StatusCode"]), getOrgByIdentifierResult.Errors[0].Message);

            // Map
            TOrganization organization = getOrgByIdentifierResult.Value;
            this.Map(organization);

            // Delete
            var deleteOrgResult = await this.DeleteAsync(organization, cancellationToken);

            if (deleteOrgResult.IsFailed)
                return RemoveOrganizationExceptionHandler.Org_Failed_To_Update_Status(Convert.ToInt32(getOrgByIdentifierResult.Errors[0].Metadata["StatusCode"]), getOrgByIdentifierResult.Errors[0].Message);

            // Update Cache
            BackgroundJob.Enqueue(() => this.OnPublishOrganizationRemovedDomainEvent(request.Identifier));

            return this.Response();
        }
        catch (Exception ex)
        {
            return RemoveOrganizationExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

#endregion Command Service

#region Event Service

public class RemoveOrganizationIntegrationServiceHandler : IRequestHandler<RemoveOrganizationIntegrationService, DataResponse<RemoveOrganizationResponseDTO>>
{
    private readonly IMediator mediator = null;

    public RemoveOrganizationIntegrationServiceHandler(IMediator mediator)
    {
        this.mediator = mediator;
    }

    async Task<DataResponse<RemoveOrganizationResponseDTO>> IRequestHandler<RemoveOrganizationIntegrationService, DataResponse<RemoveOrganizationResponseDTO>>.Handle(RemoveOrganizationIntegrationService request, CancellationToken cancellationToken)
    {
        return await this.mediator.Send(new RemoveOrganizationCommand()
        {
            Identifier = request.Identifier,
        }, cancellationToken);
    }
}

public class RemoveOrganizationRollBackIntegrationServiceHandler : IRequestHandler<RemoveOrganizationCommand, DataResponse<RemoveOrganizationResponseDTO>>
{
    private readonly OrganizationContext organizationContext = null;
    private readonly IOrganizationSharedRepository organizationSharedRepository = null;
    private readonly IMediator mediator = null;

    public RemoveOrganizationRollBackIntegrationServiceHandler(OrganizationContext organizationContext, IOrganizationSharedRepository organizationSharedRepository, IMediator mediator)
    {
        this.organizationContext = organizationContext;
        this.organizationSharedRepository = organizationSharedRepository;
        this.mediator = mediator;
    }

    private void Map(TOrganization organization)
    {
        //StatusEnum status = StatusEnum.Inactive;
        //organization.Status = status == StatusEnum.Inactive;
        organization.Status = Convert.ToBoolean((int)StatusEnum.Active);
    }

    private async Task<Result> DeleteAsync(TOrganization organization, CancellationToken cancellationToken)
    {
        this.organizationContext.Update<TOrganization>(organization);
        var flag = await this.organizationContext.SaveChangesAsync(cancellationToken);

        if (flag <= 0)
            return Result.Fail(new FluentResults.Error("Failed to remove Organization").WithMetadata("StatusCode", HttpStatusCode.InternalServerError));

        return Result.Ok();
    }

    private DataResponse<RemoveOrganizationResponseDTO> Response()
    {
        var removeOrgResponse = new RemoveOrganizationResponseDTO()
        {
            UpdatedTime = DateTime.UtcNow,
        };

        return DataResponse.Response(true, Convert.ToInt32(HttpStatusCode.OK), removeOrgResponse, "Org Removed succesfully");
    }

    public Task OnPublishOrganizationRemovedDomainEvent(Guid? identifier) =>
        this.mediator.Publish(new OrganizationCreatedDomainEvent(identifier));

    async Task<DataResponse<RemoveOrganizationResponseDTO>> IRequestHandler<RemoveOrganizationCommand, DataResponse<RemoveOrganizationResponseDTO>>.Handle(RemoveOrganizationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check Request is Empty Or not
            if (request is not null)
                return RemoveOrganizationExceptionHandler.Argument_Null_Exception_Command_Service;

            // Get org Data by Identifier
            var getOrgByIdentifierResult = await this.organizationSharedRepository.GetOrgByIdentifierAsync(request.Identifier);

            if (getOrgByIdentifierResult.IsFailed)
                return RemoveOrganizationExceptionHandler.Not_Found_Org(Convert.ToInt32(getOrgByIdentifierResult.Errors[0].Metadata["StatusCode"]), getOrgByIdentifierResult.Errors[0].Message);

            // Map
            TOrganization organization = getOrgByIdentifierResult.Value;
            this.Map(organization);

            // Delete
            var deleteOrgResult = await this.DeleteAsync(organization, cancellationToken);

            if (deleteOrgResult.IsFailed)
                return RemoveOrganizationExceptionHandler.Org_Failed_To_Update_Status(Convert.ToInt32(getOrgByIdentifierResult.Errors[0].Metadata["StatusCode"]), getOrgByIdentifierResult.Errors[0].Message);

            // Update Cache
            BackgroundJob.Enqueue(() => this.OnPublishOrganizationRemovedDomainEvent(request.Identifier));

            return this.Response();
        }
        catch (Exception ex)
        {
            return RemoveOrganizationExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

public class OrganizationRemovedDomainEvent : INotification
{
    public OrganizationRemovedDomainEvent(Guid? identifier)
    {
        Identifier = identifier;
    }

    public Guid? Identifier { get; }
}

public class OrganizationRemovedDomainEventHandler : INotificationHandler<OrganizationCreatedDomainEvent>
{
    private readonly IMediator mediator = null;
    private readonly ILogger<OrganizationCreatedDomainEventHandler> logger;

    public OrganizationRemovedDomainEventHandler(IMediator mediator, ILogger<OrganizationCreatedDomainEventHandler> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    async Task INotificationHandler<OrganizationCreatedDomainEvent>.Handle(OrganizationCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await this.mediator.Send(new OrganizationSharedCacheService(notification.Identifier));
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{nameof(OrganizationCreatedDomainEventHandler)} : Message : {ex.Message}");
        }
    }
}

#endregion Event Service