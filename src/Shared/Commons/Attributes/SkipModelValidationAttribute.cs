using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProductAuthMicroservice.Commons.Attributes;

/// <summary>
/// Attribute that disables model validation for the action it's applied to
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class SkipModelValidationAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Clear any validation errors that might have been set
        context.ModelState.Clear();
        base.OnActionExecuting(context);
    }
}
