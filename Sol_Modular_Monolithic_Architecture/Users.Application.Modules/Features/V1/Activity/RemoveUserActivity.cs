using Microsoft.EntityFrameworkCore.Storage;
using Users.Application.Modules.Shared.Repository;
using Users.Contracts.Shared.Services;
using Utility.Shared.Saga;

namespace Users.Application.Modules.Features.V1.Activity;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public class RemoveUserController : UserBaseController
{
    private readonly IUserProviderService userProviderService = null;

    public RemoveUserController(IMediator mediator, IUserProviderService userProviderService) : base(mediator)
    {
        this.userProviderService = userProviderService;
    }

    [HttpDelete()]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [Authorize]
    [ProducesResponseType<DataResponse<AddUserResponseDTO>>((int)HttpStatusCode.Created)]
    [ProducesResponseType<DataResponse<AddUserResponseDTO>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> RemoveAsync(RemoveUserCommand removeUserCommand, CancellationToken cancellationToken)
    {
        removeUserCommand.Identifier = this.userProviderService.GetUserIdentifier();
        var response = await Mediator.Send(removeUserCommand, cancellationToken);
        return base.StatusCode(Convert.ToInt32(response.StatusCode), response);
    }
}

#endregion Controller

#region Exception Service

public static class RemoveUserExceptionHandler
{
    public static DataResponse<RemoveUserResponseDTO> CommandHandlerException(string errorMessage) =>
     DataResponse.Response<RemoveUserResponseDTO>(false, (int?)HttpStatusCode.InternalServerError, null, errorMessage);

    public static DataResponse<RemoveUserResponseDTO> Argument_Null_Exception_Command_Service =>
        DataResponse.Response<RemoveUserResponseDTO>(false, (int?)HttpStatusCode.BadRequest, null, "Arguments should not be empty");

    public static DataResponse<RemoveUserResponseDTO> Not_Found_User(int statusCode, string errorMessage) =>
         DataResponse.Response<RemoveUserResponseDTO>(false, statusCode, null, errorMessage);
}

#endregion Exception Service

#region Command Service

public class RemoveUserCommand : RemoveUserRequestDTO, IRequest<DataResponse<RemoveUserResponseDTO>>
{
}

public class RemoveUserCommandHandler : IRequestHandler<RemoveUserCommand, DataResponse<RemoveUserResponseDTO>>
{
    private readonly UsersContext usersContext;
    private readonly IUserSharedRepository userSharedRepository;
    private readonly IMediator mediator;

    public RemoveUserCommandHandler(UsersContext usersContext, IUserSharedRepository userSharedRepository, IMediator mediator)
    {
        this.usersContext = usersContext;
        this.userSharedRepository = userSharedRepository;
        this.mediator = mediator;
    }

    private DataResponse<RemoveUserResponseDTO> Response()
    {
        var response = new RemoveUserResponseDTO()
        {
            UpdatedTime = DateTime.UtcNow,
        };

        return DataResponse.Response<RemoveUserResponseDTO>(true, Convert.ToInt32(HttpStatusCode.OK), response, "User deactivate");
    }

    async Task<DataResponse<RemoveUserResponseDTO>> IRequestHandler<RemoveUserCommand, DataResponse<RemoveUserResponseDTO>>.Handle(RemoveUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check request is null or not
            if (request is null)
                return RemoveUserExceptionHandler.Argument_Null_Exception_Command_Service;

            var getUserByIdentiferResult = await this.userSharedRepository.GetUserByIdentifierAsync(request.Identifier);
            if (getUserByIdentiferResult.IsFailed)
                return RemoveUserExceptionHandler.Not_Found_User(Convert.ToInt32(getUserByIdentiferResult.Errors[0].Metadata["StatusCode"]), getUserByIdentiferResult.Errors[0].Message);

            UserEntityResultSet userEntityResultSet = getUserByIdentiferResult.Value;

            using var transaction = await usersContext.Database.BeginTransactionAsync(cancellationToken);

            var isSagaSuccess = await this.mediator.Send(new RemoveUserSagaOrchestratorService(transaction, userEntityResultSet));

            if (isSagaSuccess == false)
                return DataResponse.Response<RemoveUserResponseDTO>(false, Convert.ToInt32(HttpStatusCode.InternalServerError), null, "Something went wrong");

            await transaction.CommitAsync(cancellationToken);

            return this.Response();
        }
        catch (Exception ex)
        {
            return RemoveUserExceptionHandler.CommandHandlerException(ex.Message);
        }
    }
}

#endregion Command Service

#region Saga Service

public class RemoveUserActivitySagaResult
{
    public RemoveUserActivitySagaResult(IDbContextTransaction dbContextTransaction, UserEntityResultSet userEntityResultSet, Result removeOrgResult = default)
    {
        this.DbContextTransaction = dbContextTransaction;
        this.UserEntityResult = userEntityResultSet;
        this.RemoveOrgResult = removeOrgResult;
    }

    public IDbContextTransaction DbContextTransaction { get; }

    public UserEntityResultSet UserEntityResult { get; }

    public Result RemoveOrgResult { get; }
}

public class RemoveUserSagaOrchestratorService : IRequest<bool>
{
    public RemoveUserSagaOrchestratorService(IDbContextTransaction dbContextTransaction, UserEntityResultSet userEntityResult)
    {
        this.DbContextTransaction = dbContextTransaction;
        UserEntityResult = userEntityResult;
    }

    public UserEntityResultSet UserEntityResult { get; }

    public IDbContextTransaction DbContextTransaction { get; }
}

public class RemoveUserSagaOrchestratorServiceHandler : IRequestHandler<RemoveUserSagaOrchestratorService, bool>
{
    private readonly IMediator mediator = null;
    private readonly ILogger<RemoveUserSagaOrchestratorServiceHandler> logger = null;
    private readonly UsersContext usersContext = null;

    public RemoveUserSagaOrchestratorServiceHandler(IMediator mediator, ILogger<RemoveUserSagaOrchestratorServiceHandler> logger, UsersContext usersContext)
    {
        this.mediator = mediator;
        this.logger = logger;
        this.usersContext = usersContext;
    }

    private async Task<Result> RemoveOrgAsync(RemoveUserSagaOrchestratorService request)
    {
        DataResponse<RemoveOrganizationResponseDTO> removeOrgResponce = null;

        TusersOrganization tUsersOrganization = request.UserEntityResult?.Organization;
        if (tUsersOrganization is not null)
        {
            // Remove Org
            removeOrgResponce = await this.mediator.Send(new RemoveOrganizationIntegrationService()
            {
                Identifier = tUsersOrganization?.OrgId
            });

            if (removeOrgResponce.Success == false)
                return Result.Fail(new FluentResults.Error(removeOrgResponce.Message).WithMetadata("StatusCode", removeOrgResponce.StatusCode));
        }

        return Result.Ok();
    }

    private async Task<Result> RemoveUserAsync(Tuser tuser)
    {
        try
        {
            StatusEnum status = StatusEnum.Inactive;
            tuser.Status = status == StatusEnum.Inactive;

            this.usersContext.Update<Tuser>(tuser);
            await this.usersContext.SaveChangesAsync();

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new FluentResults.Error(ex.Message).WithMetadata("StatusCode", HttpStatusCode.InternalServerError));
        }
    }

    async Task<bool> IRequestHandler<RemoveUserSagaOrchestratorService, bool>.Handle(RemoveUserSagaOrchestratorService request, CancellationToken cancellationToken)
    {
        ;
        try
        {
            var sagaBuilder = new SagaBuilder("Remove-User-Saga");
            sagaBuilder.Activity<RemoveUserActivitySagaResult>("Remove-User-Activity", async () =>
            {
                // Remove Org
                var removeOrgResult = await this.RemoveOrgAsync(request);

                // Remove User
                var removeUserResult = await this.RemoveUserAsync(request.UserEntityResult.User!);

                if (removeUserResult.IsFailed)
                    return new SagaResult<RemoveUserActivitySagaResult>(false, new RemoveUserActivitySagaResult(request.DbContextTransaction, request.UserEntityResult, removeOrgResult));

                return new SagaResult<RemoveUserActivitySagaResult>(true, new RemoveUserActivitySagaResult(request.DbContextTransaction, request.UserEntityResult, removeOrgResult));
            })
            .CompensationActivity<RemoveUserActivitySagaResult>("Remove-User-Activity", "RollBack-Remove-Org-Activity", async s =>
            {
                if (s.IsSuccess == false && s?.Results?.RemoveOrgResult?.IsFailed == true)
                {
                    await this.mediator.Send(new RemoveOrganizationRollBackIntegrationService()
                    {
                        Identifier = s.Results.UserEntityResult.Organization.OrgId
                    });
                }
            })
            .CompensationActivity<RemoveUserActivitySagaResult>("Remove-User-Activity", "RollBack-Remove-User-Activity", async s =>
            {
                if (s.IsSuccess == false)
                    await s.Results.DbContextTransaction.RollbackAsync();
            });

            await sagaBuilder.ExecuteAsync();

            var removeUserActivitySagaResult = sagaBuilder.ActivityResults.FirstOrDefault(x => x.ActivityName == "Remove-User-Activity");

            if (!removeUserActivitySagaResult.SagaResult.IsSuccess)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogCritical($"{nameof(RemoveUserSagaOrchestratorServiceHandler)} : Message={ex.Message}");
            return false;
        }
    }
}

#endregion Saga Service