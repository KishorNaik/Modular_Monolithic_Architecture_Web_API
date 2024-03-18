namespace Users.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
public class GetUserByIdentifierController : UserBaseController
{
    private readonly IUserProviderService userProvider = null;

    public GetUserByIdentifierController(IMediator mediator, IUserProviderService userProviderService) : base(mediator)
    {
        this.userProvider = userProviderService;
    }

    [HttpGet()]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [Authorize(Policy = ConstantValue.BuyerPolicy)]
    [Authorize(Policy = ConstantValue.SellerPolicy)]
    [ProducesResponseType<DataResponse<GetOrganizationByIdentifierQuery>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<GetOrganizationByIdentifierQuery>>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<DataResponse<GetOrganizationByIdentifierQuery>>((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get()
    {
        var identifier = this.userProvider.GetUserIdentifier();
        var response = await this.Mediator.Send(new GetUserByIdentifierQuery()
        {
            Identifier = identifier
        });
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Exception Service

public static class GetUserByIdentifierExceptionHandler
{
    public static DataResponse<GetUserByIdentifierResponseDTO> QueryHandlerException(string errorMessage) =>
      DataResponse.Response<GetUserByIdentifierResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<GetUserByIdentifierResponseDTO> Argument_Null_Exception_Command_Handler =>
       DataResponse.Response<GetUserByIdentifierResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<GetUserByIdentifierResponseDTO> User_Not_Found(int statusCode, string errorMessage) =>
        DataResponse.Response<GetUserByIdentifierResponseDTO>(false, statusCode, null, errorMessage);
}

#endregion Exception Service

#region QueryHandler

public class GetUserByIdentifierQueryHandler : IRequestHandler<GetUserByIdentifierQuery, DataResponse<GetUserByIdentifierResponseDTO>>
{
    private readonly IUserSharedRepository userSharedRepository = null;

    private readonly IDistributedCache distributedCache = null;

    public GetUserByIdentifierQueryHandler(IUserSharedRepository userSharedRepository, IDistributedCache distributedCache)
    {
        this.userSharedRepository = userSharedRepository;
        this.distributedCache = distributedCache;
    }

    public Task OnCacheEvent(string cacheKeyName, UserEntityResultSet userEntityResultSet) =>
        SqlCacheHelper.SetCacheAsync(distributedCache, cacheKeyName, ConstantValue.CacheTime, userEntityResultSet);

    private GetUserByIdentifierResponseDTO Map(UserEntityResultSet userEntityResult)
    {
        return new GetUserByIdentifierResponseDTO()
        {
            Identifier = userEntityResult.User.Identifier,
            EmailId = userEntityResult.User.EmailId,
            FirstName = userEntityResult.User.FirstName,
            LastName = userEntityResult.User.LastName,
            MobileNo = userEntityResult.User.MobileNo,
            UserType = userEntityResult.User.UserType
        };
    }

    private async Task<DataResponse<GetUserByIdentifierResponseDTO>> GetUserByIdentiferAsync(Guid? identifier)
    {
        GetUserByIdentifierResponseDTO response = null;

        string cacheKeyName = $"User_{identifier}";

        var userJsonResponse = await SqlCacheHelper.GetCacheAsync(distributedCache, cacheKeyName);

        if (userJsonResponse is null)
        {
            // Get User Data
            var result = await this.userSharedRepository.GetUserByIdentifierAsync(identifier);

            if (result.IsFailed)
                return GetUserByIdentifierExceptionHandler.User_Not_Found(Convert.ToInt32(result.Errors[0].Metadata["StatusCode"]), result.Errors[0].Message);

            // Cache
            BackgroundJob.Enqueue(() => this.OnCacheEvent(cacheKeyName, result.Value));

            // Response
            response = this.Map(result.Value);
        }
        else
        {
            var result = JsonConvert.DeserializeObject<UserEntityResultSet>(userJsonResponse);

            if (result is null)
                return GetUserByIdentifierExceptionHandler.User_Not_Found((int)HttpStatusCode.NotFound, "User Not found");

            // Response
            response = this.Map(result);
        }

        return DataResponse.Response(true, Convert.ToInt32(HttpStatusCode.OK), response, "User found");
    }

    async Task<DataResponse<GetUserByIdentifierResponseDTO>> IRequestHandler<GetUserByIdentifierQuery, DataResponse<GetUserByIdentifierResponseDTO>>.Handle(GetUserByIdentifierQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var isNullRequestResult = Guard.Against.Null(request).ToResult();

            if (isNullRequestResult.IsFailed)
                return GetUserByIdentifierExceptionHandler.Argument_Null_Exception_Command_Handler;

            return await GetUserByIdentiferAsync(request.Identifier);
        }
        catch (Exception ex)
        {
            return GetUserByIdentifierExceptionHandler.QueryHandlerException(ex.Message);
        }
    }
}

#endregion QueryHandler