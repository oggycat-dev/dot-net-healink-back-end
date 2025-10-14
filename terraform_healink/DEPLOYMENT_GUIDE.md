# 🚀 Quick Deployment Guide

## Option 1: GitHub Actions (Recommended)

### Step 1: Push Code
```bash
git add .
git commit -m "Updated Terraform configuration for all 7 microservices"
git push origin main
```

### Step 2: Run Workflow
1. Go to https://github.com/oggycat-dev/dot-net-healink-back-end/actions
2. Click "🚀 Full Deploy - All Services"
3. Click "Run workflow" button
4. Select:
   - **Branch:** `main`
   - **Environment:** `dev` (for testing) or `prod`
   - **Skip build:** `false` (first time)
5. Click "Run workflow"

### Step 3: Monitor
- Watch the workflow progress
- Check logs for each step
- Wait for all steps to complete (~15-20 minutes)

---

## Option 2: Local Deployment

### Prerequisites
```bash
# Install Terraform
brew install terraform

# Configure AWS credentials
aws configure
```

### Deploy Stateful Infrastructure (First Time Only)
```bash
cd terraform_healink/stateful-infra

# Initialize
terraform init -reconfigure

# Create/select workspace
terraform workspace new dev  # or: terraform workspace select dev

# Plan
terraform plan

# Apply
terraform apply
```

### Deploy Application Infrastructure
```bash
cd ../app-infra

# Initialize
terraform init -reconfigure

# Create/select workspace
terraform workspace new dev  # or: terraform workspace select dev

# Plan
terraform plan

# Apply
terraform apply
```

### Get Service URLs
```bash
# Still in app-infra directory
terraform output
```

---

## 🔍 Verify Deployment

### Check Terraform Outputs
```bash
cd terraform_healink/app-infra
terraform output
```

Expected outputs:
- `gateway_url` - http://healink-gateway-dev-*.ap-southeast-2.elb.amazonaws.com
- `auth_service_url` - http://healink-auth-service-dev-*.elb.amazonaws.com
- `user_service_url` - http://healink-user-service-dev-*.elb.amazonaws.com
- `content_service_url` - http://healink-content-service-dev-*.elb.amazonaws.com
- `notification_service_url` - http://healink-notification-service-dev-*.elb.amazonaws.com
- `subscription_service_url` - http://healink-subscription-service-dev-*.elb.amazonaws.com
- `payment_service_url` - http://healink-payment-service-dev-*.elb.amazonaws.com

### Health Check
```bash
# Test each service (replace URL with actual output)
curl http://your-gateway-url/health
curl http://your-auth-service-url/health
curl http://your-user-service-url/health
curl http://your-content-service-url/health
curl http://your-notification-service-url/health
curl http://your-subscription-service-url/health
curl http://your-payment-service-url/health
```

Expected response: `200 OK` or service-specific health status

---

## 🔧 Build & Push Docker Images Manually

### Prerequisites
```bash
# Login to ECR
aws ecr get-login-password --region ap-southeast-2 | docker login --username AWS --password-stdin 855160720656.dkr.ecr.ap-southeast-2.amazonaws.com
```

### Build All Services
```bash
# From project root
ECR_REGISTRY="855160720656.dkr.ecr.ap-southeast-2.amazonaws.com"

# AuthService
docker build --platform linux/amd64 -t $ECR_REGISTRY/healink/auth-service:latest -f src/AuthService/AuthService.API/Dockerfile .
docker push $ECR_REGISTRY/healink/auth-service:latest

# UserService
docker build --platform linux/amd64 -t $ECR_REGISTRY/healink/user-service:latest -f src/UserService/UserService.API/Dockerfile .
docker push $ECR_REGISTRY/healink/user-service:latest

# ContentService
docker build --platform linux/amd64 -t $ECR_REGISTRY/healink/content-service:latest -f src/ContentService/ContentService.API/Dockerfile .
docker push $ECR_REGISTRY/healink/content-service:latest

# NotificationService
docker build --platform linux/amd64 -t $ECR_REGISTRY/healink/notification-service:latest -f src/NotificaitonService/NotificaitonService.API/Dockerfile .
docker push $ECR_REGISTRY/healink/notification-service:latest

# SubscriptionService
docker build --platform linux/amd64 -t $ECR_REGISTRY/healink/subscription-service:latest -f src/SubscriptionService/SubscriptionService.API/Dockerfile .
docker push $ECR_REGISTRY/healink/subscription-service:latest

# PaymentService
docker build --platform linux/amd64 -t $ECR_REGISTRY/healink/payment-service:latest -f src/PaymentService/PaymentService.API/Dockerfile .
docker push $ECR_REGISTRY/healink/payment-service:latest

# Gateway
docker build --platform linux/amd64 -t $ECR_REGISTRY/healink/gateway:latest -f src/Gateway/Gateway.API/Dockerfile .
docker push $ECR_REGISTRY/healink/gateway:latest
```

---

## 🗑️ Destroy Infrastructure (Cleanup)

### Using GitHub Actions
1. Go to Actions → "☢️ Nuke AWS (Keep ECR & RDS)"
2. Click "Run workflow"
3. Type **"NUKE"** exactly
4. Select environment
5. Keep ECR images: `true` (recommended)
6. Keep RDS: `true` (recommended)
7. Click "Run workflow"

### Using Terraform Locally

#### Destroy Application Infrastructure
```bash
cd terraform_healink/app-infra
terraform workspace select dev
terraform destroy
```

#### Destroy Stateful Infrastructure (⚠️ Deletes database!)
```bash
cd ../stateful-infra
terraform workspace select dev
terraform destroy
```

---

## 🐛 Troubleshooting

### Issue: "Workspace does not exist"
```bash
terraform workspace list
terraform workspace new dev
```

### Issue: "Backend initialization failed"
```bash
terraform init -reconfigure -upgrade
```

### Issue: "ECR repository not found"
```bash
# Deploy stateful infrastructure first
cd terraform_healink/stateful-infra
terraform apply
```

### Issue: "ECS task failing to start"
```bash
# Check ECS logs in AWS Console
# Or use AWS CLI:
aws ecs describe-tasks --cluster healink-cluster-dev --tasks <task-id>
```

### Issue: "Health check failing"
- Verify service exposes `/health` endpoint
- Check security groups allow traffic
- Check CloudWatch logs for errors

---

## 📊 Cost Monitoring

### Check current costs
```bash
# AWS Cost Explorer
aws ce get-cost-and-usage \
  --time-period Start=2025-10-01,End=2025-10-14 \
  --granularity DAILY \
  --metrics UnblendedCost \
  --group-by Type=SERVICE
```

### Optimize costs
1. **Stop dev environment when not in use:**
   ```bash
   aws ecs update-service --cluster healink-cluster-dev --service healink-auth-service-dev --desired-count 0
   ```

2. **Use Fargate Spot for dev:**
   - Add `capacity_provider_strategy` in Terraform

3. **Consolidate ALBs:**
   - Use single ALB with path-based routing
   - Saves ~$96/month

---

## 🔒 Security Best Practices

### Move secrets to AWS Secrets Manager
```bash
# Create secret
aws secretsmanager create-secret \
  --name /healink/dev/database/password \
  --secret-string "YourSecurePassword"

# Update Terraform to use secrets
# Replace hardcoded passwords with:
# data "aws_secretsmanager_secret_version" "db_password" { ... }
```

### Enable encryption
- ✅ RDS: `storage_encrypted = true`
- ⚠️ Redis: `at_rest_encryption_enabled = false` (change to true for prod)
- ⚠️ Redis: `transit_encryption_enabled = false` (change to true for prod)

---

## 📝 Next Steps

1. ✅ Deploy to `dev` environment first
2. ✅ Test all services
3. ✅ Run integration tests
4. ✅ Monitor for 24 hours
5. ✅ Deploy to `prod` environment
6. 🔄 Set up monitoring and alerts
7. 🔄 Configure auto-scaling
8. 🔄 Add CI/CD automated testing

---

**Quick Links:**
- GitHub Actions: https://github.com/oggycat-dev/dot-net-healink-back-end/actions
- AWS Console: https://ap-southeast-2.console.aws.amazon.com/
- Terraform Docs: https://registry.terraform.io/providers/hashicorp/aws/latest/docs
