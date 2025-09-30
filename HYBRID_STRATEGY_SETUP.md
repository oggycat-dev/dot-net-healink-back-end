# 🏆 Hybrid Strategy Setup Guide

## Tổng Quan

**Chi phí**: $5-10/tháng  
**Tiết kiệm**: $63-68/tháng so với full AWS  
**Thời gian**: 90% local (free), 10% AWS (testing)

---

## 📋 Step-by-Step Setup

### Step 1: Verify Environment

```bash
# Check Docker
docker --version
docker compose version

# Check if services are running
docker ps

# If running, stop them
docker compose down
```

### Step 2: Setup Local Development

```bash
# Start local environment
./scripts/local-dev.sh start

# This will start:
# ✅ PostgreSQL (local - FREE)
# ✅ Redis (local - FREE)
# ✅ RabbitMQ (local - FREE)
# ✅ All microservices (local - FREE)
# ✅ pgAdmin (local - FREE)

# Wait 2-3 minutes for all services to start
```

### Step 3: Verify Local Services

```bash
# Check service status
./scripts/local-dev.sh status

# Show all URLs
./scripts/local-dev.sh urls

# Expected output:
# 🚪 Gateway API:      http://localhost:5010
# 🔐 Auth Service:     http://localhost:5001
# 👤 User Service:     http://localhost:5002
# 📝 Content Service:  http://localhost:5003
# 🔔 Notification:     http://localhost:5004
# 🐰 RabbitMQ Admin:   http://localhost:15672
# 🗄️  PostgreSQL Admin: http://localhost:5050
```

### Step 4: Test Local Services

```bash
# Test Gateway health
curl http://localhost:5010/api/auth/health
curl http://localhost:5010/api/users/health
curl http://localhost:5010/api/content/health

# All should return 200 OK
```

### Step 5: Daily Development Workflow

```bash
# Morning: Start local environment
./scripts/local-dev.sh start

# Develop and test locally
# - Make code changes
# - Test locally
# - Commit changes

# Check logs if needed
./scripts/local-dev.sh logs authservice-api

# Rebuild service after changes
./scripts/local-dev.sh rebuild contentservice-api

# Evening: Stop local environment
./scripts/local-dev.sh stop
```

---

## ☁️ AWS Testing (Only When Needed)

### When to Deploy to AWS?

Deploy to AWS only when you need to:
- ✅ Test cloud-specific features (S3, CloudFront, etc.)
- ✅ Test with real AWS services (RDS, ElastiCache if needed)
- ✅ Demo to stakeholders
- ✅ Integration testing before production
- ✅ Performance testing with real infrastructure

**Frequency**: 1-2 times per week, 2-3 hours each time

### Step 6: Deploy to AWS for Testing

```bash
# Option A: Using GitHub Actions (Recommended)
# 1. Go to GitHub → Actions
# 2. Select "🚀 Full Deploy - All Services"
# 3. Click "Run workflow"
# 4. Select environment: "dev"
# 5. Click "Run workflow"
# 6. Wait 15-20 minutes

# Option B: Using Scripts (Manual)
./scripts/healink-manager.sh create dev
```

### Step 7: Test on AWS

```bash
# Get service URLs from Terraform output
cd terraform_healink/app-infra
terraform output

# Test services
curl http://{gateway-alb-url}/api/auth/health
curl http://{gateway-alb-url}/api/users/health

# Run your cloud-specific tests
# ... test for 2-3 hours ...
```

### Step 8: Nuke AWS After Testing

```bash
# Option A: Using GitHub Actions (Recommended)
# 1. Go to GitHub → Actions
# 2. Select "☢️ Nuke AWS (Keep ECR & RDS)"
# 3. Click "Run workflow"
# 4. Type "NUKE" in confirmation
# 5. Check "Keep ECR images" ✓
# 6. Check "Keep RDS database" ✓
# 7. Click "Run workflow"
# 8. Wait 5-10 minutes

# Option B: Using Scripts (Manual)
./scripts/healink-manager.sh destroy dev
```

---

## 💰 Cost Tracking

### Expected Monthly Costs

```
Week 1: Deploy 3h → Test → Nuke  = $0.37
Week 2: Deploy 2h → Test → Nuke  = $0.25
Week 3: Deploy 3h → Test → Nuke  = $0.37
Week 4: Deploy 2h → Test → Nuke  = $0.25
────────────────────────────────────────
Total: 10 hours/month              = ~$1.24

Add buffer for:
- RDS snapshots                    = $0.50
- CloudWatch logs                  = $0.50
- S3 storage                       = $0.50
- Data transfer                    = $1.00
────────────────────────────────────────
Monthly Total:                     = $3.74

With safety margin:                = $5-10/month
```

### Cost Breakdown per Deploy

```
Deploy for 3 hours:
├─ ECS Fargate (5 services)
│  ├─ 5 tasks × 0.25 vCPU × 3h = 3.75 vCPU-hours
│  ├─ Cost: 3.75 × $0.04048 = $0.15
│  ├─ 5 tasks × 0.5 GB × 3h = 7.5 GB-hours
│  └─ Cost: 7.5 × $0.004445 = $0.03
├─ Total Fargate: $0.18
├─ RDS (Free Tier): $0.00
├─ ALB (Free Tier): $0.00
└─ Total: ~$0.20 per 3-hour session
```

---

## 📊 Weekly Schedule Example

### Monday - Friday (Development)
```
8:00 AM  - Start local environment
           ./scripts/local-dev.sh start

9:00 AM  - Develop features locally
to       - Test locally
5:00 PM  - Commit changes

6:00 PM  - Stop local environment
           ./scripts/local-dev.sh stop

Cost: $0/day
```

### Friday Afternoon (AWS Testing)
```
3:00 PM  - Deploy to AWS
           GitHub Actions → Full Deploy

3:20 PM  - Test cloud features
to       - Integration testing
5:30 PM  - Final checks

5:30 PM  - Nuke AWS
           GitHub Actions → Nuke AWS

Cost: ~$0.40 (2.5 hours deployment)
```

### Weekend
```
No deployment - Everything stopped
Cost: $0
```

---

## 🎯 Best Practices

### 1. Always Test Locally First
```bash
# Before deploying to AWS:
./scripts/local-dev.sh start
# ... test thoroughly ...
# If OK, then deploy to AWS
```

### 2. Set Time Limit for AWS Testing
```bash
# Set a timer for 2-3 hours
# When time's up → Nuke AWS
# This prevents accidentally leaving services running overnight
```

### 3. Use GitHub Actions for AWS
```bash
# ✅ Recommended: GitHub Actions
# - Automated process
# - Can't forget to nuke
# - Audit trail

# ⚠️  Manual: Only for emergencies
# - Easy to forget to destroy
# - Can lead to unexpected costs
```

### 4. Monitor Costs Daily
```bash
# AWS Console → Billing Dashboard
# Check costs every morning
# Set budget alert: $10/month
# If costs > $5 in first week → investigate
```

### 5. Keep Local Environment Updated
```bash
# Weekly: Pull latest images
docker compose pull

# After code changes: Rebuild
./scripts/local-dev.sh rebuild {service-name}

# Monthly: Clean up old images
docker system prune -a
```

---

## 🛠️ Troubleshooting

### Issue: Local services not starting
```bash
# Check if ports are in use
lsof -i :5010  # Gateway
lsof -i :5432  # PostgreSQL
lsof -i :6379  # Redis

# Stop conflicting services
# Then restart
./scripts/local-dev.sh stop
./scripts/local-dev.sh start
```

### Issue: Can't connect to local services
```bash
# Check if services are running
./scripts/local-dev.sh status

# Check logs
./scripts/local-dev.sh logs {service-name}

# Restart specific service
./scripts/local-dev.sh restart {service-name}
```

### Issue: AWS costs higher than expected
```bash
# Check what's running
./scripts/healink-manager.sh status dev

# Destroy immediately
./scripts/healink-manager.sh destroy dev

# Review AWS Cost Explorer
# Check for:
# - Forgotten deployments
# - Data transfer costs
# - NAT Gateway costs
```

### Issue: Forgot to nuke AWS
```bash
# Nuke immediately!
# GitHub Actions → Nuke AWS → Run

# Or manual:
./scripts/healink-manager.sh destroy dev

# Check costs:
# AWS Console → Billing → Cost Explorer
# One day forgotten = ~$2.50
```

---

## 📈 Cost Optimization Tips

### 1. Batch Your AWS Testing
```bash
# ❌ Don't: Test multiple times a day
#    Cost: 5× per day × $0.20 = $1/day = $30/month

# ✅ Do: Test once a week, batch all tests
#    Cost: 1× per week × $0.40 = $1.60/month
```

### 2. Use Shorter Test Sessions
```bash
# Deploy → Run automated tests → Nuke
# Target: 1-2 hours instead of 3-4 hours
# Savings: 50% reduction in Fargate costs
```

### 3. Deploy Only Changed Services
```bash
# If only AuthService changed:
# Don't deploy all services
# Future: Individual service workflows
```

### 4. Use Local RDS for Development
```bash
# Current: AWS RDS (Free Tier)
# Alternative: Local PostgreSQL
# Savings: $0 (already free, but no cloud dependency)
```

---

## ✅ Success Checklist

### Initial Setup
- [ ] Local environment running
- [ ] All services accessible
- [ ] Can develop and test locally
- [ ] GitHub Actions configured
- [ ] AWS Budget Alert set ($10/month)

### Weekly Operations
- [ ] Develop locally 90% of time
- [ ] Deploy to AWS only when needed
- [ ] Test thoroughly in 2-3 hours
- [ ] Nuke AWS after testing
- [ ] Check AWS costs

### Monthly Review
- [ ] Review total AWS costs (should be $5-10)
- [ ] Clean up old Docker images
- [ ] Update local environment
- [ ] Review and optimize workflow

---

## 📊 Expected Results

### First Month
```
Week 1: Setup + 1 deploy         = $2
Week 2: 1 deploy                 = $1
Week 3: 2 deploys                = $2
Week 4: 1 deploy                 = $1
────────────────────────────────────
Total:                           = $6
```

### Typical Month (after settling)
```
Weekly AWS testing (2-3h)        = $4
Buffer (snapshots, logs, etc.)   = $2
────────────────────────────────────
Total:                           = $6-8/month
```

### Cost Savings vs Full AWS
```
Full AWS:          $73/month
Hybrid Strategy:   $6-8/month
────────────────────────────────
Savings:           $65-67/month
Annual Savings:    $780-804/year
```

---

## 🎉 You're Ready!

**Start Now**:
```bash
# Step 1: Start local environment
./scripts/local-dev.sh start

# Step 2: Verify everything works
./scripts/local-dev.sh urls

# Step 3: Start developing (FREE!)
# ... your daily work ...

# Step 4: Deploy to AWS only when needed (CHEAP!)
# GitHub Actions → Full Deploy
```

**Expected Monthly Cost**: $5-10  
**Savings**: $65-67 per month  
**Annual Savings**: $780-804 🎊

---

**Last Updated**: September 30, 2025  
**Strategy**: Hybrid Development  
**Status**: ✅ Ready to Use
