namespace ProductAuthMicroservice.Commons.Exceptions;

public class ForbiddenAccessException : Exception
{
    /// <summary>
    /// Create a new forbidden access exception
    /// </summary>
    public ForbiddenAccessException()
        : base("You do not have permission to access this resource.")
    {
    }
    
    /// <summary>
    /// Create a new forbidden access exception with custom message
    /// </summary>
    public ForbiddenAccessException(string message)
        : base(message)
    {
    }
} 
