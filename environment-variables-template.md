# Healink Microservices Environment Variables Template

Copy nội dung bên dưới vào file `.env` trong root directory của project.

```bash
# ===================================
# HEALINK MICROSERVICES ENVIRONMENT VARIABLES
# Copy this content to .env file and update values for your environment
# ===================================

# ===================================
# DATABASE CONFIGURATION
# ===================================
# PostgreSQL Database Host and Port
DB_HOST=postgres
DB_PORT=5432
DB_USER=healink_user
DB_PASSWORD=healink_password_2024

# Service-specific database names (used by docker-compose and EnvironmentConfigHelper)
AUTH_DB_NAME=healink_auth_db
USER_DB_NAME=healink_user_db

# Full connection strings (auto-generated from components above if not set)
# These take precedence over individual components
AUTH_DB_CONNECTION_STRING=Host=postgres;Database=healink_auth_db;Username=healink_user;Password=healink_password_2024
USER_DB_CONNECTION_STRING=Host=postgres;Database=healink_user_db;Username=healink_user;Password=healink_password_2024

# Database retry configuration (used by EnvironmentConfigHelper)
AUTH_DB_RETRY_ON_FAILURE=true
AUTH_DB_MAX_RETRY_COUNT=3
AUTH_DB_MAX_RETRY_DELAY=30

USER_DB_RETRY_ON_FAILURE=true
USER_DB_MAX_RETRY_COUNT=3
USER_DB_MAX_RETRY_DELAY=30

# ===================================
# JWT CONFIGURATION
# Used by EnvironmentConfigHelper.GetJwtConfig()
# ===================================
JWT_SECRET_KEY=YourSuperSecretJwtKeyThatShouldBeAtLeast256BitsLongForHS256Algorithm
JWT_ISSUER=HealinkAuthService
JWT_AUDIENCE=HealinkClients
JWT_ACCESS_TOKEN_EXPIRATION=60
JWT_REFRESH_TOKEN_EXPIRATION=7
JWT_CLOCK_SKEW=5

# ===================================
# RABBITMQ MESSAGE BROKER CONFIGURATION
# Used by EnvironmentConfigHelper.GetRabbitMQConfig()
# ===================================
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
RABBITMQ_RETRY_DELAY=5000
RABBITMQ_AUTO_ACK=false
RABBITMQ_PREFETCH_COUNT=10
RABBITMQ_CONNECTION_TIMEOUT=30000
RABBITMQ_HEARTBEAT=60
RABBITMQ_ENABLE_SSL=false
RABBITMQ_SSL_SERVER_NAME=

# ===================================
# REDIS CACHE CONFIGURATION
# Used by EnvironmentConfigHelper.GetRedisConfig()
# ===================================
REDIS_HOST=redis
REDIS_PORT=6379
REDIS_PASSWORD=redis_password_2024
REDIS_CONNECTION_STRING=redis:6379,password=redis_password_2024

# ===================================
# CACHE CONFIGURATION
# Used by EnvironmentConfigHelper.GetCacheConfig()
# ===================================
CACHE_USER_STATE_MINUTES=30
CACHE_USER_STATE_SLIDING_MINUTES=15
CACHE_ACTIVE_USERS_HOURS=24
CACHE_CLEANUP_INTERVAL_MINUTES=60
CACHE_MAX_SIZE=10000

# ===================================
# OUTBOX EVENT PROCESSING CONFIGURATION
# Used by EnvironmentConfigHelper.GetOutboxConfig()
# ===================================
OUTBOX_PROCESSING_INTERVAL_SECONDS=30
OUTBOX_BATCH_SIZE=100
OUTBOX_MAX_RETRY_ATTEMPTS=3
OUTBOX_ENABLED=true
OUTBOX_PROCESSING_TIMEOUT_SECONDS=300

# ===================================
# CORS CONFIGURATION
# Used by EnvironmentConfigHelper.GetCorsConfig()
# ===================================
CORS_ALLOWED_ORIGINS=http://localhost:3000,http://localhost:3001,http://localhost:5173,http://localhost:4200
CORS_ALLOWED_METHODS=GET,POST,PUT,DELETE,OPTIONS
CORS_ALLOWED_HEADERS=*
CORS_ALLOW_CREDENTIALS=true

# ===================================
# DATA & MIGRATION CONFIGURATION
# Used by EnvironmentConfigHelper.GetDataConfig()
# ===================================
DATA_ENABLE_AUTO_MIGRATIONS=true

# ===================================
# ADMIN ACCOUNT CONFIGURATION
# Used by both AuthService and UserService for admin seeding
# ===================================
ADMIN_EMAIL=admin@healink.com
ADMIN_PASSWORD=HealinkAdmin2024!
DEFAULT_ADMIN_USER_ID=00000000-0000-0000-0000-000000000001

# ===================================
# ASPNETCORE CONFIGURATION
# Used by all ASP.NET Core services
# ===================================
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:80

# ===================================
# SERVICE URLS (Internal Communication)
# Used by API Gateway and inter-service communication
# ===================================
AUTH_SERVICE_URL=http://authservice-api
USER_SERVICE_URL=http://userservice-api
GATEWAY_SERVICE_URL=http://gateway-api

# ===================================
# JWT VALIDATION CONFIGURATION (Optional overrides)
# Used for fine-tuning JWT validation behavior
# ===================================
JWT_VALIDATE_ISSUER=true
JWT_VALIDATE_AUDIENCE=true
JWT_VALIDATE_LIFETIME=true
JWT_VALIDATE_ISSUER_SIGNING_KEY=true
```

## 📋 Usage Instructions

### 1. **Create .env file:**
```bash
# Copy template to .env
cp environment-variables-template.md .env
# Edit values as needed
nano .env
```

### 2. **Start services:**
```bash
# All environment variables will be automatically loaded
docker-compose up -d
```

### 3. **Override for different environments:**

**Development (.env.development):**
```bash
ASPNETCORE_ENVIRONMENT=Development
DATA_ENABLE_AUTO_MIGRATIONS=true
JWT_ACCESS_TOKEN_EXPIRATION=60
OUTBOX_PROCESSING_INTERVAL_SECONDS=10
```

**Production (.env.production):**
```bash
ASPNETCORE_ENVIRONMENT=Production
DATA_ENABLE_AUTO_MIGRATIONS=false
JWT_ACCESS_TOKEN_EXPIRATION=15
OUTBOX_PROCESSING_INTERVAL_SECONDS=60
# Use strong passwords and secrets
JWT_SECRET_KEY=your-production-secret-key
DB_PASSWORD=your-production-db-password
```

## 🔄 How It Works

### **Environment Variable Priority:**
1. **Environment Variables** (highest priority)
2. **appsettings.json** (fallback)
3. **Default values** in `EnvironmentConfigHelper` (lowest priority)

### **Service-Specific Configuration:**
- **AUTH Service**: Uses `AUTH_DB_*` variables
- **USER Service**: Uses `USER_DB_*` variables  
- **Shared**: JWT, RabbitMQ, Redis configs applied to all services

### **Docker Integration:**
- `docker-compose.yml` references these environment variables
- Each service gets appropriate subset of configurations
- Database connections automatically configured per service

## ⚠️ Security Notes

1. **Never commit .env to version control**
2. **Use strong passwords in production**
3. **Rotate JWT secret keys regularly**
4. **Use separate environments for dev/staging/prod**
5. **Store sensitive values in secret management systems for production**
