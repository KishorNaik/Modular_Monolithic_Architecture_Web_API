namespace Users.Application.Modules.Shared.Policy;

public class BuyerSellerOnlyAuthRequirement : IAuthorizationRequirement
{
    public BuyerSellerOnlyAuthRequirement()
    {
    }
}

public class BuyerSellerOnlyAuthRequirementHandler : AuthorizationHandler<BuyerSellerOnlyAuthRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BuyerSellerOnlyAuthRequirement requirement)
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
        else if (roleName.ToLower() == UserType.Seller.ToString().ToLower())
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}