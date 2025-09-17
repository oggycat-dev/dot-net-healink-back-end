# ⚙️ Configuration Loading Guide

This guide explains how ASP.NET Core loads configuration files and how our environment variable system works.

## 🔄 ASP.NET Core Configuration Loading Order

ASP.NET Core loads configuration in this **priority order** (later sources override earlier ones):

### **1. Default Sources (Lowest Priority)**
```csharp
// Built-in providers
- appsettings.json (always loaded)
- appsettings.{Environment}.json (based on ASPNETCORE_ENVIRONMENT)
- User secrets (Development only)
- Environment variables (highest priority for same key)
- Command line arguments (highest priority)
```

### **2. Environment-Specific Files**
```bash
# Environment Variable
ASPNETCORE_ENVIRONMENT=Development

# Files loaded in order:
1. appsettings.json
2. appsettings.Development.json  # Overrides appsettings.json
```

### **3. Docker Environment**
```yaml
# docker-compose.yml
environment:
  ASPNETCORE_ENVIRONMENT: Development  # Loads appsettings.Development.json

# ❌ WRONG: appsettings.Docker.json is NOT automatically loaded
# ✅ CORRECT: Use appsettings.Development.json for Docker
```

## 🐰 RabbitMQ Configuration Mapping

### **Environment Variables → RabbitMQConfig**
```bash
# Environment Variables (in .env or docker-compose.yml)
RABBITMQ_HOSTNAME=rabbitmq
RABBITMQ_PORT=5672
RABBITMQ_USER=healink_rabbitmq
RABBITMQ_PASSWORD=rabbitmq_password_2024
RABBITMQ_VHOST=/
RABBITMQ_EXCHANGE=healink.events
RABBITMQ_QUEUE_NAME=
RABBITMQ_DURABLE=true
RABBITMQ_AUTO_DELETE=false
RABBITMQ_RETRY_COUNT=3
RABBITMQ_RETRY_DELAY=5000              # milliseconds
RABBITMQ_AUTO_ACK=false
RABBITMQ_PREFETCH_COUNT=10
RABBITMQ_CONNECTION_TIMEOUT=30000      # milliseconds
RABBITMQ_HEARTBEAT=60                  # seconds
RABBITMQ_ENABLE_SSL=false
RABBITMQ_SSL_SERVER_NAME=
```

### **appsettings.json Format**
```json
{
  "RabbitMQ": {
    "HostName": "${RABBITMQ_HOSTNAME:localhost}",
    "Port": "${RABBITMQ_PORT:5672}",
    "UserName": "${RABBITMQ_USER}",
    "Password": "${RABBITMQ_PASSWORD}",
    "VirtualHost": "${RABBITMQ_VHOST:/}",
    "ExchangeName": "${RABBITMQ_EXCHANGE:healink.events}",
    "QueueName": "${RABBITMQ_QUEUE_NAME:}",
    "Durable": "${RABBITMQ_DURABLE:true}",
    "AutoDelete": "${RABBITMQ_AUTO_DELETE:false}",
    "RetryCount": "${RABBITMQ_RETRY_COUNT:3}",
    "RetryDelay": "${RABBITMQ_RETRY_DELAY:5000}",
    "AutoAck": "${RABBITMQ_AUTO_ACK:false}",
    "PrefetchCount": "${RABBITMQ_PREFETCH_COUNT:10}",
    "ConnectionTimeout": "${RABBITMQ_CONNECTION_TIMEOUT:30000}",
    "RequestedHeartbeat": "${RABBITMQ_HEARTBEAT:60}",
    "EnableSsl": "${RABBITMQ_ENABLE_SSL:false}",
    "SslServerName": "${RABBITMQ_SSL_SERVER_NAME:}"
  }
}
```

### **docker-compose.yml Format**
```yaml
# AuthService and UserService (appsettings.json binding)
environment:
  RabbitMQ__HostName: rabbitmq
  RabbitMQ__Port: ${RABBITMQ_PORT:-5672}
  RabbitMQ__UserName: ${RABBITMQ_USER}
  RabbitMQ__Password: ${RABBITMQ_PASSWORD}
  RabbitMQ__VirtualHost: ${RABBITMQ_VHOST:-/}
  RabbitMQ__ExchangeName: ${RABBITMQ_EXCHANGE}
  RabbitMQ__RetryCount: ${RABBITMQ_RETRY_COUNT:-3}
  RabbitMQ__RetryDelay: ${RABBITMQ_RETRY_DELAY:-5000}

# Gateway (EnvironmentConfigHelper direct loading)
environment:
  RABBITMQ_HOSTNAME: rabbitmq
  RABBITMQ_PORT: ${RABBITMQ_PORT:-5672}
  RABBITMQ_USER: ${RABBITMQ_USER}
  RABBITMQ_PASSWORD: ${RABBITMQ_PASSWORD}
  RABBITMQ_VHOST: ${RABBITMQ_VHOST:-/}
  RABBITMQ_EXCHANGE: ${RABBITMQ_EXCHANGE}
```

## 🔧 Configuration Loading Methods

### **Method 1: Traditional IConfiguration Binding**
```csharp
// Used by AuthService and UserService
services.Configure<RabbitMQConfig>(configuration.GetSection("RabbitMQ"));

// Reads from appsettings.json RabbitMQ section
// Environment variables override through ${VAR_NAME:default} syntax
```

### **Method 2: EnvironmentConfigHelper (Gateway)**
```csharp
// Used by Gateway
var rabbitMQConfig = EnvironmentConfigHelper.GetRabbitMQConfig(configuration);
services.AddSingleton(rabbitMQConfig);

// Directly reads environment variables with fallbacks
// More flexible but requires explicit mapping
```

## 📊 Configuration Priority Matrix

| Source | Priority | AuthService | UserService | Gateway |
|--------|----------|-------------|-------------|---------|
| **Environment Variables** | 🥇 Highest | ✅ Via ${VAR} | ✅ Via ${VAR} | ✅ Direct |
| **appsettings.{Environment}.json** | 🥈 High | ✅ Development | ✅ Development | ✅ Development |
| **appsettings.json** | 🥉 Medium | ✅ Base config | ✅ Base config | ✅ Base config |
| **Code Defaults** | 🥉 Lowest | ✅ In helpers | ✅ In helpers | ✅ In helpers |

## 🚀 Best Practices

### **1. Environment Variable Naming**
```bash
# ✅ GOOD: Consistent naming
RABBITMQ_HOSTNAME=rabbitmq
RABBITMQ_PORT=5672
RABBITMQ_USER=healink_rabbitmq

# ❌ BAD: Inconsistent naming
RABBIT_HOST=rabbitmq
RMQ_PORT=5672
RABBITMQ_USERNAME=user
```

### **2. Default Values**
```json
// ✅ GOOD: Provide sensible defaults
"HostName": "${RABBITMQ_HOSTNAME:localhost}",
"Port": "${RABBITMQ_PORT:5672}",
"RetryCount": "${RABBITMQ_RETRY_COUNT:3}"

// ❌ BAD: No defaults for optional values
"HostName": "${RABBITMQ_HOSTNAME}",
"Port": "${RABBITMQ_PORT}",
"RetryCount": "${RABBITMQ_RETRY_COUNT}"
```

### **3. Docker Environment Variables**
```yaml
# ✅ GOOD: Use appsettings.json binding format
environment:
  RabbitMQ__HostName: rabbitmq        # Double underscore for nested config
  RabbitMQ__Port: 5672

# ✅ ALSO GOOD: Use direct environment variables  
environment:
  RABBITMQ_HOSTNAME: rabbitmq         # For EnvironmentConfigHelper
  RABBITMQ_PORT: 5672
```

### **4. Unit Conversion Handling**
```csharp
// ✅ GOOD: Handle unit conversions in EnvironmentConfigHelper
RetryDelaySeconds = GetIntValue("RABBITMQ_RETRY_DELAY", config, 5000) / 1000, // ms to seconds
ConnectionTimeoutSeconds = GetIntValue("RABBITMQ_CONNECTION_TIMEOUT", config, 30000) / 1000, // ms to seconds

// Environment: RABBITMQ_RETRY_DELAY=5000 (milliseconds)
// Config Class: RetryDelaySeconds=5 (seconds)
```

## 🔍 Troubleshooting

### **Configuration Not Loading**
```bash
# 1. Check environment variable names
echo $RABBITMQ_HOSTNAME

# 2. Check Docker environment
docker exec container-name env | grep RABBITMQ

# 3. Check appsettings.json syntax
cat appsettings.json | jq .  # Validate JSON
```

### **Environment Variables Not Working**
```bash
# ❌ PROBLEM: Wrong naming
RABBIT_HOST=rabbitmq

# ✅ SOLUTION: Correct naming
RABBITMQ_HOSTNAME=rabbitmq

# ❌ PROBLEM: Missing in docker-compose.yml
environment:
  ASPNETCORE_ENVIRONMENT: Development

# ✅ SOLUTION: Add to docker-compose.yml
environment:
  ASPNETCORE_ENVIRONMENT: Development
  RABBITMQ_HOSTNAME: rabbitmq
  RABBITMQ_USER: ${RABBITMQ_USER}
```

### **appsettings.Docker.json Not Loading**
```csharp
// ❌ PROBLEM: ASP.NET Core doesn't automatically load appsettings.Docker.json
// ASPNETCORE_ENVIRONMENT=Development loads appsettings.Development.json

// ✅ SOLUTION: Use appsettings.Development.json for Docker
// Or: Explicitly add in Program.cs
builder.Configuration.AddJsonFile("appsettings.Docker.json", optional: true);
```

## 📝 Configuration Validation

### **Startup Validation**
```csharp
// Add to Program.cs for debugging
var config = builder.Configuration;
var rabbitMQSection = config.GetSection("RabbitMQ");
Console.WriteLine($"RabbitMQ HostName: {rabbitMQSection["HostName"]}");
Console.WriteLine($"RabbitMQ Port: {rabbitMQSection["Port"]}");
```

### **Environment Variable Testing**
```bash
# Test environment variable loading
docker-compose exec authservice-api env | grep RABBITMQ
docker-compose exec gateway-api env | grep RABBITMQ

# Test configuration loading
docker-compose logs authservice-api | grep -i rabbitmq
docker-compose logs gateway-api | grep -i rabbitmq
```

## 🔗 Related Documentation

- [Environment Variables Template](../environment-variables-template.md)
- [EnvironmentConfigHelper.cs](../src/SharedLibrary/Commons/Configs/EnvironmentConfigHelper.cs)
- [Docker Compose Configuration](../docker-compose.yml)
- [RabbitMQ Configuration](../src/SharedLibrary/Commons/Configs/RabbitMQConfig.cs)
