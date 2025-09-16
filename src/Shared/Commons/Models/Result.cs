using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Enums;

namespace ProductAuthMicroservice.Commons.Models;

public class Result<T>
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; } = null;

    public static Result<T> Success(T data, string message = "Success")
    {
        return new Result<T> { IsSuccess = true, Message = message, Data = data };
    }

    public static Result<T> Failure(string message, ErrorCodeEnum errorCode, List<string>? errors = null)
    {
        return new Result<T> { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString(), Errors = errors };
    }

    public static Result<T> Failure(string message, ErrorCodeEnum errorCode)
    {
        return new Result<T> { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString() };
    }
}

public class Result
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }
    [JsonPropertyName("errorCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorCode { get; set; } = null;

    public static Result Success(string message = "Success")
    {
        return new Result { IsSuccess = true, Message = message };
    }

    public static Result Failure(string message, ErrorCodeEnum errorCode, List<string>? errors = null)
    {
        return new Result { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString(), Errors = errors };
    }

    public static Result Failure(string message, ErrorCodeEnum errorCode)
    {
        return new Result { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString()};
    }
}