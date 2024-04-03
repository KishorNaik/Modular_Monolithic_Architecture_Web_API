using Microsoft.AspNetCore.Http;
using Users.Contracts.Shared.Services;
using Utility.Shared.Cache;

namespace Organization.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/organizations")]
[Tags("Organizations")]
public class GetOrganizationByIdentifierController : OrganizationBaseController
{
    private readonly IUserProviderService userProviderService;

    public GetOrganizationByIdentifierController(IMediator mediator, IUserProviderService userProviderService) : base(mediator)
    {
        this.userProviderService = userProviderService;
    }

    [HttpGet("{identifier}")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [Authorize(Policy = ConstantValue.BuyerSellerPolicy)]
    [ProducesResponseType<DataResponse<GetOrganizationByIdentifierQuery>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<GetOrganizationByIdentifierQuery>>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<DataResponse<GetOrganizationByIdentifierQuery>>((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetOrganizationDataByIdentifier([FromRoute] GetOrganizationByIdentifierQuery getOrganizationByIdentifierQuery, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(getOrganizationByIdentifierQuery, cancellationToken);

        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

public class GetOrganizationByIdentifierValidation : AbstractValidator<GetOrganizationByIdentifierQuery>
{
    private readonly IActionContextAccessor actionContextAccessor = null;

    public GetOrganizationByIdentifierValidation(IActionContextAccessor actionContextAccessor)
    {
        this.actionContextAccessor = actionContextAccessor;

        IdentifierValidation();
    }

    private void IdentifierValidation()
    {
        RuleFor(x => x.Identifier)
            .Must((context, id, propertyValidatorContext) =>
            {
                var identifier = (string)actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("identifier");

                if (identifier is null)
                    return false;

                return true;
            })
            .WithMessage("id should not be empty")
            .WithErrorCode("Identifier")
            .Must((context, id, propertyValidatorContext) =>
            {
                var identifier = (string)actionContextAccessor.ActionContext.RouteData.Values.GetValueOrDefault("identifier");

                Guid identifierGuid;
                var flag = Guid.TryParse(identifier, out identifierGuid);

                return flag;
            })
            .WithMessage("id should be guid")
            .WithErrorCode("Identifier");
    }
}

#endregion Validation Service

#region Exception Services

public static class GetOrganizationExceptionHandler
{
    public static DataResponse<GetOrganizationByIdentifierResponseDTO> Argument_Null_Exception_Command_Handler =>
       DataResponse.Response<GetOrganizationByIdentifierResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<GetOrganizationByIdentifierResponseDTO> QueryHandlerException(string errorMessage) =>
       DataResponse.Response<GetOrganizationByIdentifierResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);
}

#endregion Exception Services

#region Query Handler Service

public class GetOrganizationByIdentifierQuery : GetOrganizationByIdentifierRequestDTO, IRequest<DataResponse<GetOrganizationByIdentifierResponseDTO>>
{
}

public class GetOrganizationByIdentifierQueryHandler : IRequestHandler<GetOrganizationByIdentifierQuery, DataResponse<GetOrganizationByIdentifierResponseDTO>>
{
    private readonly IOrganizationSharedRepository organizationSharedRepository = null;
    private readonly IDistributedCache distributedCache = null;

    public GetOrganizationByIdentifierQueryHandler(IOrganizationSharedRepository organizationSharedRepository, IDistributedCache distributedCache)
    {
        this.organizationSharedRepository = organizationSharedRepository;
        this.distributedCache = distributedCache;
    }

    private GetOrganizationByIdentifierResponseDTO Map(TOrganization organization)
    {
        return new GetOrganizationByIdentifierResponseDTO()
        {
            Id = organization.Id,
            Identifier = organization.Identifier,
            Name = organization.Name
        };
    }

    private async Task<DataResponse<GetOrganizationByIdentifierResponseDTO>> GetOrgDataByIdentifierAsync(Guid? identifier)
    {
        GetOrganizationByIdentifierResponseDTO response = null;

        string cacheKeyName = $"Org_{identifier}";

        var orgJsonResponse = await distributedCache.GetStringAsync(cacheKeyName);

        if (orgJsonResponse is null)
        {
            // Get Org Data by Identifier
            var result = await organizationSharedRepository.GetOrgByIdentifierAsync(identifier);

            if (result.IsFailed)
                return DataResponse.Response<GetOrganizationByIdentifierResponseDTO>(false, Convert.ToInt32(result.Errors[0].Metadata["StatusCode"]), null, result.Errors[0].Message);

            // Cache
            BackgroundJob.Enqueue(() => this.OnCacheEvent(cacheKeyName, result.Value));

            // Map : from TOrganization to DTO
            response = this.Map(result.Value);
        }
        else
        {
            var result = JsonConvert.DeserializeObject<TOrganization>(orgJsonResponse);

            if (result is null)
                return DataResponse.Response<GetOrganizationByIdentifierResponseDTO>(false, Convert.ToInt32(HttpStatusCode.NotFound), null, "Organization not found");

            // Map : from TOrganization to DTO
            response = Map(result);
        }

        return DataResponse.Response(true, Convert.ToInt32(HttpStatusCode.OK), response, "Organization found");
    }

    public Task OnCacheEvent(string cacheKeyName, TOrganization organization) => SqlCacheHelper.SetCacheAsync(distributedCache, cacheKeyName, ConstantValue.CacheTime, organization);

    async Task<DataResponse<GetOrganizationByIdentifierResponseDTO>> IRequestHandler<GetOrganizationByIdentifierQuery, DataResponse<GetOrganizationByIdentifierResponseDTO>>.Handle(GetOrganizationByIdentifierQuery request, CancellationToken cancellationToken)

    {
        try
        {
            // Check request is empty or not
            if (request is null)
                return GetOrganizationExceptionHandler.Argument_Null_Exception_Command_Handler;

            return await GetOrgDataByIdentifierAsync(request.Identifier);
        }
        catch (Exception ex)
        {
            return GetOrganizationExceptionHandler.QueryHandlerException(ex.Message);
        }
    }
}

#endregion Query Handler Service

#region Event Service

public class GetOrganizationByIdentifierIntegrationServiceHandler : IRequestHandler<GetOrganizationByIdentifierIntegrationService, DataResponse<GetOrganizationByIdentifierResponseDTO>>
{
    private readonly IMediator mediator = null;

    public GetOrganizationByIdentifierIntegrationServiceHandler(IMediator mediator)
    {
        this.mediator = mediator;
    }

    async Task<DataResponse<GetOrganizationByIdentifierResponseDTO>> IRequestHandler<GetOrganizationByIdentifierIntegrationService, DataResponse<GetOrganizationByIdentifierResponseDTO>>.Handle(GetOrganizationByIdentifierIntegrationService request, CancellationToken cancellationToken)
    {
        return await this.mediator.Send(new GetOrganizationByIdentifierQuery()
        {
            Identifier = request.Identifier
        }, cancellationToken);
    }
}

#endregion Event Service