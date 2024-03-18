using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Results;
using Frameworks.Aspnetcore.Library.MIddleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Utility.Shared.Validations;

namespace Frameworks.Aspnetcore.Library.Extensions;

public class Error
{
    public string Code { get; set; }
    public string Description { get; set; }
}

public class UseCustomErrorModelInterceptor : IValidatorInterceptor
{
    public ValidationResult AfterAspNetValidation(ActionContext actionContext, IValidationContext validationContext, ValidationResult result)
    {
        var failures = result.Errors
             .Select(error => new ValidationFailure(error.PropertyName, SerializeError(error)));

        return new ValidationResult(failures);
    }

    public IValidationContext BeforeAspNetValidation(ActionContext actionContext, IValidationContext commonContext)
    {
        return commonContext;
    }

    private static string SerializeError(ValidationFailure failure)
    {
        var error = new Error()
        {
            Code = failure.ErrorCode,
            Description = failure.ErrorMessage
        };
        return JsonSerializer.Serialize(error);
    }
}

public static class FluentValidationServiceExtension
{
    public static IMvcBuilder AddFluentValidationException(this IMvcBuilder mvcBuilder, Type type, IServiceCollection services)
    {
        var obj = mvcBuilder
            .ConfigureApiBehaviorOptions((option) =>
            {
                option.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(v => v.Errors)
                        //.Select(e => JsonSerializer.Deserialize<Error>(e.ErrorMessage));
                        .Select(e =>
                        {
                            bool isJson = JsonValidation.IsValidJson(e.ErrorMessage);

                            if (!isJson)
                                return new Error()
                                {
                                    Code = "Unexpected",
                                    Description = e.ErrorMessage
                                };

                            Error error = JsonSerializer.Deserialize<Error>(e.ErrorMessage);

                            error.Description = error.Description.Replace("'Body ", "'");
                            error.Description = error.Description.Replace("'Query ", "'");

                            return error;
                        });

                    var response = new
                    {
                        Success = false,
                        MessageList = errors
                    };

                    return new BadRequestObjectResult(response);
                };
            })
            .AddFluentValidation((config) =>
            {
                config.RegisterValidatorsFromAssemblyContaining(type);
                config.ImplicitlyValidateChildProperties = true;
            });

        services.AddTransient<IValidatorInterceptor, UseCustomErrorModelInterceptor>();

        return obj;
    }
}