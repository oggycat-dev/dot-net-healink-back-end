# 🌐 Gateway Environment Configuration Guide

This guide explains how the API Gateway has been updated to use environment variables consistently with other microservices.

## 🔄 What Changed

### **Before (Hardcoded Configuration)**
```csharp
// Gateway used hardcoded values in appsettings.json
var jwtConfig = builder.Configuration.GetSection("JwtConfig").Get<JwtConfig>();
builder.Services.Configure<CacheConfig>(configuration.GetSection("Cache"));
```

### **After (Environment-Based Configuration)**
```csharp
// Gateway now uses EnvironmentConfigHelper like other services
builder.Services.AddEnvironmentBasedConfigurations(builder.Configuration);
var jwtConfig = EnvironmentConfigHelper.GetJwtConfig(builder.Configuration);
```

## 📋 Environment Variables for Gateway

### **Required Variables**
| Variable | Description | Example |
|----------|-------------|---------|
| `JWT_SECRET_KEY` | JWT signing key | `YourSecretKey...` |
| `JWT_ISSUER` | JWT issuer | `HealinkAuthService` |
| `JWT_AUDIENCE` | JWT audience | `HealinkClients` |
| `REDIS_CONNECTION_STRING` | Redis connection | `redis:6379,password=...` |
| `RABBITMQ_USER` | RabbitMQ username | `healink_rabbitmq` |
| `RABBITMQ_PASSWORD` | RabbitMQ password | `password123` |

### **Optional Variables (with defaults)**
| Variable | Default | Description |
|----------|---------|-------------|
| `JWT_ACCESS_TOKEN_EXPIRATION` | `60` | Access token expiration (minutes) |
| `JWT_REFRESH_TOKEN_EXPIRATION` | `7` | Refresh token expiration (days) |
| `JWT_CLOCK_SKEW` | `5` | Clock skew tolerance (minutes) |
| `CACHE_USER_STATE_MINUTES` | `30` | User state cache duration |
| `CACHE_MAX_SIZE` | `10000` | Maximum cache size |
| `AUTH_SERVICE_URL` | `http://authservice-api` | Auth service internal URL |
| `USER_SERVICE_URL` | `http://userservice-api` | User service internal URL |

## 🔧 Updated Files

### **1. Gateway/Gateway.API/Program.cs**
```csharp
// NEW: Uses environment-based configuration
builder.Services.AddEnvironmentBasedConfigurations(builder.Configuration);
var jwtConfig = EnvironmentConfigHelper.GetJwtConfig(builder.Configuration);

// NEW: Environment-based service URL
var authServiceUrl = Environment.GetEnvironmentVariable("AUTH_SERVICE_URL") ?? "http://authservice-api";
builder.Services.AddHttpClient("AuthService", client =>
{
    client.BaseAddress = new Uri(authServiceUrl);
});
```

### **2. Gateway/Gateway.API/appsettings.json**
```json
{
  "JwtConfig": {
    "Key": "${JWT_SECRET_KEY}",
    "Issuer": "${JWT_ISSUER}",
    "Audience": "${JWT_AUDIENCE}"
  },
  "Cache": {
    "UserStateCacheMinutes": "${CACHE_USER_STATE_MINUTES:30}",
    "MaxCacheSize": "${CACHE_MAX_SIZE:10000}"
  },
  "ServiceUrls": {
    "AuthService": "${AUTH_SERVICE_URL:http://authservice-api}",
    "UserService": "${USER_SERVICE_URL:http://userservice-api}"
  }
}
```

### **3. docker-compose.yml**
```yaml
gateway-api:
  environment:
    # JWT Configuration  
    JWT_SECRET_KEY: ${JWT_SECRET_KEY}
    JWT_ISSUER: ${JWT_ISSUER}
    JWT_AUDIENCE: ${JWT_AUDIENCE}
    
    # Cache Configuration
    CACHE_USER_STATE_MINUTES: ${CACHE_USER_STATE_MINUTES:-30}
    CACHE_MAX_SIZE: ${CACHE_MAX_SIZE:-10000}
    
    # Service URLs
    AUTH_SERVICE_URL: "http://authservice-api"
    USER_SERVICE_URL: "http://userservice-api"
```

### **4. SharedLibrary/Commons/Extensions/DistributedAuthExtensions.cs**
```csharp
// UPDATED: Uses EnvironmentConfigHelper for cache and Redis config
public static IServiceCollection AddGatewayDistributedAuth(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var cacheConfig = EnvironmentConfigHelper.GetCacheConfig(configuration);
    var redisConfig = EnvironmentConfigHelper.GetRedisConfig(configuration);
    
    services.AddSingleton(cacheConfig);
    services.AddSingleton(redisConfig);
    // ...
}
```

## 🌍 Environment-Specific Configuration

### **Development (.env)**
```bash
# Gateway configuration
JWT_SECRET_KEY=DevSecretKeyForLocalDevelopment
JWT_ISSUER=HealinkAuthService  
JWT_AUDIENCE=HealinkClients
JWT_ACCESS_TOKEN_EXPIRATION=60

# Redis
REDIS_CONNECTION_STRING=localhost:6379

# Cache settings
CACHE_USER_STATE_MINUTES=30
CACHE_MAX_SIZE=1000

# Service URLs (for development)
AUTH_SERVICE_URL=http://localhost:5001
USER_SERVICE_URL=http://localhost:5002
```

### **Docker (.env)**
```bash
# Gateway configuration (same JWT as other services)
JWT_SECRET_KEY=YourProductionSecretKey
JWT_ISSUER=HealinkAuthService
JWT_AUDIENCE=HealinkClients

# Redis (container network)
REDIS_CONNECTION_STRING=redis:6379,password=redis_password_2024

# Cache settings
CACHE_USER_STATE_MINUTES=30
CACHE_MAX_SIZE=10000

# Service URLs (container network)
AUTH_SERVICE_URL=http://authservice-api
USER_SERVICE_URL=http://userservice-api
```

### **Production (.env.production)**
```bash
# Gateway configuration
JWT_SECRET_KEY=YourSuperSecureProductionKey
JWT_ISSUER=HealinkAuthService
JWT_AUDIENCE=HealinkClients
JWT_ACCESS_TOKEN_EXPIRATION=15

# Redis (production server)
REDIS_CONNECTION_STRING=prod-redis.company.com:6379,password=secure_password

# Cache settings (optimized for production)
CACHE_USER_STATE_MINUTES=60
CACHE_MAX_SIZE=50000

# Service URLs (production)
AUTH_SERVICE_URL=http://authservice.internal
USER_SERVICE_URL=http://userservice.internal
```

## 🔄 Configuration Priority

Gateway now follows the same priority as other services:

1. **Environment Variables** 🥇 (Highest Priority)
2. **appsettings.json placeholders** 🥈 (Fallback)
3. **Code defaults** 🥉 (Last resort)

## ✅ Benefits Achieved

### **1. Consistency**
- Gateway uses same environment variables as AuthService and UserService
- Unified configuration management across all microservices
- Same JWT configuration everywhere

### **2. Environment Isolation**
- Dev/staging/prod can use different .env files
- No hardcoded values in configuration files
- Secure secrets management

### **3. Docker Integration**
- Seamless integration with docker-compose.yml
- Environment variables automatically passed to containers
- No need for separate Docker configuration files

### **4. Maintainability**
- Single source of truth for configuration values
- Easy to update JWT secrets or service URLs
- Centralized environment variable management

## 🔍 Validation

### **Test Environment Variables Loading**
```bash
# 1. Set environment variables
export JWT_SECRET_KEY="TestSecretKey"
export JWT_ISSUER="TestIssuer"  
export REDIS_CONNECTION_STRING="localhost:6379"

# 2. Start Gateway
cd src/Gateway/Gateway.API
dotnet run

# 3. Check logs for environment-based configuration loading
```

### **Test with Docker**
```bash
# 1. Create .env file with all required variables
# 2. Start services
docker-compose up gateway-api

# 3. Verify Gateway can communicate with other services
curl http://localhost:5000/health
```

## 🔗 Related Documentation

- [Environment Configuration Guide](./environment-configuration.md) - General environment setup
- [Microservices Architecture](./microservices-architecture.md) - Overall system design
- [Database Migrations Guide](./database-migrations-guide.md) - Database environment setup
