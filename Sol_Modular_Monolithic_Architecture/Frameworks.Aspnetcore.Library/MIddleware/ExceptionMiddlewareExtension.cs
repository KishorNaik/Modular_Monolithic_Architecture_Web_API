using FluentResults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frameworks.Aspnetcore.Library.MIddleware
{
    public record ErrorHandler(bool Success, int StatusCode, string Message);

    public static class ExceptionMiddlewareExtensions
    {
        public static void UseCustomeExceptionHandler(this IApplicationBuilder app)
        {
            _ = app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    //context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        var errorHandler = new ErrorHandler(false, context.Response.StatusCode, contextFeature.Error.Message);

                        await context.Response.WriteAsJsonAsync(errorHandler);
                    }
                });
            });
        }
    }
}