# 🚀 Hướng Dẫn Chạy CI/CD - Healink Microservices

**Date:** October 14, 2025  
**Branch:** features/oggy  
**Commit:** 330aadc

---

## ✅ ĐÃ PUSH LÊN GITHUB

Code và AI models đã được push lên GitHub thành công!

**Các file AI models đã bao gồm:**
- ✅ `collaborative_filtering_model.h5` (2.02 MB)
- ✅ `mappings.pkl`
- ✅ `podcasts.pkl`
- ✅ `ratings.pkl`
- ✅ `users.pkl`

---

## 🎯 CÁCH CHẠY CI/CD

### Option 1: Merge vào Main để Deploy (Khuyến nghị)

```bash
# 1. Tạo Pull Request từ features/oggy sang main
# Vào GitHub: https://github.com/oggycat-dev/dot-net-healink-back-end

# 2. Review và merge PR

# 3. Sau khi merge, chạy workflow manual:
# - Vào Actions tab
# - Chọn "🚀 Full Deploy - All Services"
# - Click "Run workflow"
# - Chọn branch: main
# - Chọn environment: dev
# - Click "Run workflow"
```

### Option 2: Chạy Trực Tiếp Từ Branch features/oggy

```bash
# 1. Vào GitHub Actions
https://github.com/oggycat-dev/dot-net-healink-back-end/actions

# 2. Chọn workflow "🚀 Full Deploy - All Services"

# 3. Click "Run workflow"

# 4. Cấu hình:
   Branch: features/oggy
   Environment: dev
   Skip build: false

# 5. Click "Run workflow" để bắt đầu
```

---

## 📦 Workflow Sẽ Làm Gì?

### Step 1: Build Services (10-15 phút)
```
✅ Build AuthService Docker image
✅ Build UserService Docker image
✅ Build ContentService Docker image
✅ Build NotificationService Docker image
✅ Build SubscriptionService Docker image
✅ Build PaymentService Docker image
✅ Build Gateway Docker image
✅ Push tất cả images lên ECR
```

### Step 2: Deploy Stateful Infrastructure (5-10 phút)
```
✅ Create/Update RDS PostgreSQL
✅ Create/Update ElastiCache Redis
✅ Create/Update Amazon MQ RabbitMQ
✅ Create/Update 7 ECR Repositories
```

### Step 3: Deploy Application Infrastructure (5-10 phút)
```
✅ Create/Update ECS Cluster
✅ Create/Update 7 ECS Services
✅ Create/Update 7 Application Load Balancers
✅ Create/Update CloudWatch Log Groups
✅ Configure Security Groups
```

### Step 4: Health Check (2-3 phút)
```
✅ Wait for services to start
✅ Verify all tasks are running
✅ Check health endpoints
```

**Tổng thời gian:** ~25-40 phút

---

## 🔍 MONITORING DEPLOYMENT

### 1. Xem Progress trong GitHub Actions
- Vào: https://github.com/oggycat-dev/dot-net-healink-back-end/actions
- Click vào workflow đang chạy
- Xem logs của từng step

### 2. Check AWS Console
```
ECS Dashboard:
https://ap-southeast-2.console.aws.amazon.com/ecs/

CloudWatch Logs:
https://ap-southeast-2.console.aws.amazon.com/cloudwatch/

ECR Repositories:
https://ap-southeast-2.console.aws.amazon.com/ecr/
```

### 3. Get Service URLs
Sau khi deploy xong, vào Actions workflow output để lấy URLs:
```
gateway_url: http://healink-gateway-dev-*.elb.amazonaws.com
auth_service_url: http://healink-auth-service-dev-*.elb.amazonaws.com
user_service_url: http://healink-user-service-dev-*.elb.amazonaws.com
content_service_url: http://healink-content-service-dev-*.elb.amazonaws.com
notification_service_url: http://healink-notification-service-dev-*.elb.amazonaws.com
subscription_service_url: http://healink-subscription-service-dev-*.elb.amazonaws.com
payment_service_url: http://healink-payment-service-dev-*.elb.amazonaws.com
```

---

## 🧪 TEST DEPLOYMENT

### Test Health Endpoints
```bash
# Gateway
curl http://your-gateway-url/health

# AuthService
curl http://your-auth-service-url/health

# UserService
curl http://your-user-service-url/health

# ContentService
curl http://your-content-service-url/health

# NotificationService
curl http://your-notification-service-url/health

# SubscriptionService
curl http://your-subscription-service-url/health

# PaymentService
curl http://your-payment-service-url/health
```

### Test AI Recommendation (Local)
```bash
# Recommendation service chạy local trong Docker Compose
curl http://localhost:8000/recommendations/me?num_recommendations=5

# Expected: JSON response với camelCase format
{
  "userId": "me",
  "recommendations": [
    {
      "podcastId": "...",
      "predictedRating": 4.5,
      "title": "...",
      ...
    }
  ]
}
```

---

## 🐛 TROUBLESHOOTING

### Issue: Workflow Failed at Build Step
```bash
# Check logs trong GitHub Actions
# Thường do:
1. Dockerfile không tồn tại
2. Build context sai
3. Dependencies không resolve được

# Solution:
- Verify Dockerfile paths
- Test build locally:
  docker build -t test -f src/AuthService/AuthService.API/Dockerfile .
```

### Issue: Workflow Failed at Deploy Stateful
```bash
# Check Terraform logs
# Thường do:
1. S3 backend bucket không tồn tại
2. VPC/Subnet IDs sai
3. AWS credentials invalid

# Solution:
- Verify S3 bucket: healink-tf-state-2025-oggycatdev
- Check VPC ID trong stateful-infra/main.tf
- Verify AWS credentials trong GitHub Secrets
```

### Issue: Workflow Failed at Deploy Application
```bash
# Check Terraform logs
# Thường do:
1. ECR images chưa được push
2. Stateful infrastructure chưa deploy
3. Resource limits exceeded

# Solution:
- Run stateful deployment first
- Check ECR images exist
- Review AWS service quotas
```

### Issue: ECS Tasks Failing to Start
```bash
# Check CloudWatch logs:
aws logs tail /ecs/healink-auth-service-dev --follow

# Common causes:
1. Image not found in ECR
2. Environment variables missing
3. Database connection failed
4. Insufficient CPU/Memory

# Solution:
- Verify image tag matches
- Check environment variables in Terraform
- Verify RDS is running and accessible
```

---

## 💰 COST MONITORING

### Check Costs During Deployment
```bash
# AWS Cost Explorer
aws ce get-cost-and-usage \
  --time-period Start=2025-10-14,End=2025-10-15 \
  --granularity DAILY \
  --metrics UnblendedCost \
  --group-by Type=SERVICE
```

### Expected Costs (Dev Environment)
```
During Free Tier (12 months):
- RDS: $0 (db.t3.micro free)
- ALB: $0 (750 hrs free, but need 7 ⚠️)
- ECS: Partial free (20GB storage)
- ElastiCache: ~$12/month
- Amazon MQ: ~$18/month
Total: ~$30-40/month

After Free Tier:
Total: ~$192-213/month
```

---

## 🎯 NEXT STEPS AFTER DEPLOYMENT

### 1. Verify All Services
```bash
# Create a test script
for service in gateway auth user content notification subscription payment; do
  echo "Testing $service..."
  curl -I http://healink-$service-dev-*.elb.amazonaws.com/health
done
```

### 2. Run Integration Tests
```bash
# Test user registration flow
# Test authentication
# Test content upload
# Test subscription payment
```

### 3. Monitor for 24 Hours
```bash
# Check CloudWatch metrics
# Monitor ECS service health
# Review application logs
# Track API response times
```

### 4. Optimize Costs
```bash
# Stop services when not in use:
aws ecs update-service \
  --cluster healink-cluster-dev \
  --service healink-auth-service-dev \
  --desired-count 0

# Repeat for other services
```

### 5. Plan Production Deployment
```bash
# After dev is stable:
1. Update prod.tfvars with production settings
2. Run workflow with environment: prod
3. Enable auto-scaling
4. Configure monitoring and alarms
5. Set up disaster recovery
```

---

## 📝 IMPORTANT NOTES

### AI Models
- ✅ Models đã được push lên git (2.02 MB)
- ⚠️ Đây là test models, chưa optimize
- 🔄 TODO: Move to S3 or Git LFS for production

### Secrets Management
- ⚠️ Database passwords đang hardcoded trong Terraform
- 🔒 TODO: Move to AWS Secrets Manager before prod

### Redis Encryption
- ⚠️ Disabled trong dev để tiết kiệm chi phí
- 🔒 TODO: Enable cho production environment

### PodcastRecommendationService
- ⚠️ Hiện tại chạy local trong Docker Compose
- 🔄 TODO: Add ECS deployment cho Python service

---

## 🆘 NEED HELP?

### Documentation
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md) - Chi tiết từng bước
- [TERRAFORM_UPDATE_SUMMARY.md](./TERRAFORM_UPDATE_SUMMARY.md) - Tổng hợp thay đổi
- [READY_TO_DEPLOY.md](./READY_TO_DEPLOY.md) - Checklist đầy đủ

### GitHub
- Actions: https://github.com/oggycat-dev/dot-net-healink-back-end/actions
- Issues: https://github.com/oggycat-dev/dot-net-healink-back-end/issues

### AWS Console
- ECS: https://ap-southeast-2.console.aws.amazon.com/ecs/
- CloudWatch: https://ap-southeast-2.console.aws.amazon.com/cloudwatch/
- Costs: https://us-east-1.console.aws.amazon.com/cost-management/

---

## ✅ READY TO GO!

Code đã push lên GitHub, AI models đã bao gồm, và bạn sẵn sàng chạy CI/CD!

**Quick Start:**
1. Vào GitHub Actions
2. Run "🚀 Full Deploy - All Services"
3. Chọn branch: features/oggy hoặc main
4. Chọn environment: dev
5. Đợi ~30 phút
6. Test services!

**Good luck! 🚀**
