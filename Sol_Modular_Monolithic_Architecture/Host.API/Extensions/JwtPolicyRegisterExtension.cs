﻿using Microsoft.AspNetCore.Authorization;
using Users.Application.Modules.Shared.Policy;

namespace Host.API.Extensions
{
    public static class JwtPolicyRegisterExtension
    {
        public static Action<AuthorizationOptions>? GetRegisterPolicy()
        {
            return (option) =>
            {
                option.AddPolicy(ConstantValue.SellerPolicy, (policy) => policy.Requirements.Add(new SellerOnlyAuthRequirement(Users.Contracts.Shared.Enums.UserType.Seller)));
                option.AddPolicy(ConstantValue.BuyerPolicy, (policy) => policy.Requirements.Add(new BuyerOnlyAuthRequirement(Users.Contracts.Shared.Enums.UserType.Buyer)));
                option.AddPolicy(ConstantValue.BuyerSellerPolicy, (policy) => policy.Requirements.Add(new BuyerSellerOnlyAuthRequirement()));
            };
        }
    }
}