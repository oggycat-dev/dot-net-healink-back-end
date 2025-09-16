using FluentValidation.Results;

namespace ProductAuthMicroservice.Commons.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Dictionary of validation errors
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }
    
    /// <summary>
    /// Flat list of all error messages
    /// </summary>
    public List<string> ErrorMessages { get; }

    /// <summary>
    /// Create a new validation exception
    /// </summary>
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
        ErrorMessages = new List<string>();
    }

    /// <summary>
    /// Create a new validation exception with failures
    /// </summary>
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        // Group by property name for structured errors
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
            
        // Also create a flat list of all error messages for easier access
        ErrorMessages = failures.Select(f => f.ErrorMessage).ToList();
    }
}