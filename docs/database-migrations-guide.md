# 🗄️ Database Migrations Guide

This guide explains how to run EF Core migrations with environment variables for the Healink Microservices system.

## 🔧 How It Works

### **Environment Variable Priority in Migrations**

When running migrations, the system loads configuration in this priority order:

1. **Environment Variables** 🥇 (Highest Priority)
2. **appsettings.json** 🥈 (Fallback)
3. **Default values** 🥉 (Code defaults)

### **Automatic Service Detection**

The `DbContextFactory` automatically detects which service it's running for:

```csharp
// AuthDbContext -> Uses AUTH_DB_* environment variables
// UserDbContext -> Uses USER_DB_* environment variables
// OrderDbContext -> Uses ORDER_DB_* environment variables
```

## 🚀 Running Migrations

### **1. Set Environment Variables**

```bash
# Method 1: Export environment variables
export AUTH_DB_CONNECTION_STRING="Host=localhost;Database=healink_auth_db;Username=postgres;Password=your_password"
export USER_DB_CONNECTION_STRING="Host=localhost;Database=healink_user_db;Username=postgres;Password=your_password"

# Method 2: Load from .env file
source .env

# Method 3: Use specific variables for migration
export DB_HOST=localhost
export DB_USER=postgres
export DB_PASSWORD=your_password
export AUTH_DB_NAME=healink_auth_db
export USER_DB_NAME=healink_user_db
```

### **2. Add Migration**

```bash
# For Auth Service
cd src/AuthService/AuthService.Infrastructure
dotnet ef migrations add InitialCreate --project ../AuthService.Infrastructure --startup-project ../AuthService.API

# For User Service  
cd src/UserService/UserService.Infrastructure
dotnet ef migrations add InitialCreate --project ../UserService.Infrastructure --startup-project ../UserService.API
```

### **3. Apply Migration**

```bash
# For Auth Service
cd src/AuthService/AuthService.Infrastructure
dotnet ef database update --project ../AuthService.Infrastructure --startup-project ../AuthService.API

# For User Service
cd src/UserService/UserService.Infrastructure
dotnet ef database update --project ../UserService.Infrastructure --startup-project ../UserService.API
```

## 📋 Environment Variables Reference

### **Auth Service (AuthDbContext)**

| Variable | Required | Example | Description |
|----------|----------|---------|-------------|
| `AUTH_DB_CONNECTION_STRING` | ❌ | `Host=localhost;Database=auth_db;...` | Full connection string |
| `AUTH_DB_NAME` | ✅* | `healink_auth_db` | Database name |
| `DB_HOST` | ✅* | `localhost` | Database host |
| `DB_USER` | ✅* | `postgres` | Database user |
| `DB_PASSWORD` | ✅* | `your_password` | Database password |
| `AUTH_DB_RETRY_ON_FAILURE` | ❌ | `true` | Enable retry on failure |

*Required if `AUTH_DB_CONNECTION_STRING` is not provided

### **User Service (UserDbContext)**

| Variable | Required | Example | Description |
|----------|----------|---------|-------------|
| `USER_DB_CONNECTION_STRING` | ❌ | `Host=localhost;Database=user_db;...` | Full connection string |
| `USER_DB_NAME` | ✅* | `healink_user_db` | Database name |
| `DB_HOST` | ✅* | `localhost` | Database host |
| `DB_USER` | ✅* | `postgres` | Database user |
| `DB_PASSWORD` | ✅* | `your_password` | Database password |
| `USER_DB_RETRY_ON_FAILURE` | ❌ | `true` | Enable retry on failure |

## 🐳 Docker Environment

### **Using docker-compose**

```bash
# All environment variables are automatically loaded from docker-compose.yml
docker-compose exec authservice-api dotnet ef database update
docker-compose exec userservice-api dotnet ef database update
```

### **Accessing migration container**

```bash
# Run migration from within container
docker-compose exec authservice-api bash
cd /app
dotnet ef database update --no-build

# Or run directly
docker-compose run --rm authservice-api dotnet ef database update --no-build
```

## 🔧 Configuration Examples

### **Development (.env)**
```bash
# PostgreSQL local development
DB_HOST=localhost
DB_PORT=5432
DB_USER=postgres
DB_PASSWORD=dev_password

AUTH_DB_NAME=healink_auth_dev
USER_DB_NAME=healink_user_dev

# Or use full connection strings
AUTH_DB_CONNECTION_STRING=Host=localhost;Database=healink_auth_dev;Username=postgres;Password=dev_password
USER_DB_CONNECTION_STRING=Host=localhost;Database=healink_user_dev;Username=postgres;Password=dev_password
```

### **Docker Compose (.env)**
```bash
# Using PostgreSQL container
DB_HOST=postgres
DB_PORT=5432
DB_USER=healink_user
DB_PASSWORD=healink_password_2024

AUTH_DB_NAME=healink_auth_db
USER_DB_NAME=healink_user_db
```

### **Production (.env.production)**
```bash
# Production database server
DB_HOST=prod-postgres.company.com
DB_PORT=5432
DB_USER=healink_prod_user
DB_PASSWORD=secure_production_password

AUTH_DB_NAME=healink_auth_prod
USER_DB_NAME=healink_user_prod

# Enable retry for production stability
AUTH_DB_RETRY_ON_FAILURE=true
USER_DB_RETRY_ON_FAILURE=true
AUTH_DB_MAX_RETRY_COUNT=5
USER_DB_MAX_RETRY_COUNT=5
```

## 🔄 Migration Workflow

### **Initial Setup**
```bash
# 1. Set environment variables
source .env

# 2. Create initial migrations
dotnet ef migrations add InitialCreate --project AuthService.Infrastructure --startup-project AuthService.API
dotnet ef migrations add InitialCreate --project UserService.Infrastructure --startup-project UserService.API

# 3. Apply to database
dotnet ef database update --project AuthService.Infrastructure --startup-project AuthService.API
dotnet ef database update --project UserService.Infrastructure --startup-project UserService.API
```

### **Schema Updates**
```bash
# 1. Add new migration
dotnet ef migrations add AddUserProfiles --project UserService.Infrastructure --startup-project UserService.API

# 2. Review generated migration
# Edit migration file if needed

# 3. Apply to database
dotnet ef database update --project UserService.Infrastructure --startup-project UserService.API
```

### **Rollback Migration**
```bash
# Rollback to previous migration
dotnet ef database update PreviousMigrationName --project UserService.Infrastructure --startup-project UserService.API

# Remove last migration
dotnet ef migrations remove --project UserService.Infrastructure --startup-project UserService.API
```

## 🐛 Troubleshooting

### **Common Issues**

**1. "Connection string not found"**
```bash
# Check environment variables
echo $AUTH_DB_CONNECTION_STRING
echo $USER_DB_CONNECTION_STRING

# Verify .env file is loaded
source .env
env | grep DB_
```

**2. "Cannot connect to database"**
```bash
# Test database connection manually
psql -h $DB_HOST -U $DB_USER -d $AUTH_DB_NAME

# Check if database exists
psql -h $DB_HOST -U $DB_USER -c "\l"
```

**3. "Assembly not found"**
```bash
# Make sure you're in the correct directory
cd src/AuthService/AuthService.Infrastructure

# Use full project paths
dotnet ef database update --project . --startup-project ../AuthService.API
```

**4. "Multiple DbContext found"**
```bash
# Specify the DbContext explicitly
dotnet ef database update --context AuthDbContext --project . --startup-project ../AuthService.API
```

### **Debug Commands**
```bash
# List available migrations
dotnet ef migrations list --project AuthService.Infrastructure --startup-project AuthService.API

# Generate SQL script without applying
dotnet ef migrations script --project AuthService.Infrastructure --startup-project AuthService.API

# Check current migration status
dotnet ef migrations has-pending-model-changes --project AuthService.Infrastructure --startup-project AuthService.API
```

## 📝 Best Practices

### **1. Environment Separation**
- Use different databases for dev/staging/prod
- Never run production migrations from development machine
- Use CI/CD pipelines for production migrations

### **2. Migration Safety**
- Always backup database before migrations
- Test migrations on staging environment first
- Review generated migration SQL before applying

### **3. Security**
- Use least-privilege database users for migrations
- Store sensitive connection strings in secret management systems
- Rotate database passwords regularly

### **4. Monitoring**
- Log all migration activities
- Monitor migration performance
- Set up alerts for failed migrations

## 🔗 Related Documentation

- [Environment Configuration Guide](./environment-configuration.md)
- [EnvironmentConfigHelper.cs](../src/SharedLibrary/Commons/Configs/EnvironmentConfigHelper.cs)
- [DbContextFactory.cs](../src/SharedLibrary/Commons/Factories/DbContextFactory.cs)
- [Docker Compose Setup](../docker-compose.yml)
