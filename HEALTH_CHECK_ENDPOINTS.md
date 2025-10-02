# Health Check Endpoints Documentation

## Overview
Tất cả các microservices trong hệ thống Healink đều có health check endpoints để monitor trạng thái hoạt động.

## Gateway Access (Recommended)
Truy cập health checks qua API Gateway (Port 5000):

### 1. AuthService
```bash
GET http://localhost:5000/api/auth/health
```
**Response:**
```json
{
  "Status": "Healthy",
  "Service": "AuthService",
  "Timestamp": "2025-10-01T12:00:00.000Z"
}
```

### 2. UserService
```bash
GET http://localhost:5000/api/users/health
```
**Response:**
```json
{
  "Status": "Healthy",
  "Service": "UserService",
  "Timestamp": "2025-10-01T12:00:00.000Z"
}
```

### 3. ContentService
```bash
GET http://localhost:5000/api/content/health
```
**Response:**
```json
{
  "Status": "Healthy",
  "Service": "ContentService",
  "Timestamp": "2025-10-01T12:00:00.000Z"
}
```

### 4. SubscriptionService ✨ NEW
```bash
GET http://localhost:5000/api/subscription/health
```
**Response:**
```json
{
  "Status": "Healthy",
  "Service": "SubscriptionService",
  "Timestamp": "2025-10-01T12:00:00.000Z"
}
```

### 5. PaymentService ✨ NEW
```bash
GET http://localhost:5000/api/payment/health
```
**Response:**
```json
{
  "Status": "Healthy",
  "Service": "PaymentService",
  "Timestamp": "2025-10-01T12:00:00.000Z"
}
```

### 6. NotificationService ✨ NEW
```bash
GET http://localhost:5000/api/notification/health
```
**Response:**
```json
{
  "Status": "Healthy",
  "Service": "NotificationService",
  "Timestamp": "2025-10-01T12:00:00.000Z"
}
```

## Direct Service Access
Truy cập trực tiếp vào từng service (Development only):

| Service | Port | Direct Health Check URL |
|---------|------|-------------------------|
| Gateway | 5000 | N/A (Gateway không có health endpoint riêng) |
| AuthService | 5001 | http://localhost:5001/health |
| UserService | 5002 | http://localhost:5002/health |
| NotificationService | 5003 | http://localhost:5003/api/health |
| ContentService | 5004 | http://localhost:5004/api/health |
| SubscriptionService | 5005 | http://localhost:5005/api/health |
| PaymentService | 5006 | http://localhost:5006/api/health |

## Docker Health Checks
Docker container health checks được cấu hình trong `docker-compose.yml`:

### AuthService, UserService, ContentService, SubscriptionService, PaymentService
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

### NotificationService
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

## Ocelot Gateway Routes
Health check routes được cấu hình trong `ocelot.json`:

```json
{
  "Routes": [
    // AuthService Health
    {
      "DownstreamPathTemplate": "/health",
      "DownstreamHostAndPorts": [{ "Host": "authservice-api", "Port": 80 }],
      "UpstreamPathTemplate": "/api/auth/health"
    },
    // UserService Health
    {
      "DownstreamPathTemplate": "/health",
      "DownstreamHostAndPorts": [{ "Host": "userservice-api", "Port": 80 }],
      "UpstreamPathTemplate": "/api/users/health"
    },
    // ContentService Health
    {
      "DownstreamPathTemplate": "/api/health",
      "DownstreamHostAndPorts": [{ "Host": "contentservice-api", "Port": 80 }],
      "UpstreamPathTemplate": "/api/content/health"
    },
    // SubscriptionService Health ✨ NEW
    {
      "DownstreamPathTemplate": "/api/health",
      "DownstreamHostAndPorts": [{ "Host": "subscriptionservice-api", "Port": 80 }],
      "UpstreamPathTemplate": "/api/subscription/health"
    },
    // PaymentService Health ✨ NEW
    {
      "DownstreamPathTemplate": "/api/health",
      "DownstreamHostAndPorts": [{ "Host": "paymentservice-api", "Port": 80 }],
      "UpstreamPathTemplate": "/api/payment/health"
    },
    // NotificationService Health ✨ NEW
    {
      "DownstreamPathTemplate": "/api/health",
      "DownstreamHostAndPorts": [{ "Host": "notificationservice-api", "Port": 80 }],
      "UpstreamPathTemplate": "/api/notification/health"
    }
  ]
}
```

## Testing Health Checks

### PowerShell Testing Script
```powershell
# Test all health checks via Gateway
$services = @(
    "auth",
    "users", 
    "content",
    "subscription",
    "payment",
    "notification"
)

foreach ($service in $services) {
    Write-Host "Testing $service health..." -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5000/api/$service/health" -Method GET
        Write-Host "✅ $service is healthy" -ForegroundColor Green
        $response | ConvertTo-Json
    }
    catch {
        Write-Host "❌ $service health check failed" -ForegroundColor Red
        Write-Host $_.Exception.Message
    }
    Write-Host ""
}
```

### Bash Testing Script
```bash
#!/bin/bash
# Test all health checks via Gateway

services=("auth" "users" "content" "subscription" "payment" "notification")

for service in "${services[@]}"; do
    echo "Testing $service health..."
    curl -s http://localhost:5000/api/$service/health | jq '.'
    echo ""
done
```

### Docker Compose Health Status
```bash
# Check health status of all containers
docker-compose ps

# Check specific service logs
docker-compose logs subscriptionservice-api
docker-compose logs paymentservice-api
docker-compose logs notificationservice-api
```

## Health Check Implementation

### Controller Pattern
Tất cả services sử dụng cùng một pattern cho HealthController:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace ServiceName.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new 
        { 
            Status = "Healthy", 
            Service = "ServiceName", 
            Timestamp = DateTime.UtcNow 
        });
    }
}
```

### Why NotificationService Needs Health Check?
Dù NotificationService không lưu trữ data vào database, nhưng health check vẫn cần thiết để:

1. **Container Orchestration**: Docker cần biết service đã ready chưa
2. **Service Discovery**: Gateway cần verify service availability
3. **Load Balancing**: Health checks giúp load balancer route traffic đúng
4. **Monitoring**: Ops team cần monitor service uptime
5. **Dependency Management**: Các services khác depend on NotificationService cần biết nó có available không

## Monitoring Integration

### Prometheus Metrics (Future Enhancement)
```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'healink-services'
    static_configs:
      - targets: 
        - 'authservice-api:80'
        - 'userservice-api:80'
        - 'contentservice-api:80'
        - 'subscriptionservice-api:80'
        - 'paymentservice-api:80'
        - 'notificationservice-api:80'
    metrics_path: '/api/health'
```

### Grafana Dashboard
- Service Uptime
- Response Times
- Error Rates
- Request Counts

## Troubleshooting

### Service Not Healthy
1. Check container logs: `docker-compose logs <service-name>`
2. Check database connection
3. Check RabbitMQ connection
4. Check Redis connection
5. Verify environment variables

### Gateway Cannot Reach Service
1. Check docker network: `docker network inspect dot-net-healink-back-end_healink-network`
2. Verify service name in ocelot.json matches docker-compose service name
3. Check service port configuration
4. Verify service is running: `docker-compose ps`

### Health Check Timeout
1. Increase `start_period` in docker-compose.yml
2. Check service startup time
3. Verify dependencies are ready first
4. Check resource constraints (CPU/Memory)

## Summary

✅ **All 6 microservices now have health check endpoints**
✅ **Ocelot Gateway routes configured for all health checks**
✅ **Docker health checks enabled for container orchestration**
✅ **Consistent health check patterns across all services**
✅ **NotificationService now has HealthController despite not storing DB records**

Services ready for monitoring, orchestration, and production deployment!