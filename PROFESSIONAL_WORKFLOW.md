# Professional Cost-Effective AWS Development Workflow

This repository implements a **professional, cost-effective development workflow** for AWS-based microservices. The core principle: **Only pay for what you use, when you use it**.

## 🎯 Philosophy

- **99% of development time**: Work locally with Docker Compose (FREE)
- **1% of time**: Deploy to AWS for integration testing and presentations (PAID)
- **Result**: Professional infrastructure at student-friendly costs

## 🏗️ Architecture Overview

```
Frontend (Local) ←→ Backend APIs (Local Docker) ←→ Local Database
                          ↓ (Only when needed)
                    AWS Production Environment
```

## 📋 Daily Development Workflow (99% FREE)

### Local Development
```bash
# Frontend and Backend teams work entirely on local machines
docker-compose up -d  # Start all services locally
# Frontend connects to: http://localhost:8080/api
# Cost: $0.00
```

### Code Collaboration
- All development happens locally
- Git for code sharing
- Docker Compose for consistent environment
- No AWS resources running = No costs

## 🚀 Professional Deployment Workflow (1% PAID)

### 1. Development Environment (Team Integration Testing)
When frontend needs to test with a shared backend:

```bash
# Create temporary dev environment
./scripts/create-dev-env.sh

# Share the API URL with frontend team
# They test for 2-3 hours

# CRITICAL: Destroy immediately after testing
./scripts/destroy-dev-env.sh
```

**Cost**: ~$2-5 for the testing session

### 2. Production Environment (Presentations/Demos)
For final presentations or client demos:

```bash
# Deploy production-grade environment
./scripts/deploy-prod-env.sh

# Use for presentation (30-60 minutes)

# CRITICAL: Destroy immediately after presentation
./scripts/destroy-prod-env.sh
```

**Cost**: ~$5-10 for the presentation session

## 💰 Cost Breakdown

### Always Running (Fixed Costs)
- **ECR Repository**: ~$0.01/month (stores Docker images)
- **S3 Bucket**: ~$0.01/month (Terraform state)
- **Total Fixed**: ~$0.25/month

### On-Demand Costs (Only When Deployed)
- **Dev Environment**: ~$2-5/hour
- **Prod Environment**: ~$5-10/hour
- **When NOT deployed**: $0.00/hour

### Real-World Example
```
Month 1:
- Fixed costs: $0.25
- 2 dev sessions (3 hours each): $15
- 1 presentation (1 hour): $7
- Total: ~$22

Month 2 (only local development):
- Fixed costs: $0.25
- No deployments: $0
- Total: $0.25
```

## 🛠️ Environment Configurations

### Development Environment (dev.tfvars)
- **Instances**: t4g.micro (cheapest)
- **Database**: db.t4g.micro, single AZ
- **MQ**: Single instance
- **ECS**: 1 task, minimal resources
- **Purpose**: Quick testing, cost optimization

### Production Environment (prod.tfvars)
- **Instances**: t4g.small (production-grade)
- **Database**: Multi-AZ, automated backups
- **MQ**: High availability
- **ECS**: 2+ tasks, auto-scaling
- **Purpose**: Presentations, demos, final submissions

## 📚 Script Reference

### Core Workflow Scripts
```bash
# Development cycle
./scripts/create-dev-env.sh     # Create dev environment
./scripts/destroy-dev-env.sh    # Destroy dev environment

# Presentation cycle  
./scripts/deploy-prod-env.sh    # Create production environment
./scripts/destroy-prod-env.sh   # Destroy production environment
```

### Legacy Scripts (For Emergency)
```bash
./scripts/start-dev-env.sh      # Start existing resources
./scripts/stop-dev-env.sh       # Stop (scale to 0) resources
./scripts/nuclear-stop.sh       # Emergency stop everything
./scripts/nuclear-start.sh      # Emergency start everything
```

## ⚠️ Critical Cost Management Rules

### 1. NEVER leave environments running
```bash
# ❌ WRONG: Deploy and forget
./scripts/deploy-prod-env.sh
# ... presentation ...
# 💸 Forgot to destroy = $200+ monthly bill

# ✅ CORRECT: Deploy, use, destroy
./scripts/deploy-prod-env.sh
# ... presentation ...
./scripts/destroy-prod-env.sh  # CRITICAL!
```

### 2. Set up billing alerts
- AWS Billing > Budgets
- Alert at $10, $25, $50
- Email notifications enabled

### 3. Monitor workspace usage
```bash
terraform workspace list  # Check active workspaces
terraform workspace show  # See current workspace
```

## 🔄 Complete Professional Cycle

### Week 1-3: Pure Development
```bash
# Local development only
docker-compose up -d
# Code, test, commit, push
# Cost: $0.25 (fixed)
```

### Week 4: Integration Testing
```bash
# Create dev environment for team testing
./scripts/create-dev-env.sh
# Frontend team tests for 3 hours
./scripts/destroy-dev-env.sh
# Cost: +$12 for the session
```

### Week 5: Final Presentation
```bash
# Deploy production for demo
./scripts/deploy-prod-env.sh
# Present to client/teacher (1 hour)
./scripts/destroy-prod-env.sh
# Cost: +$8 for the session
```

### Total Month Cost: ~$20 (instead of $200+ if always running)

## 🎓 Benefits for Students

1. **Professional Infrastructure**: Same as enterprise projects
2. **Cost Control**: Pay only for active usage
3. **Learning**: Real AWS experience with Terraform
4. **Flexibility**: Scale up/down as needed
5. **Portfolio**: Demonstrate cloud expertise

## 🚀 Getting Started

1. **Setup**: Repository is already configured
2. **Develop**: Use Docker Compose locally
3. **Test**: Create dev environment when needed
4. **Present**: Deploy production for demos
5. **Save Money**: Always destroy after use

## 📞 Support

If costs start accumulating unexpectedly:
1. Check running workspaces: `terraform workspace list`
2. Emergency stop: `./scripts/nuclear-stop.sh`
3. Check AWS billing dashboard
4. Destroy all environments: Run destroy scripts for all workspaces

---

**Remember: The key to cost-effective cloud development is discipline in destroying resources immediately after use.**