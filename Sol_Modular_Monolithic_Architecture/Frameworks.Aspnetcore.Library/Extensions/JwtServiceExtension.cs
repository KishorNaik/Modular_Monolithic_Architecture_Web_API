using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Frameworks.Aspnetcore.Library.Extensions
{
    public interface IJwtTokenService
    {
        Task<string> GenerateJwtTokenAsync(JwtAppSetting jwtAppSetting, Claim[] claims, DateTime? expires);

        Task<string> GenerateRefreshTokenAsync();

        Task<ClaimsPrincipal> GetPrincipalFromExpiredTokenAsync(string secretKey, string token);
    }

    public class JwtTokenService : IJwtTokenService
    {
        Task<string> IJwtTokenService.GenerateJwtTokenAsync(JwtAppSetting jwtAppSetting, Claim[] claims, DateTime? expires)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(jwtAppSetting?.SecretKey!);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = expires ?? DateTime.UtcNow.AddDays(1),
                    Issuer = jwtAppSetting?.Issuer!,
                    Audience = jwtAppSetting?.Audience!,
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                var token = tokenHandler.WriteToken(securityToken);

                return Task.FromResult<String>(token);
            }
            catch
            {
                throw;
            }
        }

        Task<string> IJwtTokenService.GenerateRefreshTokenAsync()
        {
            try
            {
                var randomNumber = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomNumber);
                    return Task.FromResult(Convert.ToBase64String(randomNumber));
                }
            }
            catch
            {
                throw;
            }
        }

        Task<ClaimsPrincipal> IJwtTokenService.GetPrincipalFromExpiredTokenAsync(string secretKey, string token)
        {
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false, //you might want to validate the audience and issuer depending on your use case
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateLifetime = false //here we are saying that we don't care about the token's expiration date
                };
                var tokenHandler = new JwtSecurityTokenHandler();
                SecurityToken securityToken;
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
                var jwtSecurityToken = securityToken as JwtSecurityToken;
                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    throw new SecurityTokenException("Invalid token");
                return Task.FromResult(principal);
            }
            catch
            {
                throw;
            }
        }
    }

    public class JwtAppSetting
    {
        public string? SecretKey { get; set; }

        public string? Issuer { get; set; }

        public string? Audience { get; set; }
    }

    public static class JwtServiceExtension
    {
        public static void AddJwtToken(this IServiceCollection services, JwtAppSetting jwtAppSetting, Action<AuthorizationOptions> authConfigure = null)
        {
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            var key = Encoding.ASCII.GetBytes(jwtAppSetting?.SecretKey!);
            services
            .AddAuthorization(x => authConfigure?.Invoke(x))
            .AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtAppSetting?.Issuer!,
                    ValidAudience = jwtAppSetting?.Audience!,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                };
            });
        }
    }
}