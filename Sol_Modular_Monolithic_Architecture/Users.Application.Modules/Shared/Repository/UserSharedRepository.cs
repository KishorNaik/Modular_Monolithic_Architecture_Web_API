namespace Users.Application.Modules.Shared.Repository;

#region Data Result Sets

public class UserEntityResultSet
{
    public Tuser? User { get; set; }

    public TusersOrganization? Organization { get; set; }
}

#endregion Data Result Sets

#region Shared Repository

public interface IUserSharedRepository
{
    Task<Result<UserEntityResultSet>> GetUserByIdentifierAsync(Guid? userIdentifer);

    Task<Result<Tuser>> GetUserByEmailIdAsync(string? emailId);

    Task UpdateRefreshToken(string refreshToken, DateTime tokenExpiryTime, string emailId = default(string), Tuser tuser = default(Tuser));
}

public class UserSharedRepository : IUserSharedRepository
{
    private readonly UsersContext usersContext = null;

    public UserSharedRepository(UsersContext usersContext)
    {
        this.usersContext = usersContext;
    }

    async Task<Result<UserEntityResultSet>> IUserSharedRepository.GetUserByIdentifierAsync(Guid? userIdentifer)
    {
        try
        {
            var tUserResult = await this.usersContext.Tusers.AsNoTracking()
                                                    .FirstOrDefaultAsync((e) => e.Identifier == userIdentifer);

            var tUserOrgResult = await this.usersContext.TusersOrganizations.AsNoTracking()
                                                    .FirstOrDefaultAsync((e) => e.UserId == userIdentifer);
            if (tUserResult is null)
                return Result.Fail<UserEntityResultSet>(new FluentResults.Error("The user is not found").WithMetadata("StatusCode", HttpStatusCode.NotFound));

            return Result.Ok<UserEntityResultSet>(new UserEntityResultSet()
            {
                User = tUserResult,
                Organization = tUserOrgResult
            });
        }
        catch (Exception ex)
        {
            return Result.Fail<UserEntityResultSet>(new FluentResults.Error(ex.Message).WithMetadata("StackTrace", ex.StackTrace).WithMetadata("StatusCode", HttpStatusCode.InternalServerError));
        }
    }

    async Task<Result<Tuser>> IUserSharedRepository.GetUserByEmailIdAsync(string? emailId)
    {
        try
        {
            var tUserResult = await this.usersContext.Tusers.AsNoTracking()
                                                          .FirstOrDefaultAsync((e) => e.EmailId == emailId);

            var checkTUserResult = Guard.Against.Null(tUserResult).ToResult<Tuser>();

            if (checkTUserResult.IsFailed)
                return Result.Fail<Tuser>(new FluentResults.Error("The user is not found").WithMetadata("StatusCode", HttpStatusCode.NotFound));

            return Result.Ok<Tuser>(tUserResult);
        }
        catch (Exception ex)
        {
            return Result.Fail<Tuser>(new FluentResults.Error(ex.Message).WithMetadata("StackTrace", ex.StackTrace).WithMetadata("StatusCode", HttpStatusCode.InternalServerError));
        }
    }

    async Task IUserSharedRepository.UpdateRefreshToken(string refreshToken, DateTime tokenExpiryTime, string emailId = default(string), Tuser tuser = default(Tuser))
    {
        try
        {
            if (tuser is null)
            {
                var tUserResult = await this.usersContext.Tusers.AsNoTracking()
                                                              .FirstOrDefaultAsync((e) => e.EmailId == emailId);

                var checkTUserResult = Guard.Against.Null(tUserResult).ToResult();

                if (checkTUserResult.IsFailed)
                    throw new Exception("The user not found.");

                tuser = tUserResult;
            }

            tuser.RefreshToken = refreshToken;
            tuser.RefreshTokenExpiryTime = tokenExpiryTime;

            this.usersContext.Update<Tuser>(tuser);

            await this.usersContext.SaveChangesAsync();
        }
        catch
        {
            throw;
        }
    }
}

#endregion Shared Repository