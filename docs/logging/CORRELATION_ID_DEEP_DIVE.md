# 🔍 Deep Dive: CorrelationIdMiddleware - Distributed Tracing

## 📚 Mục Lục
1. [Correlation ID là gì?](#1-correlation-id-là-gì)
2. [Tại sao cần Correlation ID?](#2-tại-sao-cần-correlation-id)
3. [Kiến trúc và Flow](#3-kiến-trúc-và-flow)
4. [Phân tích Code chi tiết](#4-phân-tích-code-chi-tiết)
5. [Use Cases thực tế](#5-use-cases-thực-tế)
6. [Best Practices](#6-best-practices)

---

## 1. Correlation ID là gì?

**Correlation ID** (hay còn gọi Request ID, Trace ID) là một **unique identifier** được gán cho mỗi HTTP request để:

- 🔗 **Link tất cả logs** từ một request xuyên suốt nhiều services
- 🔍 **Trace request flow** từ đầu đến cuối trong distributed system
- 🐛 **Debug dễ dàng** khi có lỗi xảy ra
- 📊 **Performance monitoring** - đo latency của từng service

### Ví dụ Trực Quan

```
User Request → Gateway → AuthService → UserService → Database
     │              │          │            │
     └─────────────[abc-123-def-456]───────┘
            ↑ Same Correlation ID ↑
```

**Logs sẽ như sau:**

```log
[Gateway]    INFO  [abc-123-def-456] Request received: POST /api/auth/login
[Gateway]    INFO  [abc-123-def-456] Forwarding to AuthService
[AuthService] INFO  [abc-123-def-456] Processing login request
[AuthService] INFO  [abc-123-def-456] Calling UserService to verify credentials
[UserService] INFO  [abc-123-def-456] Querying user from database
[UserService] INFO  [abc-123-def-456] User found: user@example.com
[AuthService] INFO  [abc-123-def-456] Login successful, generating token
[Gateway]    INFO  [abc-123-def-456] Response sent to client (200 OK)
```

Khi search logs với `abc-123-def-456`, bạn thấy **toàn bộ flow** của request đó!

---

## 2. Tại sao cần Correlation ID?

### ❌ Vấn Đề KHÔNG CÓ Correlation ID

Khi có lỗi trong microservices:

```log
[AuthService] ERROR Cannot connect to database
[UserService] ERROR User not found
[Gateway]    ERROR Request timeout
```

❓ **Câu hỏi:**
- Những logs này có liên quan đến cùng một request không?
- Request nào gây ra lỗi?
- User nào bị ảnh hưởng?
- Flow của request như thế nào trước khi lỗi?

→ **KHÔNG THỂ TRẢ LỜI** vì không có cách link các logs!

### ✅ Giải Pháp VỚI Correlation ID

```log
[Gateway]    INFO  [abc-123] Request received from user: john@example.com
[Gateway]    INFO  [abc-123] Forwarding to AuthService
[AuthService] INFO  [abc-123] Validating credentials for: john@example.com
[AuthService] ERROR [abc-123] Cannot connect to database
[Gateway]    ERROR [abc-123] Request timeout after 30s
```

✅ **Có thể trả lời:**
- ✅ Tất cả logs có `[abc-123]` là từ cùng một request
- ✅ User `john@example.com` gặp lỗi
- ✅ Lỗi xảy ra ở AuthService khi connect database
- ✅ Flow: Gateway → AuthService → Database connection failed

---

## 3. Kiến Trúc và Flow

### 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    Client Browser                       │
│                    (generates no ID)                    │
└────────────────────────┬────────────────────────────────┘
                         │ HTTP Request
                         │ (no X-Correlation-ID header)
                         ↓
┌─────────────────────────────────────────────────────────┐
│                   Gateway Service                       │
│  ┌─────────────────────────────────────────────────┐   │
│  │ CorrelationIdMiddleware                         │   │
│  │ • Generates: X-Correlation-ID: abc-123          │   │
│  │ • Adds to Response Headers                      │   │
│  │ • Forwards to downstream services               │   │
│  └─────────────────────────────────────────────────┘   │
└────────────────────────┬────────────────────────────────┘
                         │ HTTP Request
                         │ X-Correlation-ID: abc-123 ←
                         ↓
┌─────────────────────────────────────────────────────────┐
│                   AuthService                           │
│  ┌─────────────────────────────────────────────────┐   │
│  │ CorrelationIdMiddleware                         │   │
│  │ • Reads: X-Correlation-ID: abc-123              │   │
│  │ • Logs with abc-123                             │   │
│  │ • Calls UserService with abc-123               │   │
│  └─────────────────────────────────────────────────┘   │
└────────────────────────┬────────────────────────────────┘
                         │ HTTP Request
                         │ X-Correlation-ID: abc-123 ←
                         ↓
┌─────────────────────────────────────────────────────────┐
│                   UserService                           │
│  ┌─────────────────────────────────────────────────┐   │
│  │ CorrelationIdMiddleware                         │   │
│  │ • Reads: X-Correlation-ID: abc-123              │   │
│  │ • Logs with abc-123                             │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

### 🔄 Request Flow - Step by Step

**Scenario:** User login vào hệ thống

#### Step 1: Client gửi request
```http
POST /api/auth/login HTTP/1.1
Host: gateway.healink.com
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "secret123"
}
```

#### Step 2: Gateway nhận request
```csharp
// CorrelationIdMiddleware.InvokeAsync() tại Gateway
var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                   ?? Guid.NewGuid().ToString();
// → correlationId = "abc-123-def-456" (generated)

context.Response.Headers["X-Correlation-ID"] = "abc-123-def-456";
_logger.LogInformation("Processing request with CorrelationId: {CorrelationId}", "abc-123-def-456");
```

**Gateway Log:**
```log
[Gateway] INFO [abc-123-def-456] Processing request with CorrelationId: abc-123-def-456
```

#### Step 3: Gateway forward to AuthService
```csharp
// Gateway forwards request with header
var request = new HttpRequestMessage(HttpMethod.Post, "http://authservice-api/api/auth/verify");
request.Headers.Add("X-Correlation-ID", "abc-123-def-456"); // ← Propagate!
```

#### Step 4: AuthService nhận request
```csharp
// CorrelationIdMiddleware.InvokeAsync() tại AuthService
var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                   ?? Guid.NewGuid().ToString();
// → correlationId = "abc-123-def-456" (from Gateway)

correlationIdService.SetCorrelationId("abc-123-def-456");
_logger.LogInformation("Processing request with CorrelationId: {CorrelationId}", "abc-123-def-456");
```

**AuthService Log:**
```log
[AuthService] INFO [abc-123-def-456] Processing request with CorrelationId: abc-123-def-456
[AuthService] INFO [abc-123-def-456] Validating user credentials
```

#### Step 5: AuthService gọi UserService
```csharp
// AuthService forwards request
var request = new HttpRequestMessage(HttpMethod.Get, "http://userservice-api/api/users/by-email/john@example.com");
request.Headers.Add("X-Correlation-ID", "abc-123-def-456"); // ← Propagate!
```

#### Step 6: UserService nhận request
```csharp
// CorrelationIdMiddleware.InvokeAsync() tại UserService
var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
// → correlationId = "abc-123-def-456" (from AuthService)

_logger.LogInformation("Processing request with CorrelationId: {CorrelationId}", "abc-123-def-456");
```

**UserService Log:**
```log
[UserService] INFO [abc-123-def-456] Processing request with CorrelationId: abc-123-def-456
[UserService] INFO [abc-123-def-456] Querying database for user: john@example.com
[UserService] INFO [abc-123-def-456] User found with ID: 12345
```

#### Step 7: Response trở về
```
UserService → AuthService → Gateway → Client
            [abc-123-def-456]
```

**Final Gateway Log:**
```log
[Gateway] INFO [abc-123-def-456] Response sent: 200 OK
```

---

## 4. Phân Tích Code Chi Tiết

### 📁 File Structure

```
SharedLibrary/
└── Commons/
    ├── Middlewares/
    │   └── CorrelationIdMiddleware.cs       ← Main logic
    └── Configurations/
        └── LoggingConfiguration.cs          ← Service registration
```

### 🔍 Code Breakdown: CorrelationIdMiddleware.cs

#### Part 1: Class Definition và Constructor

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    public const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;        // ← Next middleware in pipeline
        _logger = logger;    // ← Logger for this middleware
    }
```

**Giải thích:**
- `RequestDelegate _next`: ASP.NET Core middleware pipeline pattern - gọi middleware tiếp theo
- `ILogger<CorrelationIdMiddleware> _logger`: Logger để ghi logs
- `CorrelationIdHeaderName = "X-Correlation-ID"`: HTTP header name (standard convention)

#### Part 2: Extract hoặc Generate Correlation ID

```csharp
public async Task InvokeAsync(HttpContext context, ICorrelationIdService correlationIdService)
{
    // Step 1: Extract từ incoming request HOẶC generate new
    var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault() 
                       ?? Guid.NewGuid().ToString();
```

**Logic Flow:**

```
                    ┌─────────────────────────────────┐
                    │ Request có X-Correlation-ID?    │
                    └─────────────┬───────────────────┘
                                  │
                   ┌──────────────┴──────────────┐
                   │                             │
                   ↓ YES                         ↓ NO
    ┌──────────────────────────┐   ┌────────────────────────┐
    │ Use existing ID          │   │ Generate new GUID      │
    │ (downstream service)     │   │ (first entry point)    │
    └──────────────────────────┘   └────────────────────────┘
                   │                             │
                   └──────────────┬──────────────┘
                                  ↓
                    ┌─────────────────────────────────┐
                    │ correlationId = "abc-123-..."   │
                    └─────────────────────────────────┘
```

**Ví dụ:**
- **Gateway (first entry):** Request KHÔNG có header → Generate `abc-123`
- **AuthService:** Request CÓ header `X-Correlation-ID: abc-123` → Use `abc-123`
- **UserService:** Request CÓ header `X-Correlation-ID: abc-123` → Use `abc-123`

#### Part 3: Store Correlation ID

```csharp
    // Step 2: Set vào CorrelationIdService (for dependency injection)
    correlationIdService.SetCorrelationId(correlationId);

    // Step 3: Add vào Response headers (client có thể thấy)
    context.Response.Headers[CorrelationIdHeaderName] = correlationId;

    // Step 4: Add vào HttpContext.Items (dễ access trong controllers)
    context.Items["CorrelationId"] = correlationId;
```

**Ba cách lưu trữ:**

| Location | Purpose | Access Method |
|----------|---------|---------------|
| `ICorrelationIdService` | Inject vào services via DI | `_correlationIdService.CorrelationId` |
| `Response.Headers` | Client/Gateway nhận được | Response header `X-Correlation-ID` |
| `HttpContext.Items` | Quick access trong pipeline | `context.Items["CorrelationId"]` |

#### Part 4: Health Check Filtering

```csharp
    // Step 5: Check if health check để reduce log noise
    var isHealthCheck = context.Request.Path.StartsWithSegments("/health");
```

**Why needed?**
- Docker gọi `/health` endpoint **mỗi 30 giây**
- Không cần log mỗi health check → **reduce log spam**

#### Part 5: Logging với Scope

```csharp
    // Step 6: Create logging scope với metadata
    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId,
        ["RequestPath"] = context.Request.Path,
        ["RequestMethod"] = context.Request.Method
    }))
    {
        // Logs in this scope will automatically include metadata
```

**Structured Logging Example:**

```json
{
  "timestamp": "2025-10-02T10:30:45.123Z",
  "level": "Information",
  "message": "Processing request with CorrelationId: abc-123",
  "correlationId": "abc-123",
  "requestPath": "/api/auth/login",
  "requestMethod": "POST",
  "service": "AuthService"
}
```

#### Part 6: Conditional Logging

```csharp
        // Step 7: Log theo level khác nhau
        if (!isHealthCheck)
        {
            _logger.LogInformation("Processing request with CorrelationId: {CorrelationId}", correlationId);
        }
        else
        {
            _logger.LogDebug("Health check request with CorrelationId: {CorrelationId}", correlationId);
        }
```

**Log Levels:**
- **Information:** Business requests (login, register, etc.)
- **Debug:** Health checks, internal monitoring

#### Part 7: Error Handling

```csharp
        try
        {
            // Step 8: Call next middleware in pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // Step 9: Log errors WITH correlation ID
            _logger.LogError(ex, "Unhandled exception in request with CorrelationId: {CorrelationId}", correlationId);
            throw; // Re-throw để global exception handler xử lý
        }
```

**Error Log Example:**

```log
ERROR [abc-123] Unhandled exception in request with CorrelationId: abc-123
System.NullReferenceException: Object reference not set to an instance of an object.
   at AuthService.Services.AuthService.ValidateCredentials(String email)
   at AuthService.Controllers.AuthController.Login(LoginRequest request)
```

#### Part 8: Completion Logging

```csharp
        finally
        {
            // Step 10: Log completion (chỉ cho non-health-check requests)
            if (!isHealthCheck)
            {
                _logger.LogDebug("Completed request with CorrelationId: {CorrelationId}", correlationId);
            }
        }
    }
}
```

### 🔧 Supporting Classes

#### ICorrelationIdService Interface

```csharp
public interface ICorrelationIdService
{
    string CorrelationId { get; }           // Get current correlation ID
    void SetCorrelationId(string correlationId); // Set correlation ID
}
```

#### CorrelationIdService Implementation

```csharp
public class CorrelationIdService : ICorrelationIdService
{
    private string _correlationId = Guid.NewGuid().ToString();
    
    public string CorrelationId => _correlationId;
    
    public void SetCorrelationId(string correlationId)
    {
        _correlationId = correlationId ?? Guid.NewGuid().ToString();
    }
}
```

**Lifecycle:** `AddScoped` - một instance per HTTP request

```csharp
// In LoggingConfiguration.cs
builder.Services.AddScoped<ICorrelationIdService, CorrelationIdService>();
```

---

## 5. Use Cases Thực Tế

### 🐛 Use Case 1: Debugging Production Error

**Scenario:** User report lỗi "Cannot login"

**Step 1:** User gửi email với error screenshot showing:
```
Error: Request timeout (500 Internal Server Error)
Correlation ID: cf7acd4c-9511-41c0-a7f6-3d70db351237
```

**Step 2:** Dev search logs:
```bash
grep "cf7acd4c-9511-41c0-a7f6-3d70db351237" logs/*.log
```

**Step 3:** Tìm thấy full trace:

```log
[10:30:45 Gateway]    INFO  [cf7acd4c] Request received: POST /api/auth/login
[10:30:45 Gateway]    INFO  [cf7acd4c] Forwarding to AuthService
[10:30:46 AuthService] INFO  [cf7acd4c] Processing login for: user@example.com
[10:30:46 AuthService] INFO  [cf7acd4c] Calling UserService to verify credentials
[10:30:47 UserService] INFO  [cf7acd4c] Querying database for user
[10:30:57 UserService] ERROR [cf7acd4c] Database connection timeout after 10s
[10:30:57 AuthService] ERROR [cf7acd4c] UserService call failed
[10:30:57 Gateway]    ERROR [cf7acd4c] Request timeout (500)
```

**✅ Root Cause Found:** UserService database connection timeout!

---

### 📊 Use Case 2: Performance Monitoring

**Scenario:** Monitor request latency across services

```log
[10:30:45.123 Gateway]    INFO [abc-123] Request started
[10:30:45.456 AuthService] INFO [abc-123] Processing started
[10:30:45.789 UserService] INFO [abc-123] Database query started
[10:30:46.012 UserService] INFO [abc-123] Database query completed (223ms)
[10:30:46.234 AuthService] INFO [abc-123] Processing completed (778ms)
[10:30:46.456 Gateway]    INFO [abc-123] Request completed (1333ms)
```

**Analysis:**
- Total time: **1333ms**
- Gateway overhead: 1333 - 778 = **555ms**
- AuthService processing: 778 - 223 = **555ms**
- Database query: **223ms**

---

### 🔗 Use Case 3: Distributed Tracing với External Tool

**Integration với Jaeger/Zipkin:**

```csharp
public class JaegerTracingMiddleware
{
    public async Task InvokeAsync(HttpContext context, ICorrelationIdService correlationIdService)
    {
        var correlationId = correlationIdService.CorrelationId;
        
        using var span = _tracer.StartActiveSpan("http.request");
        span.SetTag("correlation_id", correlationId);
        span.SetTag("http.method", context.Request.Method);
        span.SetTag("http.path", context.Request.Path);
        
        await _next(context);
    }
}
```

**Visualization trong Jaeger UI:**

```
User Request [abc-123]
├─ Gateway       (100ms)
│  └─ AuthService (450ms)
│     ├─ UserService (200ms)
│     │  └─ Database Query (150ms)
│     └─ Token Generation (50ms)
└─ Response      (50ms)
```

---

## 6. Best Practices

### ✅ DO: Propagate Correlation ID

```csharp
// GOOD: Forward correlation ID to downstream services
var request = new HttpRequestMessage(HttpMethod.Get, "http://userservice/api/users/123");
request.Headers.Add("X-Correlation-ID", _correlationIdService.CorrelationId);
await _httpClient.SendAsync(request);
```

### ❌ DON'T: Lose Correlation ID

```csharp
// BAD: Create new request without correlation ID
var request = new HttpRequestMessage(HttpMethod.Get, "http://userservice/api/users/123");
// Missing: request.Headers.Add("X-Correlation-ID", ...)
await _httpClient.SendAsync(request);
// ❌ Downstream service will generate NEW correlation ID!
```

### ✅ DO: Include in Structured Logs

```csharp
// GOOD: Structured logging with correlation ID
_logger.LogInformation(
    "User {UserId} logged in successfully. CorrelationId: {CorrelationId}",
    userId,
    _correlationIdService.CorrelationId
);
```

### ❌ DON'T: Hardcode in String

```csharp
// BAD: String concatenation loses structure
_logger.LogInformation($"User {userId} logged in. CorrelationId: {_correlationIdService.CorrelationId}");
// ❌ Cannot query by correlation ID efficiently
```

### ✅ DO: Return in API Response

```csharp
// GOOD: Client có thể reference lại
return new ApiResponse
{
    Success = true,
    Data = result,
    CorrelationId = _correlationIdService.CorrelationId // ← Include!
};
```

### ✅ DO: Use Consistent Header Name

```csharp
// GOOD: Industry standard
public const string CorrelationIdHeaderName = "X-Correlation-ID";

// ALSO GOOD: Alternative standards
// "X-Request-ID"
// "X-Trace-ID"
// "Trace-Id" (OpenTelemetry)
```

---

## 📋 Summary Table

| Aspect | Detail |
|--------|--------|
| **Purpose** | Trace requests across distributed services |
| **Header Name** | `X-Correlation-ID` |
| **Format** | GUID (UUID v4) - e.g., `abc-123-def-456-...` |
| **Lifecycle** | Generated at entry point, propagated to all services |
| **Storage** | 3 places: `ICorrelationIdService`, `Response.Headers`, `HttpContext.Items` |
| **Log Level** | Info (business requests), Debug (health checks) |
| **Registration** | `AddScoped<ICorrelationIdService, CorrelationIdService>()` |
| **Middleware Order** | Early in pipeline (before authentication) |

---

## 🚀 Next Steps

1. ✅ **Current:** CorrelationIdMiddleware working in all 6 services
2. ⏳ **Optional:** Integrate with APM tools (Jaeger, Zipkin, Application Insights)
3. ⏳ **Optional:** Add performance metrics (request duration tracking)
4. ⏳ **Optional:** Build correlation ID visualization dashboard

---

## 📚 References

- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [Distributed Tracing Patterns](https://microservices.io/patterns/observability/distributed-tracing.html)
- [OpenTelemetry Trace Context](https://www.w3.org/TR/trace-context/)
- [Correlation IDs in Microservices](https://blog.rapid7.com/2016/12/23/the-value-of-correlation-ids/)

---

Generated: 2025-10-02  
Status: ✅ Production Ready  
Services: 6 Microservices with Correlation ID Support
