# 🔄 CorrelationIdMiddleware - Flow Diagrams

## 🎯 Complete Request Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           USER BROWSER                                      │
│                                                                             │
│  POST /api/auth/login                                                       │
│  { "email": "john@example.com", "password": "secret123" }                  │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 │ NO X-Correlation-ID Header
                                 │
                                 ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                          GATEWAY SERVICE (Port 5000)                        │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ CorrelationIdMiddleware.InvokeAsync()                                   │ │
│ │                                                                         │ │
│ │ 1. Extract from Header:                                                │ │
│ │    var id = context.Request.Headers["X-Correlation-ID"]                │ │
│ │            ?? Guid.NewGuid().ToString();                               │ │
│ │    → id = "abc-123-def-456" (GENERATED - first entry point)           │ │
│ │                                                                         │ │
│ │ 2. Store in 3 places:                                                  │ │
│ │    • correlationIdService.SetCorrelationId("abc-123-def-456")         │ │
│ │    • context.Response.Headers["X-Correlation-ID"] = "abc-123-def-456" │ │
│ │    • context.Items["CorrelationId"] = "abc-123-def-456"               │ │
│ │                                                                         │ │
│ │ 3. Log:                                                                │ │
│ │    _logger.LogInformation("Processing request with                     │ │
│ │        CorrelationId: {CorrelationId}", "abc-123-def-456");           │ │
│ │                                                                         │ │
│ │ 4. Call next middleware:                                               │ │
│ │    await _next(context);  ──────────────┐                             │ │
│ └─────────────────────────────────────────┼─────────────────────────────┘ │
│                                            │                               │
│                          ┌─────────────────┘                               │
│                          ↓                                                 │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ Gateway Controller (AuthController)                                     │ │
│ │                                                                         │ │
│ │ • Receives request with CorrelationId in scope                        │ │
│ │ • Forwards to AuthService:                                            │ │
│ │                                                                         │ │
│ │   var request = new HttpRequestMessage(                               │ │
│ │       HttpMethod.Post,                                                │ │
│ │       "http://authservice-api/api/auth/verify");                      │ │
│ │                                                                         │ │
│ │   request.Headers.Add("X-Correlation-ID", "abc-123-def-456");  ←─────│ │
│ │                              ↑                                          │ │
│ │                         PROPAGATE!                                      │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 │ WITH X-Correlation-ID: abc-123-def-456
                                 │
                                 ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                         AUTHSERVICE (Port 5001)                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ CorrelationIdMiddleware.InvokeAsync()                                   │ │
│ │                                                                         │ │
│ │ 1. Extract from Header:                                                │ │
│ │    var id = context.Request.Headers["X-Correlation-ID"]                │ │
│ │            ?? Guid.NewGuid().ToString();                               │ │
│ │    → id = "abc-123-def-456" (FROM GATEWAY - reuse existing!)          │ │
│ │                                                                         │ │
│ │ 2. Store & Log (same as Gateway)                                       │ │
│ │    _logger.LogInformation("Processing request with                     │ │
│ │        CorrelationId: {CorrelationId}", "abc-123-def-456");           │ │
│ │                                                                         │ │
│ │ 3. Call next middleware → Controller                                   │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ AuthController.VerifyCredentials()                                      │ │
│ │                                                                         │ │
│ │ • Validates user credentials                                           │ │
│ │ • Needs to call UserService:                                           │ │
│ │                                                                         │ │
│ │   var request = new HttpRequestMessage(                               │ │
│ │       HttpMethod.Get,                                                 │ │
│ │       "http://userservice-api/api/users/by-email/john@example.com");  │ │
│ │                                                                         │ │
│ │   request.Headers.Add("X-Correlation-ID", "abc-123-def-456");  ←─────│ │
│ │                              ↑                                          │ │
│ │                         PROPAGATE AGAIN!                                │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 │ WITH X-Correlation-ID: abc-123-def-456
                                 │
                                 ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                         USERSERVICE (Port 5002)                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ CorrelationIdMiddleware.InvokeAsync()                                   │ │
│ │                                                                         │ │
│ │ 1. Extract from Header:                                                │ │
│ │    var id = context.Request.Headers["X-Correlation-ID"]                │ │
│ │            ?? Guid.NewGuid().ToString();                               │ │
│ │    → id = "abc-123-def-456" (FROM AUTHSERVICE - still same!)          │ │
│ │                                                                         │ │
│ │ 2. Store & Log                                                         │ │
│ │    _logger.LogInformation("Processing request with                     │ │
│ │        CorrelationId: {CorrelationId}", "abc-123-def-456");           │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ UserController.GetUserByEmail()                                         │ │
│ │                                                                         │ │
│ │ • Query database for user                                              │ │
│ │ • Log with CorrelationId:                                              │ │
│ │   _logger.LogInformation("User found: {Email}, CorrelationId: {Id}",  │ │
│ │       "john@example.com", "abc-123-def-456");                         │ │
│ │ • Return user data                                                     │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 │ Response with user data
                                 │ Header: X-Correlation-ID: abc-123-def-456
                                 │
                                 ↓
                        ┌────────────────┐
                        │  AuthService   │ ← Receives response
                        └────────┬───────┘
                                 │ Generate JWT token
                                 │ Header: X-Correlation-ID: abc-123-def-456
                                 │
                                 ↓
                        ┌────────────────┐
                        │    Gateway     │ ← Receives response
                        └────────┬───────┘
                                 │ Header: X-Correlation-ID: abc-123-def-456
                                 │
                                 ↓
                        ┌────────────────┐
                        │ User Browser   │ ← Receives final response
                        └────────────────┘
                        Response Headers:
                        • X-Correlation-ID: abc-123-def-456
                        • Authorization: Bearer eyJhbGc...
```

---

## 📊 Correlation ID Lifecycle

```
Time  │ Service      │ CorrelationId      │ Action
──────┼──────────────┼────────────────────┼─────────────────────────────────
10:30 │ Gateway      │ abc-123 (GENERATE) │ Receives request from client
10:30 │ Gateway      │ abc-123 (STORE)    │ Adds to Response.Headers
10:30 │ Gateway      │ abc-123 (LOG)      │ "Processing request..."
10:30 │ Gateway      │ abc-123 (FORWARD)  │ Calls AuthService with header
──────┼──────────────┼────────────────────┼─────────────────────────────────
10:31 │ AuthService  │ abc-123 (EXTRACT)  │ Reads from Request.Headers
10:31 │ AuthService  │ abc-123 (STORE)    │ Sets in CorrelationIdService
10:31 │ AuthService  │ abc-123 (LOG)      │ "Processing request..."
10:31 │ AuthService  │ abc-123 (FORWARD)  │ Calls UserService with header
──────┼──────────────┼────────────────────┼─────────────────────────────────
10:31 │ UserService  │ abc-123 (EXTRACT)  │ Reads from Request.Headers
10:31 │ UserService  │ abc-123 (STORE)    │ Sets in CorrelationIdService
10:31 │ UserService  │ abc-123 (LOG)      │ "Processing request..."
10:31 │ UserService  │ abc-123 (LOG)      │ "Querying database..."
10:32 │ UserService  │ abc-123 (LOG)      │ "User found"
10:32 │ UserService  │ abc-123 (RETURN)   │ Response with header
──────┼──────────────┼────────────────────┼─────────────────────────────────
10:32 │ AuthService  │ abc-123 (RECEIVE)  │ Gets UserService response
10:32 │ AuthService  │ abc-123 (LOG)      │ "Generating JWT token"
10:32 │ AuthService  │ abc-123 (RETURN)   │ Response with header
──────┼──────────────┼────────────────────┼─────────────────────────────────
10:32 │ Gateway      │ abc-123 (RECEIVE)  │ Gets AuthService response
10:32 │ Gateway      │ abc-123 (LOG)      │ "Request completed"
10:32 │ Gateway      │ abc-123 (RETURN)   │ Final response to client
──────┴──────────────┴────────────────────┴─────────────────────────────────

Total Duration: 2 seconds
Services Involved: 3 (Gateway, AuthService, UserService)
Same CorrelationId Throughout: abc-123-def-456
```

---

## 🔍 Log Aggregation Example

### Complete Log Stream (All Services)

```log
[2025-10-02 10:30:45.123] [Gateway]     INFO  [abc-123] Processing request with CorrelationId: abc-123-def-456
[2025-10-02 10:30:45.234] [Gateway]     INFO  [abc-123] Forwarding to AuthService: POST /api/auth/verify
[2025-10-02 10:30:45.456] [AuthService] INFO  [abc-123] Processing request with CorrelationId: abc-123-def-456
[2025-10-02 10:30:45.567] [AuthService] INFO  [abc-123] Validating credentials for: john@example.com
[2025-10-02 10:30:45.678] [AuthService] INFO  [abc-123] Calling UserService to verify user
[2025-10-02 10:30:45.789] [UserService] INFO  [abc-123] Processing request with CorrelationId: abc-123-def-456
[2025-10-02 10:30:45.890] [UserService] INFO  [abc-123] Querying database for user: john@example.com
[2025-10-02 10:30:46.012] [UserService] INFO  [abc-123] User found with ID: 12345
[2025-10-02 10:30:46.123] [UserService] DEBUG [abc-123] Completed request with CorrelationId: abc-123-def-456
[2025-10-02 10:30:46.234] [AuthService] INFO  [abc-123] User validated successfully
[2025-10-02 10:30:46.345] [AuthService] INFO  [abc-123] Generating JWT token for user: john@example.com
[2025-10-02 10:30:46.456] [AuthService] DEBUG [abc-123] Completed request with CorrelationId: abc-123-def-456
[2025-10-02 10:30:46.567] [Gateway]     INFO  [abc-123] AuthService response received: 200 OK
[2025-10-02 10:30:46.678] [Gateway]     DEBUG [abc-123] Completed request with CorrelationId: abc-123-def-456
```

### Search Command

```bash
# Search all logs for specific correlation ID
grep "abc-123" logs/**/*.log

# Or with Docker
docker-compose logs | grep "abc-123"
```

---

## 🔄 Health Check vs Business Request

### Business Request (WITH Correlation ID Logging)

```
┌────────────────┐
│ Client Request │
│ POST /api/auth │
└───────┬────────┘
        │
        ↓
┌─────────────────────────────────────┐
│ CorrelationIdMiddleware             │
│                                     │
│ isHealthCheck = false               │
│ → Log at Information level          │
│                                     │
│ Log: "Processing request with       │
│       CorrelationId: abc-123"       │
└───────┬─────────────────────────────┘
        │
        ↓
┌─────────────────────────────────────┐
│ Controller processes request        │
└─────────────────────────────────────┘
```

**Log Output:**
```log
INFO [abc-123] Processing request with CorrelationId: abc-123-def-456
```

### Health Check (NO Correlation ID Logging at Info)

```
┌────────────────┐
│ Docker Daemon  │
│ GET /health    │
└───────┬────────┘
        │
        ↓
┌─────────────────────────────────────┐
│ CorrelationIdMiddleware             │
│                                     │
│ isHealthCheck = true                │
│ → Log at Debug level (filtered)     │
│                                     │
│ Log: [Debug] "Health check request │
│       with CorrelationId: xyz-789"  │
└───────┬─────────────────────────────┘
        │
        ↓
┌─────────────────────────────────────┐
│ HealthController returns OK         │
└─────────────────────────────────────┘
```

**Log Output:**
```log
(No log at Information level - only Debug)
```

---

## 🎯 Error Scenario with Correlation ID

### When Error Occurs

```
User Request [abc-123]
    ↓
Gateway [abc-123] → OK
    ↓
AuthService [abc-123] → Processing
    ↓
UserService [abc-123] → ERROR! Database timeout
    ↓
AuthService [abc-123] → ERROR! Downstream failure
    ↓
Gateway [abc-123] → ERROR! Return 500 to client
```

### Error Logs (All with Same Correlation ID)

```log
[10:30:45] [Gateway]     INFO  [abc-123] Processing request
[10:30:45] [Gateway]     INFO  [abc-123] Forwarding to AuthService
[10:30:46] [AuthService] INFO  [abc-123] Processing request
[10:30:46] [AuthService] INFO  [abc-123] Calling UserService
[10:30:47] [UserService] INFO  [abc-123] Processing request
[10:30:47] [UserService] INFO  [abc-123] Querying database
[10:30:57] [UserService] ERROR [abc-123] Database connection timeout after 10s
                                          Connection string: Server=postgres;Database=userdb;...
                                          Stack trace: at Npgsql.NpgsqlConnection.Open()...
[10:30:57] [AuthService] ERROR [abc-123] UserService call failed: HTTP 500
                                          Response: {"error": "Database timeout"}
[10:30:57] [Gateway]     ERROR [abc-123] AuthService returned error: HTTP 500
[10:30:57] [Gateway]     INFO  [abc-123] Returning error to client
```

**Benefits:**
1. ✅ Tất cả logs có cùng `[abc-123]` → Easy to trace
2. ✅ Biết error bắt đầu ở UserService database
3. ✅ Thấy error propagate qua AuthService → Gateway
4. ✅ Có full context để fix

---

## 📈 Performance Metrics with Correlation ID

```log
[Gateway]     INFO [abc-123] Request started: 10:30:45.123
[Gateway]     INFO [abc-123] Calling AuthService: 10:30:45.234
[AuthService] INFO [abc-123] Request started: 10:30:45.456
[AuthService] INFO [abc-123] Calling UserService: 10:30:45.678
[UserService] INFO [abc-123] Request started: 10:30:45.789
[UserService] INFO [abc-123] Database query: 10:30:45.890
[UserService] INFO [abc-123] Database result: 10:30:46.012 (122ms)
[UserService] INFO [abc-123] Request completed: 10:30:46.123 (334ms)
[AuthService] INFO [abc-123] Token generation: 10:30:46.345 (111ms)
[AuthService] INFO [abc-123] Request completed: 10:30:46.456 (1000ms)
[Gateway]     INFO [abc-123] Request completed: 10:30:46.678 (1555ms)
```

**Performance Breakdown:**
- Total: **1555ms**
- Gateway overhead: 1555 - 1000 = **555ms**
- AuthService processing: 1000 - 334 = **666ms**
- UserService total: **334ms**
- Database query: **122ms**

---

Generated: 2025-10-02  
Purpose: Visual diagrams for CorrelationIdMiddleware flow  
Status: ✅ Complete
