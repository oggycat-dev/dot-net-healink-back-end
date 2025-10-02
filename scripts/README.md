# 🚀 Healink Scripts - Local & Manual Operations

## 📋 Overview

These scripts are for **local development** and **manual/emergency operations**. 

For **production deployments**, use [GitHub Actions CI/CD](.github/workflows/README_FIRST.md) instead.

---

## 🎯 When to Use What?

| Scenario | Use This | Documentation |
|----------|----------|---------------|
| **Local Development** | `local-dev.sh` | This file |
| **Deploy to AWS** | GitHub Actions | [CI/CD Guide](../.github/workflows/README_FIRST.md) |
| **Nuke AWS** | GitHub Actions | [Nuke Workflow](../.github/workflows/README.md#nuke-aws) |
| **Manual AWS Operations** | `healink-manager.sh` | This file |
| **Emergency Recovery** | `healink-manager.sh` | This file |
| **Test Terraform Changes** | `healink-manager.sh` | This file |

---

## 🐳 Local Development (`local-dev.sh`)

For rapid local development with Docker Compose.

### Quick Start:
```bash
# Start all services locally
./scripts/local-dev.sh start

# Show service URLs
./scripts/local-dev.sh urls

# Check logs
./scripts/local-dev.sh logs authservice-api

# Rebuild after changes
./scripts/local-dev.sh rebuild contentservice-api

# Clean up
./scripts/local-dev.sh clean
```

### Available Commands:
| Command | Description |
|---------|-------------|
| `start [service]` | Start all or specific service |
| `stop [service]` | Stop all or specific service |
| `restart [service]` | Restart services |
| `rebuild [service]` | Rebuild and restart service |
| `logs [service]` | Show logs |
| `status` | Show status of all services |
| `clean` | Clean up containers and volumes |
| `reset` | Complete reset (clean + rebuild) |
| `urls` | Show all service URLs |
| `create-service` | Create new microservice template |

### Service URLs:
```
🚪 Gateway API:      http://localhost:5010
🔐 Auth Service:     http://localhost:5001
👤 User Service:     http://localhost:5002
📝 Content Service:  http://localhost:5003
🔔 Notification:     http://localhost:5004
🐰 RabbitMQ Admin:   http://localhost:15672
🗄️  PostgreSQL Admin: http://localhost:5050
```

---

## ☁️ Manual AWS Operations (`healink-manager.sh`)

For manual/emergency AWS operations when CI/CD is not available.

### ⚠️ WARNING:
For normal deployments, **use GitHub Actions** instead:
- **Deploy**: [Full Deploy Workflow](../.github/workflows/full-deploy.yml)
- **Nuke**: [Nuke AWS Workflow](../.github/workflows/nuke-aws.yml)

### Emergency Use Cases:
```bash
# Emergency: Check AWS status
./scripts/healink-manager.sh status dev

# Emergency: Manual deployment
./scripts/healink-manager.sh deploy dev

# Emergency: Manual destroy (save money)
./scripts/healink-manager.sh destroy dev

# Test Terraform changes locally
./scripts/healink-manager.sh create dev
```

### Available Commands:
| Command | Description |
|---------|-------------|
| `create <env>` | Create fresh environment (dev/prod) |
| `deploy <env>` | Deploy/update environment |
| `start <env>` | Start existing environment |
| `stop <env>` | Stop environment (not implemented) |
| `destroy <env>` | Destroy environment completely |
| `status <env>` | Show environment status |
| `logs <env>` | Show recent logs (not implemented) |
| `config` | Generate config files from .env |

---

## 📦 Other Scripts

### `deploy-modules.sh`
Specialized Terraform modules deployment.

```bash
# Plan with modules
./scripts/deploy-modules.sh --plan

# Deploy with modules
./scripts/deploy-modules.sh
```

### `git-push.sh`
Git automation for quick commits.

```bash
# Quick commit and push
./scripts/git-push.sh "commit message"
```

---

## 🆚 Scripts vs CI/CD Comparison

### 🏆 Use GitHub Actions CI/CD for:
- ✅ **Production deployments** (safer, automated)
- ✅ **Building Docker images** (parallel builds)
- ✅ **Team collaboration** (everyone uses same process)
- ✅ **Cost optimization** (automated nuke workflows)
- ✅ **Audit trail** (all actions logged in GitHub)

### 🔧 Use Scripts for:
- ✅ **Local development** (Docker Compose)
- ✅ **Manual testing** (Terraform changes)
- ✅ **Emergency operations** (when CI/CD is down)
- ✅ **Quick experiments** (test ideas locally)
- ✅ **Personal workflows** (individual preferences)

---

## 🎓 Recommended Workflow

### Daily Development:
```bash
# Morning: Start local environment
./scripts/local-dev.sh start

# Develop and test locally
./scripts/local-dev.sh logs authservice-api

# Make changes, rebuild
./scripts/local-dev.sh rebuild authservice-api

# Evening: Stop local environment
./scripts/local-dev.sh stop
```

### Deploy to AWS:
```bash
# Commit your changes
git add .
git commit -m "feature: new API endpoint"
git push

# Go to GitHub Actions
# Run "Full Deploy" workflow
# Select environment: dev
# Wait 15-20 minutes
# ✅ Done!
```

### Emergency Recovery:
```bash
# If CI/CD is broken or unavailable:
./scripts/healink-manager.sh status dev
./scripts/healink-manager.sh destroy dev
./scripts/healink-manager.sh create dev
```

---

## 📚 Documentation Links

### Local Development:
- [LOCAL_DEVELOPMENT.md](../LOCAL_DEVELOPMENT.md) - Complete local dev guide
- [docker-compose.yml](../docker-compose.yml) - Service configuration

### CI/CD:
- [README_FIRST.md](../.github/workflows/README_FIRST.md) - Start here
- [QUICK_START.md](../.github/workflows/QUICK_START.md) - Common scenarios
- [ARCHITECTURE.md](../.github/workflows/ARCHITECTURE.md) - System design

### Terraform:
- [terraform_healink/README.md](../terraform_healink/README.md) - Terraform guide
- [deploy.sh](../terraform_healink/deploy.sh) - Terraform script

---

## 🧹 Cleanup Status

### ✅ Cleaned Up (Moved to `old-scripts/`):
These scripts have been replaced by `healink-manager.sh`:
- ❌ `create-dev-env.sh`
- ❌ `start-dev-env.sh`
- ❌ `stop-dev-env.sh`
- ❌ `destroy-dev-env.sh`
- ❌ `deploy-prod-env.sh`
- ❌ `destroy-prod-env.sh`
- ❌ `nuclear-start.sh`
- ❌ `nuclear-stop.sh`

### ✅ Current Scripts (Active):
- ✅ `local-dev.sh` - Local Docker Compose development
- ✅ `healink-manager.sh` - Manual AWS operations
- ✅ `deploy-modules.sh` - Terraform modules
- ✅ `git-push.sh` - Git automation

---

## 💡 Pro Tips

### 1. Local Development First
Always test locally before deploying to AWS:
```bash
./scripts/local-dev.sh start
# Test your changes
# If OK, commit and deploy via GitHub Actions
```

### 2. Use CI/CD for Production
Don't use scripts for production deployments:
```bash
# ❌ DON'T: ./scripts/healink-manager.sh deploy prod
# ✅ DO: GitHub Actions → Full Deploy → prod
```

### 3. Emergency Recovery
Keep scripts for emergencies:
```bash
# When GitHub Actions is down:
./scripts/healink-manager.sh status dev
```

### 4. Cost Optimization
Use CI/CD Nuke workflow for better cost savings:
```bash
# ✅ Safer: GitHub Actions → Nuke AWS → Type "NUKE"
# vs
# ⚠️  Manual: ./scripts/healink-manager.sh destroy dev
```

---

## 🎯 Quick Reference

### Local Development:
```bash
./scripts/local-dev.sh start     # Start
./scripts/local-dev.sh stop      # Stop
./scripts/local-dev.sh logs      # Logs
./scripts/local-dev.sh urls      # Show URLs
```

### Manual AWS (Emergency):
```bash
./scripts/healink-manager.sh status dev    # Check status
./scripts/healink-manager.sh deploy dev    # Deploy
./scripts/healink-manager.sh destroy dev   # Destroy
```

### CI/CD (Recommended):
```
GitHub → Actions → Full Deploy → Run
GitHub → Actions → Nuke AWS → Type "NUKE" → Run
```

---

## 📞 Need Help?

- **Local Dev Issues**: Check [LOCAL_DEVELOPMENT.md](../LOCAL_DEVELOPMENT.md)
- **AWS Deployment**: Use [CI/CD Guide](../.github/workflows/README_FIRST.md)
- **Emergency**: Use scripts + contact team

---

**Last Updated**: September 30, 2025  
**Status**: Active (Complementary to CI/CD)  
**Maintained By**: Healink DevOps Team