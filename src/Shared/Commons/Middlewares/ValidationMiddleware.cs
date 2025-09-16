using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.Commons.Middlewares;

/// <summary>
/// Filter attribute to validate models using FluentValidation
/// </summary>
public class ValidationFilterAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Validates the model state before processing the request
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(m => m.Value?.Errors.Count > 0)
                .SelectMany(m => m.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();

            var errorResponse = Result.Failure("Invalid input data", ErrorCodeEnum.ValidationFailed, errors);
            context.Result = new BadRequestObjectResult(errorResponse);
        }
    }
}

/// <summary>
/// Extension method to register validation behavior
/// </summary>
public static class ValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds validation configuration to services 
    /// </summary>
    public static IServiceCollection AddValidationConfiguration(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilterAttribute>();
        });
        
        return services;
    }
}
