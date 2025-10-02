# Health Check Testing Script for Healink Microservices
# Run this after starting docker-compose to verify all services are healthy

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Healink Microservices Health Check" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Gateway base URL
$gatewayUrl = "http://localhost:5000"

# Services to check
$services = @(
    @{ Name = "AuthService"; Path = "auth" },
    @{ Name = "UserService"; Path = "users" },
    @{ Name = "ContentService"; Path = "content" },
    @{ Name = "SubscriptionService"; Path = "subscription" },
    @{ Name = "PaymentService"; Path = "payment" },
    @{ Name = "NotificationService"; Path = "notification" }
)

$healthyCount = 0
$unhealthyCount = 0

foreach ($service in $services) {
    Write-Host "Testing $($service.Name)..." -ForegroundColor Yellow
    
    try {
        $url = "$gatewayUrl/api/$($service.Path)/health"
        $response = Invoke-RestMethod -Uri $url -Method GET -TimeoutSec 5
        
        if ($response.Status -eq "Healthy") {
            Write-Host "✅ $($service.Name) is HEALTHY" -ForegroundColor Green
            Write-Host "   Service: $($response.Service)" -ForegroundColor Gray
            Write-Host "   Timestamp: $($response.Timestamp)" -ForegroundColor Gray
            $healthyCount++
        }
        else {
            Write-Host "⚠️  $($service.Name) returned unexpected status: $($response.Status)" -ForegroundColor Yellow
            $unhealthyCount++
        }
    }
    catch {
        Write-Host "❌ $($service.Name) is UNHEALTHY or UNREACHABLE" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        $unhealthyCount++
    }
    
    Write-Host ""
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Health Check Summary" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "✅ Healthy Services: $healthyCount" -ForegroundColor Green
Write-Host "❌ Unhealthy Services: $unhealthyCount" -ForegroundColor $(if ($unhealthyCount -eq 0) { "Green" } else { "Red" })
Write-Host "Total Services: $($services.Count)" -ForegroundColor Cyan
Write-Host ""

if ($unhealthyCount -eq 0) {
    Write-Host "🎉 All services are healthy and ready!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "⚠️  Some services are not healthy. Please check the logs." -ForegroundColor Yellow
    Write-Host "Run: docker-compose logs <service-name>" -ForegroundColor Gray
    exit 1
}
