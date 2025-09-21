# 🐳 Local Development Environment

## 🚀 Quick Start

### **Start All Services:**
```bash
./scripts/local-dev.sh start
```

### **Check Service URLs:**
```bash
./scripts/local-dev.sh urls
```

### **Create New Service:**
```bash
./scripts/local-dev.sh create-service
```

---

## 📋 Available Commands

| Command | Description | Example |
|---------|-------------|---------|
| `start [service]` | Start all or specific service | `./scripts/local-dev.sh start` |
| `stop [service]` | Stop all or specific service | `./scripts/local-dev.sh stop` |
| `restart [service]` | Restart service | `./scripts/local-dev.sh restart authservice-api` |
| `rebuild [service]` | Rebuild and restart | `./scripts/local-dev.sh rebuild productservice-api` |
| `logs [service]` | Show logs | `./scripts/local-dev.sh logs gateway-api` |
| `status` | Show all service status | `./scripts/local-dev.sh status` |
| `clean` | Clean containers/volumes | `./scripts/local-dev.sh clean` |
| `reset` | Complete reset | `./scripts/local-dev.sh reset` |
| `urls` | Show service URLs | `./scripts/local-dev.sh urls` |
| `create-service` | Create new service template | `./scripts/local-dev.sh create-service` |

---

## 🌐 Service URLs (After Start)

| Service | URL | Purpose |
|---------|-----|---------|
| **API Gateway** | http://localhost:5000 | Main entry point |
| **Auth Service** | http://localhost:5001 | Authentication |
| **Product Service** | http://localhost:5002 | Product management |
| **RabbitMQ Admin** | http://localhost:15672 | Message queue management |
| **pgAdmin** | http://localhost:5050 | Database management |
| **Redis Commander** | http://localhost:8081 | Cache management |

---

## 🔧 Development Workflow

### **1. Start Development Environment:**
```bash
# Start all services
./scripts/local-dev.sh start

# Check if everything is running
./scripts/local-dev.sh status

# View service URLs
./scripts/local-dev.sh urls
```

### **2. Create New Microservice:**
```bash
# Interactive service creation
./scripts/local-dev.sh create-service

# Follow prompts:
# Enter service name: order-service
# ✅ Creates complete service template
# ✅ Adds to docker-compose.yml
# ✅ Sets up proper ports and networking
```

### **3. Development Cycle:**
```bash
# Make code changes...

# Rebuild specific service
./scripts/local-dev.sh rebuild order-service-api

# Check logs
./scripts/local-dev.sh logs order-service-api

# Test your service
curl http://localhost:5003/health
```

### **4. Daily Workflow:**
```bash
# Start of day
./scripts/local-dev.sh start

# Work on your code...

# End of day
./scripts/local-dev.sh stop
```

---

## 🏗️ Service Template Structure

When you create a new service, it generates:

```
src/OrderService/
├── OrderService.API/
│   ├── Dockerfile
│   └── OrderService.API.csproj (to be created)
├── OrderService.Application/
│   └── OrderService.Application.csproj (to be created)
├── OrderService.Domain/
│   └── OrderService.Domain.csproj (to be created)
└── OrderService.Infrastructure/
    └── OrderService.Infrastructure.csproj (to be created)
```

**Docker Compose entry added automatically:**
- Service name: `order-service-api`
- Port: Auto-assigned (5003, 5004, etc.)
- Database: `healink_order_service_db`
- Full integration with existing infrastructure

---

## 🔧 Environment Configuration

The script auto-creates `.env` file with defaults:

```bash
# Database
DB_USER=healink_user
DB_PASSWORD=healink_password_2024
AUTH_DB_NAME=healink_auth_db
PRODUCT_DB_NAME=healink_product_db

# RabbitMQ
RABBITMQ_USER=healink_mq
RABBITMQ_PASSWORD=healink_mq_password_2024

# Redis
REDIS_PASSWORD=healink_redis_password_2024

# JWT
JWT_SECRET_KEY=HealinkJWTSecretKeyForDevelopmentEnvironmentOnly2024
```

---

## 🚨 Troubleshooting

### **Port Conflicts:**
```bash
# Check what's using ports
lsof -i :5000-5010

# Stop all services
./scripts/local-dev.sh stop
```

### **Service Won't Start:**
```bash
# Check logs
./scripts/local-dev.sh logs service-name

# Rebuild from scratch
./scripts/local-dev.sh rebuild service-name
```

### **Database Issues:**
```bash
# Reset everything
./scripts/local-dev.sh reset

# Or clean and restart
./scripts/local-dev.sh clean
./scripts/local-dev.sh start
```

---

## 🎯 Next Steps After Local Development

1. **Test locally** with Docker Compose
2. **Push to Git** when ready
3. **Deploy to AWS** using Terraform modules:
   ```bash
   ./scripts/healink-manager.sh create dev
   ```

---

## 💡 Pro Tips

- **Use `./scripts/local-dev.sh urls`** to quickly access all services
- **Check logs frequently** during development
- **Clean up regularly** to free disk space
- **Create services incrementally** - start simple, add complexity

**Perfect local development environment for rapid microservices development! 🚀**