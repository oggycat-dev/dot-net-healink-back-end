using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Extensions;

namespace ProductAuthMicroservice.Commons.Models;

public class PaginationResult<T>
{
    [JsonPropertyName("currentPage")]
    public int CurrentPage { get; set; }
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }
    [JsonPropertyName("totalPages")]
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    [JsonPropertyName("hasPrevious")]
    public bool HasPrevious => CurrentPage > 1;
    [JsonPropertyName("hasNext")]
    public bool HasNext => CurrentPage < TotalPages;
    [JsonPropertyName("items")]
    public IEnumerable<T> Items { get; set; } = new List<T>();
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; } 
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; set; }
    [JsonPropertyName("errorCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorCode { get; set; } = null;


    public static PaginationResult<T> Success(List<T> items, int pageNumber, int pageSize, int totalItems)
    {
        return new PaginationResult<T>
        {
            IsSuccess = true,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            Items = items
        };
    }

    public static PaginationResult<T> Failure(string message, ErrorCodeEnum errorCode, List<string>? errors = null)
    {
        return new PaginationResult<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors,
            ErrorCode = errorCode.ToString()
        };
    }

    public static PaginationResult<T> Failure(string message, ErrorCodeEnum errorCode)
    {
        return new PaginationResult<T>
        {
            IsSuccess = false,
            Message = message,
            ErrorCode = errorCode.ToString()
        };
    }

    /// <summary>
    /// Gets HTTP status code from PaginatedResult
    /// </summary>
    public int GetHttpStatusCode()
    {
        if (IsSuccess) return StatusCodes.Status200OK;
        
        if (string.IsNullOrEmpty(ErrorCode) || !Enum.TryParse<ErrorCodeEnum>(ErrorCode, out var errorCode))
            return StatusCodes.Status500InternalServerError;
            
        return errorCode.ToHttpStatusCode();
    }
}