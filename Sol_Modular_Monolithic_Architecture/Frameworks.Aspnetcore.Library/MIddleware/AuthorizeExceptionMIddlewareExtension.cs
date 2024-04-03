using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Models.Shared.Response;
using System.Text.Json;

namespace Frameworks.Aspnetcore.Library.MIddleware;

public class AuthorizeExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizeExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        await _next(httpContext);

        if (httpContext.Response.StatusCode == 401)
        {
            await HandleExceptionAsync(httpContext, "UnAuthorize");
        }
        else if (httpContext.Response.StatusCode == 403)
        {
            await HandleExceptionAsync(httpContext, "Forbidden");
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, string message)
    {
        context.Response.ContentType = "application/json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        };

        var errorHandler = new ErrorHandlerModel(false, context.Response.StatusCode, message);
        await context.Response.WriteAsJsonAsync(errorHandler, options);
    }
}

public static class AuthorizeExceptionMiddlewareExtension
{
    public static IApplicationBuilder UseAuthorizeExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizeExceptionMiddleware>();
    }
}