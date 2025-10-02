# ⚡ Quick Start Guide - Healink CI/CD

## 🎯 TL;DR

```bash
# Deploy everything to dev
GitHub Actions → "Full Deploy" → Select "dev" → Run

# Save money when done
GitHub Actions → "Nuke AWS" → Type "NUKE" → Run

# Redeploy next day
GitHub Actions → "Full Deploy" → Select "dev" → Run
```

---

## 📋 Prerequisites Checklist

- [ ] AWS Account with admin access
- [ ] IAM Role: `GitHubActionRole-Healink` configured
- [ ] S3 Bucket: `healink-tf-state-2025-oggycatdev` created
- [ ] GitHub Secrets configured:
  - `JWT_ISSUER`
  - `JWT_AUDIENCE`
  - `JWT_SECRET_KEY`
  - `REDIS_CONNECTION_STRING`

---

## 🚀 Scenario 1: First Time Deploy

**Goal**: Deploy entire Healink system to AWS for the first time

**Steps**:
1. Go to GitHub repo → **Actions** tab
2. Click **"🚀 Full Deploy - All Services"**
3. Click **"Run workflow"** button
4. Select:
   - Environment: **dev**
   - Skip build: **false** (unchecked)
5. Click **"Run workflow"**
6. Wait **15-20 minutes** ⏱️
7. Check workflow output for service URLs
8. Test: `curl http://{gateway-url}/health`

**Result**: All 5 services running on AWS ✅

**Cost**: ~$74-94/month

---

## 💰 Scenario 2: Save Money Overnight

**Goal**: Stop services to save money, but keep data

**Steps**:
1. Go to GitHub repo → **Actions** tab
2. Click **"☢️ Nuke AWS (Keep ECR & RDS)"**
3. Click **"Run workflow"** button
4. Fill in:
   - Environment: **dev**
   - Confirmation: **NUKE** (must type exactly)
   - Keep ECR images: **✓ checked**
   - Keep RDS: **✓ checked**
5. Click **"Run workflow"**
6. Wait **5-10 minutes** ⏱️
7. Verify: Check that RDS and ECR still exist in AWS Console

**Result**: Services deleted, data preserved ✅

**Cost**: ~$27.50/month (saving $46.50-66.50/month)

---

## 🔄 Scenario 3: Daily Development Cycle

**Morning** (Start work):
```
1. Run "Full Deploy" (dev)
2. Wait 15-20 minutes
3. Get coffee ☕
4. Start coding
```

**Evening** (End of day):
```
1. Commit & push code
2. Run "Nuke AWS" (dev)
3. Go home
4. Save $2-3 per night 💰
```

**Savings**: ~$60/month if you work 5 days/week

---

## 🏗️ Scenario 4: Update Single Service

**Goal**: Only rebuild and deploy one changed service

**Current**: Use Full Deploy with `skip_build: false`

**Future**: Individual service workflows (coming soon)

**Steps**:
1. Make code changes in `src/UserService/`
2. Run "Full Deploy"
3. Services will detect changes and rebuild only UserService
4. Other services use existing images

---

## 🐛 Scenario 5: Something Broke - Full Reset

**Goal**: Clean everything and start fresh

**Steps**:

### Step 1: Nuke Everything
```
1. Run "Nuke AWS" workflow
2. Type "NUKE" to confirm
3. Wait for completion
```

### Step 2: Manual Cleanup (if needed)
```bash
# Delete any stuck resources
aws ecs list-clusters
aws ecs delete-cluster --cluster {cluster-name}

aws elbv2 describe-load-balancers
aws elbv2 delete-load-balancer --load-balancer-arn {arn}
```

### Step 3: Clear Terraform State (if needed)
```bash
cd terraform_healink
./deploy.sh clean
```

### Step 4: Redeploy
```
1. Run "Full Deploy" workflow
2. Select environment
3. Wait for completion
```

---

## 📊 Scenario 6: Deploy to Production

**Goal**: Deploy to production environment

**⚠️ IMPORTANT**: Test in dev first!

**Steps**:

### Step 1: Test in Dev
```
1. Run "Full Deploy" (dev)
2. Test all endpoints
3. Verify everything works
4. Run integration tests
```

### Step 2: Deploy to Prod
```
1. Run "Full Deploy" (prod)
2. Select environment: "prod"
3. Wait 15-20 minutes
4. Monitor CloudWatch logs
```

### Step 3: Verify Production
```bash
# Check health endpoints
curl http://{prod-gateway-url}/api/auth/health
curl http://{prod-gateway-url}/api/users/health
curl http://{prod-gateway-url}/api/content/health

# Check RabbitMQ
# Check Redis
# Check RDS connections
```

### Step 4: Monitor
```
1. Watch CloudWatch Logs
2. Set up alarms (if not already)
3. Monitor costs in AWS Console
```

**Cost**: ~$150-200/month (prod uses larger instances)

---

## 🔍 Scenario 7: Debug Failed Deployment

### If "Full Deploy" Fails

**1. Build Phase Failed**
```
Check: Docker build errors
Solution: 
  - Fix Dockerfile syntax
  - Check dependencies in .csproj files
  - Ensure all files exist
```

**2. Stateful Infrastructure Failed**
```
Check: Terraform errors in stateful-infra
Solution:
  - Check if RDS already exists
  - Verify VPC/subnet configuration
  - Check AWS quota limits
```

**3. Application Infrastructure Failed**
```
Check: Terraform errors in app-infra
Solution:
  - Ensure stateful layer deployed first
  - Check ECS task definition
  - Verify security group rules
```

**4. Health Check Failed**
```
Check: Services not starting
Solution:
  - Check CloudWatch logs: /ecs/healink-{service}-{env}
  - Verify environment variables
  - Check RDS connection
  - Verify RabbitMQ connection
```

### If "Nuke AWS" Fails

**1. ALB Won't Delete**
```
Solution:
  - Wait 5 minutes, retry
  - Manually delete in AWS Console
  - Check if any targets still registered
```

**2. ECS Service Won't Delete**
```
Solution:
  aws ecs delete-service \
    --cluster healink-dev \
    --service {service-name} \
    --force
```

**3. Target Group Won't Delete**
```
Solution:
  - Delete ALB first
  - Wait for ALB to fully delete
  - Then delete target groups
```

---

## 📈 Monitoring After Deployment

### Check Service Health
```bash
# Get ALB URLs from Terraform output
cd terraform_healink/app-infra
terraform output

# Test each service
curl http://{auth-alb-url}/health
curl http://{user-alb-url}/health
curl http://{content-alb-url}/health
curl http://{gateway-alb-url}/health
```

### Check CloudWatch Logs
```
AWS Console → CloudWatch → Log Groups → 
  /ecs/healink-authservice-dev
  /ecs/healink-userservice-dev
  /ecs/healink-contentservice-dev
  /ecs/healink-notificationservice-dev
  /ecs/healink-gateway-dev
```

### Check ECS Services
```
AWS Console → ECS → Clusters → healink-dev → 
  Check: Desired count = Running count
  Check: All tasks healthy
```

### Check RDS
```
AWS Console → RDS → Databases → healink-dev-postgres →
  Check: Status = Available
  Check: Connections active
```

---

## 💡 Pro Tips

### 1. Speed Up Deployments
```yaml
# Use skip_build when only updating Terraform configs
Full Deploy → skip_build: true
# This skips Docker build phase (saves 5-8 minutes)
```

### 2. Save Money on Dev
```bash
# Automated nuke/deploy schedule (future enhancement)
# Add to workflow:
on:
  schedule:
    - cron: '0 18 * * 1-5'  # Nuke at 6PM weekdays
    - cron: '0 8 * * 1-5'   # Deploy at 8AM weekdays
```

### 3. Tag Docker Images
```bash
# Images are automatically tagged with:
# - latest
# - {environment} (dev/prod)
# - {commit-sha}

# Rollback to previous version:
# Update Terraform variable: image_tag = {old-commit-sha}
# Run "Full Deploy" with skip_build: true
```

### 4. Monitor Costs
```bash
# AWS Console → Cost Explorer → 
# Filter by tag: Environment = dev/prod
# Set budget alerts at $100/month
```

### 5. Backup Before Major Changes
```bash
# Backup RDS before deploying to prod
AWS Console → RDS → Healink DB → 
  Actions → Take snapshot
```

---

## 🆘 Emergency Procedures

### Emergency: Delete Everything Right Now
```bash
# 1. Nuke application layer
Run "Nuke AWS" workflow → Type "NUKE" → Run

# 2. Delete RDS manually (if needed)
AWS Console → RDS → Delete database → 
  Don't create snapshot if emergency

# 3. Delete ECR images (if needed)
AWS Console → ECR → Delete repository

# 4. Clean Terraform state
cd terraform_healink
./deploy.sh clean
```

### Emergency: Service Down in Production
```bash
# 1. Check ECS task logs
AWS Console → ECS → Cluster → Service → Tasks → Logs

# 2. Restart service
aws ecs update-service \
  --cluster healink-prod \
  --service {service-name} \
  --force-new-deployment

# 3. Scale down to 0, then scale up
aws ecs update-service \
  --cluster healink-prod \
  --service {service-name} \
  --desired-count 0

# Wait 30 seconds

aws ecs update-service \
  --cluster healink-prod \
  --service {service-name} \
  --desired-count 2
```

### Emergency: Rollback Deployment
```bash
# 1. Get previous commit SHA
git log --oneline

# 2. Update Terraform with old image tag
cd terraform_healink/app-infra
terraform apply -var="image_tag={old-commit-sha}"

# Or use GitHub Actions:
# Run "Full Deploy" → skip_build: true → 
# (Modify workflow to accept image_tag parameter)
```

---

## 📚 Common Commands Cheat Sheet

### GitHub Actions
```
Full Deploy:
  Actions → Full Deploy → Run workflow → Environment → Run

Nuke AWS:
  Actions → Nuke AWS → Type "NUKE" → Run
```

### AWS CLI
```bash
# List ECS services
aws ecs list-services --cluster healink-dev

# View service details
aws ecs describe-services --cluster healink-dev --services {service-name}

# View task logs (get task ID first)
aws ecs list-tasks --cluster healink-dev --service-name {service-name}
aws logs tail /ecs/healink-{service}-dev --follow

# List RDS instances
aws rds describe-db-instances

# List ECR repositories
aws ecr describe-repositories
```

### Terraform
```bash
cd terraform_healink

# Status
./deploy.sh status

# Deploy stateful only
./deploy.sh stateful-apply

# Deploy app only
./deploy.sh app-apply

# Destroy app (safe)
./deploy.sh app-destroy

# Full cycle test
./deploy.sh quick-test

# Clean everything
./deploy.sh clean
```

---

## 🎓 Learning Path

**Day 1**: First deployment
- Run "Full Deploy" to dev
- Explore AWS Console
- Check all services are running

**Day 2**: Cost optimization
- Run "Nuke AWS" at end of day
- Check cost reduction in AWS Console
- Redeploy next morning

**Day 3**: Debugging
- Intentionally break a service
- Use CloudWatch logs to debug
- Fix and redeploy

**Day 4**: Production deployment
- Test thoroughly in dev
- Deploy to prod
- Monitor for 24 hours

**Day 5**: Advanced operations
- Practice rollback
- Test auto-scaling
- Set up custom alerts

---

## ✅ Success Checklist

After running "Full Deploy":
- [ ] All 5 services show "Running" in ECS
- [ ] Health endpoints return 200 OK
- [ ] RabbitMQ is accessible
- [ ] Redis is connected
- [ ] RDS has all databases
- [ ] CloudWatch logs are flowing
- [ ] Cost < $100/month for dev

After running "Nuke AWS":
- [ ] ECS cluster deleted
- [ ] ALBs deleted
- [ ] CloudWatch logs deleted
- [ ] RDS still exists
- [ ] ECR repositories still exist
- [ ] Can redeploy anytime

---

**Need Help?**
- Check logs in CloudWatch
- Review Terraform outputs
- Ask in team chat
- Check AWS Service Health Dashboard

**Last Updated**: September 30, 2025
