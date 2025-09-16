namespace ProductAuthMicroservice.Commons.Enums;

// <summary>
/// Error codes for API responses
/// </summary>
public enum ErrorCodeEnum
{
    // Success
    Success = 0,
    
    // Authentication & Authorization (401, 403)
    Unauthorized = 1001,
    Forbidden = 1002,
    InvalidCredentials = 1003,
    TokenExpired = 1004,
    InvalidToken = 1005,
    TokenRevoked = 1006,
    
    // Validation & Bad Request (400)
    ValidationFailed = 2001,
    InvalidInput = 2002,
    DuplicateEntry = 2003,
    InvalidOperation = 2004,
    TooManyRequests = 2005,
    
    // Not Found (404)
    NotFound = 3001,
    
    // Business Logic Errors (422)
    BusinessRuleViolation = 4001,
    InsufficientPermissions = 4002,
    ResourceConflict = 4003,
    
    // Internal Server Errors (500)
    InternalError = 5001,
    DatabaseError = 5002,
    ExternalServiceError = 5003,
    
    // File & Storage Errors
    FileUploadFailed = 6001,
    FileNotFound = 6002,
    StorageError = 6003,
    InvalidFileType = 6004,
    FileSizeTooLarge = 6005,
    
    // AI & External Service Errors
    FeatureDisabled = 7001,
    InvalidResponse = 7002,
    
    // Email Errors
    EmailSendFailed = 8001,
    EmailNotConfirmed = 8002,
    EmailAlreadyConfirmed = 8003,
    InvalidEmailToken = 8004
}
