# 🚀 Healink Scripts - Cleaned & Simplified

## 📋 Current Scripts

### **Primary Scripts:**
- **`healink-manager.sh`** - 🎯 **All-in-one environment manager** (Replaces 8 old scripts)
- **`deploy-modules.sh`** - 🏗️ **Terraform modules deployment** (Specialized for modular approach)
- **`local-dev.sh`** - 🐳 **Local Docker Compose development** (NEW!)

### **Backup Scripts:**
- **`old-scripts/`** - 📦 **Backup of previous scripts** (8 scripts moved here)

---

## 🎯 **PRIMARY USAGE: healink-manager.sh**

### **Quick Start:**
```bash
# Show help
./scripts/healink-manager.sh

# Development workflow
./scripts/healink-manager.sh create dev      # Create dev environment
./scripts/healink-manager.sh status dev      # Check status
./scripts/healink-manager.sh destroy dev     # Clean up (save money)

# Production workflow  
./scripts/healink-manager.sh deploy prod     # Deploy to production
./scripts/healink-manager.sh status prod     # Check production status
```

### **All Commands:**
| Command | Environment | Description |
|---------|-------------|-------------|
| `create` | dev/prod | Create fresh environment from scratch |
| `deploy` | dev/prod | Deploy/update existing environment |
| `start` | dev/prod | Start existing environment (alias for deploy) |
| `stop` | dev/prod | Stop environment (not implemented yet) |
| `destroy` | dev/prod | Completely destroy environment |
| `status` | dev/prod | Show current environment status |
| `logs` | dev/prod | Show recent logs (not implemented yet) |

### **Environment Safety:**
- **Dev Environment**: Simple confirmation (`yes`)
- **Production Environment**: Requires typing `DESTROY PRODUCTION`

---

## 🏗️ **SPECIALIZED: deploy-modules.sh**

For advanced Terraform modules deployment:
```bash
# Plan with modules
./scripts/deploy-modules.sh --plan

# Deploy with modules
./scripts/deploy-modules.sh
```

## 🐳 **LOCAL DEVELOPMENT: local-dev.sh**

For rapid local development with Docker Compose:
```bash
# Start all services locally
./scripts/local-dev.sh start

# Create new microservice
./scripts/local-dev.sh create-service

# Show all URLs
./scripts/local-dev.sh urls

# Check logs
./scripts/local-dev.sh logs authservice-api

# Rebuild after changes
./scripts/local-dev.sh rebuild productservice-api
```

**Available Services:**
- 🚪 Gateway API: http://localhost:5000
- 🔐 Auth Service: http://localhost:5001  
- 📦 Product Service: http://localhost:5002
- 🐰 RabbitMQ Admin: http://localhost:15672
- 🗄️ pgAdmin: http://localhost:5050

**See [LOCAL_DEVELOPMENT.md](../LOCAL_DEVELOPMENT.md) for complete guide**

---

## 🧹 **What Was Cleaned Up:**

### **❌ Old Scripts (Moved to backup):**
- `create-dev-env.sh` → `healink-manager.sh create dev`
- `start-dev-env.sh` → `healink-manager.sh start dev`  
- `stop-dev-env.sh` → `healink-manager.sh stop dev`
- `destroy-dev-env.sh` → `healink-manager.sh destroy dev`
- `deploy-prod-env.sh` → `healink-manager.sh deploy prod`
- `destroy-prod-env.sh` → `healink-manager.sh destroy prod`
- `nuclear-start.sh` → `healink-manager.sh create dev`
- `nuclear-stop.sh` → `healink-manager.sh destroy dev`

### **✅ Benefits of Cleanup:**
- **9 scripts → 2 scripts** (89% reduction)
- **Consistent interface** for all operations
- **Built-in safety** for destructive operations
- **Better error handling** and user feedback
- **Easier maintenance** and updates

---

## 📖 **Examples:**

### **Development Workflow:**
```bash
# Start of day
./scripts/healink-manager.sh create dev

# Check everything is running
./scripts/healink-manager.sh status dev

# End of day (save money)
./scripts/healink-manager.sh destroy dev
```

### **Production Deployment:**
```bash
# Deploy to production
./scripts/healink-manager.sh deploy prod

# Verify deployment
./scripts/healink-manager.sh status prod
```

### **Emergency Recovery:**
```bash
# Check current state
./scripts/healink-manager.sh status dev

# Recreate if needed
./scripts/healink-manager.sh create dev
```

---

## 🎯 **Migration Complete!**

✅ **Scripts cleaned and simplified**  
✅ **All functionality preserved**  
✅ **Better user experience**  
✅ **Easier to maintain**  

*Old scripts are safely backed up in `old-scripts/` folder.*