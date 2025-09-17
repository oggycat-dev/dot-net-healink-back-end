# 🔧 Environment Configuration Guide

This guide explains how to configure environment variables for the Healink Microservices system.

## 📁 File Structure

```
healink-backend/
├── .env                                    # Your environment variables (create this)
├── environment-variables-template.md      # Template with all variables
├── scripts/validate-env.sh               # Validation script
├── docker-compose.yml                    # Uses environment variables
└── src/
    ├── SharedLibrary/Commons/Configs/
    │   └── EnvironmentConfigHelper.cs    # Reads environment variables
    └── [AuthService|UserService]/
        └── appsettings.json              # Fallback configuration
```

## 🚀 Quick Start

### 1. Create Environment File
```bash
# Copy template content to .env
cp environment-variables-template.md .env

# Edit with your values
nano .env
```

### 2. Validate Configuration  
```bash
# Make script executable
chmod +x scripts/validate-env.sh

# Load environment variables
source .env

# Validate all required variables are set
./scripts/validate-env.sh
```

### 3. Start Services
```bash
# All environment variables will be automatically loaded
docker-compose up -d
```

## 📋 Environment Variables Reference

### 🗄️ Database Configuration

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `DB_HOST` | ✅ | - | PostgreSQL host |
| `DB_PORT` | ❌ | `5432` | PostgreSQL port |
| `DB_USER` | ✅ | - | Database username |
| `DB_PASSWORD` | ✅ | - | Database password |
| `AUTH_DB_NAME` | ✅ | - | Auth service database name |
| `USER_DB_NAME` | ✅ | - | User service database name |

### 🔐 JWT Configuration

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `JWT_SECRET_KEY` | ✅ | - | JWT signing key (min 32 chars) |
| `JWT_ISSUER` | ✅ | - | JWT issuer |
| `JWT_AUDIENCE` | ✅ | - | JWT audience |
| `JWT_ACCESS_TOKEN_EXPIRATION` | ❌ | `60` | Access token expiration (minutes) |
| `JWT_REFRESH_TOKEN_EXPIRATION` | ❌ | `7` | Refresh token expiration (days) |

### 🐰 RabbitMQ Configuration

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `RABBITMQ_USER` | ✅ | - | RabbitMQ username |
| `RABBITMQ_PASSWORD` | ✅ | - | RabbitMQ password |
| `RABBITMQ_HOSTNAME` | ❌ | `rabbitmq` | RabbitMQ host |
| `RABBITMQ_PORT` | ❌ | `5672` | RabbitMQ port |
| `RABBITMQ_EXCHANGE` | ❌ | `healink.events` | Exchange name |

### 🔴 Redis Configuration

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `REDIS_PASSWORD` | ✅ | - | Redis password |
| `REDIS_HOST` | ❌ | `redis` | Redis host |
| `REDIS_PORT` | ❌ | `6379` | Redis port |

## 🔄 How It Works

### Configuration Priority
The system loads configuration in this order (highest to lowest priority):

1. **Environment Variables** 🥇
2. **appsettings.json** 🥈  
3. **Default values in code** 🥉

### Service-Specific Variables
Each microservice gets its own database configuration:

```bash
# Auth Service uses these:
AUTH_DB_CONNECTION_STRING=Host=postgres;Database=healink_auth_db;...
AUTH_DB_RETRY_ON_FAILURE=true

# User Service uses these:  
USER_DB_CONNECTION_STRING=Host=postgres;Database=healink_user_db;...
USER_DB_RETRY_ON_FAILURE=true
```

### Automatic Connection String Building
If you don't provide full connection strings, they're built automatically:

```csharp
// EnvironmentConfigHelper.cs builds this:
$"Host={DB_HOST};Port={DB_PORT};Database={AUTH_DB_NAME};Username={DB_USER};Password={DB_PASSWORD}"
```

## 🌍 Environment-Specific Configuration

### Development (.env.development)
```bash
ASPNETCORE_ENVIRONMENT=Development
DATA_ENABLE_AUTO_MIGRATIONS=true
JWT_ACCESS_TOKEN_EXPIRATION=60
OUTBOX_PROCESSING_INTERVAL_SECONDS=10
```

### Staging (.env.staging)  
```bash
ASPNETCORE_ENVIRONMENT=Staging
DATA_ENABLE_AUTO_MIGRATIONS=false
JWT_ACCESS_TOKEN_EXPIRATION=30
OUTBOX_PROCESSING_INTERVAL_SECONDS=30
```

### Production (.env.production)
```bash
ASPNETCORE_ENVIRONMENT=Production
DATA_ENABLE_AUTO_MIGRATIONS=false
JWT_ACCESS_TOKEN_EXPIRATION=15
OUTBOX_PROCESSING_INTERVAL_SECONDS=60
# Use secure values
JWT_SECRET_KEY=your-super-secure-production-key
DB_PASSWORD=your-secure-production-password
```

## 🔒 Security Best Practices

### 1. Secret Management
- ❌ Never commit `.env` files to git
- ✅ Use Azure Key Vault / AWS Secrets Manager in production
- ✅ Rotate secrets regularly
- ✅ Use different secrets per environment

### 2. Password Requirements
- **Minimum length**: 8 characters
- **JWT Secret Key**: At least 32 characters
- **Special characters**: Include symbols for strength

### 3. Access Control
- **Database**: Create separate users per service
- **RabbitMQ**: Use service-specific credentials  
- **Redis**: Enable AUTH and use strong passwords

## 🐛 Troubleshooting

### Common Issues

**1. "Required configuration value 'JWT_SECRET_KEY' is not set"**
```bash
# Solution: Set the missing environment variable
export JWT_SECRET_KEY="your-secret-key-here"
# Or add to .env file
```

**2. "Connection to database failed"**
```bash
# Check database is running
docker-compose ps postgres

# Verify connection string
echo $AUTH_DB_CONNECTION_STRING

# Test connection manually
psql -h localhost -U healink_user -d healink_auth_db
```

**3. "RabbitMQ connection failed"**
```bash
# Check RabbitMQ is running
docker-compose ps rabbitmq

# Verify credentials
docker-compose logs rabbitmq

# Access management UI
open http://localhost:15672
```

### Validation Commands
```bash
# Validate all environment variables
./scripts/validate-env.sh

# Check specific service logs  
docker-compose logs authservice-api
docker-compose logs userservice-api

# Verify environment in container
docker-compose exec authservice-api env | grep JWT
```

## 📚 Related Documentation

- [EnvironmentConfigHelper.cs](../src/SharedLibrary/Commons/Configs/EnvironmentConfigHelper.cs) - Configuration loading logic
- [docker-compose.yml](../docker-compose.yml) - Service environment mapping
- [appsettings.json](../src/AuthService/AuthService.API/appsettings.json) - Fallback configuration
- [Microservices Architecture](./microservices-architecture.md) - Overall system design
