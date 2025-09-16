using Microsoft.AspNetCore.Http;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.Commons.Extensions;

/// <summary>
/// Extension methods for Result classes
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Gets HTTP status code from Result
    /// </summary>
    public static int GetHttpStatusCode<T>(this Result<T> result)
    {
        if (result.IsSuccess) return StatusCodes.Status200OK;
        
        if (string.IsNullOrEmpty(result.ErrorCode) || !Enum.TryParse<ErrorCodeEnum>(result.ErrorCode, out var errorCode))
            return StatusCodes.Status500InternalServerError;
            
        return errorCode.ToHttpStatusCode();
    }
    
    /// <summary>
    /// Gets HTTP status code from Result
    /// </summary>
    public static int GetHttpStatusCode(this Result result)
    {
        if (result.IsSuccess) return StatusCodes.Status200OK;
        
        if (string.IsNullOrEmpty(result.ErrorCode) || !Enum.TryParse<ErrorCodeEnum>(result.ErrorCode, out var errorCode))
            return StatusCodes.Status500InternalServerError;
            
        return errorCode.ToHttpStatusCode();
    }
} 