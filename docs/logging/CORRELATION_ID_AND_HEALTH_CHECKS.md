# Correlation ID và Docker Health Checks

## 🔍 Correlation ID là gì?

**Correlation ID** là một unique identifier (UUID) được gán cho mỗi HTTP request để:
- **Trace request** xuyên suốt các microservices
- **Debug distributed systems** dễ dàng hơn
- **Link logs** từ nhiều services với nhau

### Ví dụ Flow

```
User Request → Gateway → AuthService → UserService → Database
     CorrelationId: abc-123-def
```

Tất cả logs từ Gateway, AuthService, UserService đều có cùng `CorrelationId: abc-123-def`, giúp bạn trace toàn bộ flow của một request.

---

## 🏥 Docker Health Checks

### Logs Bạn Đang Thấy

```
authservice-api: Processing request with CorrelationId: cf7acd4c-9511-41c0-a7f6-3d70db351237
userservice-api: Processing request with CorrelationId: f638d52f-2889-45b1-ac4f-3a0f90f4d54d
```

Đây **KHÔNG PHẢI** là user requests, mà là **Docker health checks tự động**!

### Docker Health Check Configuration

Trong `docker-compose.yml`:

```yaml
healthcheck:
  test: [ "CMD", "curl", "-f", "http://localhost/health" ]
  interval: 30s      # ← Cứ mỗi 30 giây Docker sẽ gọi /health
  timeout: 10s
  retries: 3
  start_period: 40s
```

**Docker sẽ tự động gọi endpoint `/health` mỗi 30 giây** để:
- ✅ Kiểm tra container còn healthy không
- ✅ Auto-restart nếu container unhealthy
- ✅ Load balancer biết service nào available

### Tại Sao Mỗi Service Có CorrelationId Khác Nhau?

Vì **mỗi service nhận health check độc lập** từ Docker:
- Docker → AuthService `/health` → CorrelationId: `cf7acd4c...`
- Docker → UserService `/health` → CorrelationId: `f638d52f...`
- Docker → PaymentService `/health` → CorrelationId: `553b2a6b...`

**Không phải** distributed tracing, chỉ là **local monitoring**.

---

## 📊 So Sánh: User Request vs Health Check

### User Request (Distributed Tracing)
```
Client → Gateway → AuthService → UserService
         [abc-123-def] [abc-123-def] [abc-123-def]
                    ↑ Same CorrelationId
```

**Logs:**
```
Gateway: Processing request with CorrelationId: abc-123-def
  ↳ Path: /api/auth/login
AuthService: Processing request with CorrelationId: abc-123-def
  ↳ Path: /api/auth/verify
UserService: Processing request with CorrelationId: abc-123-def
  ↳ Path: /api/users/123
```

### Health Check (Independent Monitoring)
```
Docker → AuthService   [cf7acd4c...]
Docker → UserService   [f638d52f...]
Docker → PaymentService [553b2a6b...]
         ↑ Different CorrelationIds
```

**Logs:**
```
AuthService: Processing request with CorrelationId: cf7acd4c...
  ↳ Path: /health
UserService: Processing request with CorrelationId: f638d52f...
  ↳ Path: /health
PaymentService: Processing request with CorrelationId: 553b2a6b...
  ↳ Path: /health
```

---

## 🎯 Log Filtering Strategy

### Problem: Too Much Noise

Health checks chạy **mỗi 30 giây** × 6 services = **12 logs/minute** → 720 logs/hour chỉ cho health checks!

### Solution: Filter Health Check Logs

**Updated `CorrelationIdMiddleware.cs`:**

```csharp
// Check if this is a health check request
var isHealthCheck = context.Request.Path.StartsWithSegments("/health");

if (!isHealthCheck)
{
    _logger.LogInformation("Processing request with CorrelationId: {CorrelationId}", correlationId);
}
else
{
    // Health checks only logged at Debug level
    _logger.LogDebug("Health check request with CorrelationId: {CorrelationId}", correlationId);
}
```

### Benefits

✅ **Production logs clean** - Chỉ hiện user requests  
✅ **Debug logs có health checks** - Bật khi troubleshoot  
✅ **Performance** - Ít I/O operations cho logging  

---

## 🔧 Configuration Options

### Option 1: Reduce Health Check Frequency (NOT Recommended)

```yaml
healthcheck:
  interval: 120s  # Từ 30s → 120s (mỗi 2 phút)
```

❌ **Downside:** Phát hiện chậm khi service down

### Option 2: Disable Health Check Logging (✅ Recommended)

```csharp
// In CorrelationIdMiddleware
var isHealthCheck = context.Request.Path.StartsWithSegments("/health");
if (!isHealthCheck)
{
    _logger.LogInformation("Processing request...");
}
```

✅ **Best practice:** Keep health checks running, just filter logs

### Option 3: Separate Health Check Endpoint

```csharp
// Create lightweight health endpoint without middleware
app.MapGet("/healthz", () => Results.Ok(new { status = "UP" }));
```

---

## 🚀 Production Best Practices

### 1. Log Levels Configuration

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SharedLibrary.Commons.Middlewares.CorrelationIdMiddleware": "Information"
    }
  }
}
```

**appsettings.Production.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "SharedLibrary.Commons.Middlewares.CorrelationIdMiddleware": "Warning"
    }
  }
}
```

### 2. Structured Logging

```csharp
_logger.LogInformation(
    "Request completed: {CorrelationId} | {Method} {Path} | {StatusCode} | {Duration}ms",
    correlationId,
    context.Request.Method,
    context.Request.Path,
    context.Response.StatusCode,
    stopwatch.ElapsedMilliseconds
);
```

### 3. Distributed Tracing Tools

For production, consider:
- **Jaeger** - Open-source distributed tracing
- **Zipkin** - Distributed tracing system
- **Application Insights** - Azure native monitoring
- **Elastic APM** - Part of Elastic Stack

---

## 📝 Summary

| Aspect | Health Check | User Request |
|--------|-------------|--------------|
| **Source** | Docker daemon | External client |
| **Frequency** | Every 30s (automatic) | On-demand |
| **CorrelationId** | Different each time | Same across services |
| **Purpose** | Container monitoring | Business logic |
| **Log Level** | Debug (filtered) | Information |
| **Tracing** | Local only | Distributed |

### Key Takeaways

1. ✅ Health check logs = **Good sign** (Docker monitoring working)
2. ✅ Different CorrelationIds = **Normal** (independent health checks)
3. ✅ Filter at Debug level = **Clean production logs**
4. ✅ Keep health checks enabled = **Production reliability**

---

## 🔗 Related Files

- `CorrelationIdMiddleware.cs` - Correlation ID handling
- `docker-compose.yml` - Health check configuration
- `HealthController.cs` - Health check endpoints (each service)
- `test-health-checks.ps1` - Manual health check testing

---

Generated: 2025-10-02  
Status: ✅ Production Ready  
Services: 6 Microservices with Health Checks
