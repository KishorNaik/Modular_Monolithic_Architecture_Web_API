namespace Users.Application.Modules.Shared.Policy;

public class BuyerOnlyAuthRequirement : IAuthorizationRequirement
{
    public BuyerOnlyAuthRequirement(UserType userType)
    {
        this.UserType = userType;
    }

    public UserType UserType { get; }
}

public class BuyerOnlyAuthRequirementHandler : AuthorizationHandler<BuyerOnlyAuthRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BuyerOnlyAuthRequirement requirement)
    {
        if (!context.User.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return Task.CompletedTask;
        }

        string roleName = Convert.ToString(context.User.FindFirst(c => c.Type == ClaimTypes.Role).Value);

        if (roleName.ToLower() == UserType.Buyer.ToString().ToLower())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}