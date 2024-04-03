using Users.Contracts.Features;

namespace Users.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public class GetUsersByFiltersController : UserBaseController
{
    public GetUsersByFiltersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("filters")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [Authorize]
    [ProducesResponseType<DataResponse<GetUsersByFiltersResponseDTO>>((int)HttpStatusCode.OK)]
    [ProducesResponseType<DataResponse<GetUsersByFiltersResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetUsersByFilterAsync([FromQuery] GetUsersByFiltersQuery getUsersByFiltersQuery, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(getUsersByFiltersQuery, cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Validation Service

public class GetUsersByFiltersValidation : AbstractValidator<GetUsersByFiltersQuery>
{
    public GetUsersByFiltersValidation()
    {
        this.EmailIdValidation();
        this.MobileNoValidation();
    }

    private void MobileNoValidation()
    {
        base.RuleFor(x => x.MobileNo)
           .Empty().WithErrorCode("MobileNo")
           .When(x => String.IsNullOrEmpty(x.MobileNo))
           .Must(x => long.TryParse(x, out long _mobileNo) && _mobileNo > 0)
           .WithMessage("Mobile no should be positive number").WithErrorCode("MobileNo")
           .When(x => !String.IsNullOrEmpty(x.MobileNo))
           .Length(10).WithMessage("Mobile no should length be 10").WithErrorCode("MobileNo");
    }

    private void EmailIdValidation()
    {
        base.RuleFor((x) => x.EmailId)
           .Empty().WithErrorCode("EmailId")
           .When(x => String.IsNullOrEmpty(x.EmailId))
           .EmailAddress().WithErrorCode("EmailId");
    }
}

#endregion Validation Service

#region Exception Service

public static class GetUserByFiltersExceptionHandler
{
    public static DataResponse<List<GetUsersByFiltersResponseDTO>> QueryHandlerException(string errorMessage) =>
        DataResponse.Response<List<GetUsersByFiltersResponseDTO>>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<List<GetUsersByFiltersResponseDTO>> Argument_Null_Exception_Query_Handler =>
        DataResponse.Response<List<GetUsersByFiltersResponseDTO>>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<List<GetUsersByFiltersResponseDTO>> No_Record_Found =>
        DataResponse.Response<List<GetUsersByFiltersResponseDTO>>(false, (int)HttpStatusCode.NotFound, null, "No Record found");
}

#endregion Exception Service

#region Query Service Handler

public class GetUsersByFiltersQuery : GetUsersByFiltersRequestDTO, IRequest<DataResponse<List<GetUsersByFiltersResponseDTO>>>
{
}

public class GetUsersByFiltersQueryHandler : IRequestHandler<GetUsersByFiltersQuery, DataResponse<List<GetUsersByFiltersResponseDTO>>>
{
    private readonly UsersContext usersContext = null;

    public GetUsersByFiltersQueryHandler(UsersContext usersContext)
    {
        this.usersContext = usersContext;
    }

    private Result<IQueryable<Tuser>> GetUsers()
    {
        var userResults = this.usersContext.Tusers.AsNoTracking().AsParallel().AsSequential().AsQueryable();

        if (!userResults.Any())
            return Result.Fail(new FluentResults.Error("No Record Found").WithMetadata("StatusCode", HttpStatusCode.NotFound));

        return Result.Ok<IQueryable<Tuser>>(userResults);
    }

    private IQueryable<Tuser> EmailFilter(string emailid, IQueryable<Tuser> query)
    {
        if (emailid is not null)
            return query.Where(e => e.EmailId == emailid);

        return query;
    }

    private IQueryable<Tuser> MobileFilter(string mobileNo, IQueryable<Tuser> query)
    {
        if (mobileNo is not null)
            return query.Where(e => e.MobileNo == mobileNo);

        return query;
    }

    private async Task<DataResponse<List<GetUsersByFiltersResponseDTO>>> ResponseAsync(IQueryable<Tuser> query)
    {
        var results = await query.Select(e => new GetUsersByFiltersResponseDTO()
        {
            Identifier = e.Identifier,
            FirstName = e.FirstName,
            LastName = e.LastName,
            EmailId = e.EmailId,
            MobileNo = e.MobileNo,
            UserType = (UserType)Enum.Parse(typeof(UserType), e.UserType.ToString())
        }).ToListAsync();

        if (results is null)
            return GetUserByFiltersExceptionHandler.No_Record_Found;

        return DataResponse.Response(true, Convert.ToInt32(HttpStatusCode.OK), results, "Record Found");
    }

    async Task<DataResponse<List<GetUsersByFiltersResponseDTO>>> IRequestHandler<GetUsersByFiltersQuery, DataResponse<List<GetUsersByFiltersResponseDTO>>>.Handle(GetUsersByFiltersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Check request is empty
            if (request is not null)
                return GetUserByFiltersExceptionHandler.Argument_Null_Exception_Query_Handler;

            // Get Users
            var getUserResult = this.GetUsers();

            if (getUserResult.IsFailed)
                return GetUserByFiltersExceptionHandler.No_Record_Found;

            IQueryable<Tuser> query = getUserResult.Value;

            // Email Filter
            query = this.EmailFilter(request.EmailId, query);

            // Mobile Filter
            query = this.MobileFilter(request.MobileNo, query);

            // Response
            return await ResponseAsync(query);
        }
        catch (Exception ex)
        {
            return GetUserByFiltersExceptionHandler.QueryHandlerException(ex.Message);
        }
    }
}

#endregion Query Service Handler