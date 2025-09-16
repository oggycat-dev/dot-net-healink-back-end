using FluentValidation;
using MediatR;

namespace ProductAuthMicroservice.Commons.Behaviors;

/// <summary>
/// Pipeline behavior to validate requests using FluentValidation
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Handle the validation pipeline
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        // Create validation context
        var context = new ValidationContext<TRequest>(request);

        // Run all validators and collect results
        var validationResults = await Task.WhenAll(_validators.Select(v =>
            v.ValidateAsync(context, cancellationToken)));

        // Collect any validation failures
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        // Throw exception if there are validation failures
        if (failures.Count > 0)
        {
            // Log detailed validation failures - this makes it easier to debug
            var failureMessages = string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));
            
            throw new ValidationException(failures);
        }

        // Continue with the pipeline if validation passes
        return await next();
    }
} 