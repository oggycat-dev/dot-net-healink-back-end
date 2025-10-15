# 🚀 Hướng Dẫn Deploy Healink Microservices - Free Tier

**Cập nhật:** October 15, 2025  
**Môi trường:** FREE (tối ưu AWS Free Tier)  
**Tổng số services:** 8

---

## 📋 TÓM TẮT

Hệ thống đã được tối ưu để sử dụng **MỘT môi trường duy nhất "free"** với cấu hình AWS Free Tier rẻ nhất.

### Kiến trúc triển khai:
```
1. Stateful Infrastructure (RDS, Redis, RabbitMQ, ECR)
   ↓
2. Build Docker Images (8 services)
   ↓
3. Application Infrastructure (ECS, ALB)
   ↓
4. Health Check
```

### 8 Microservices:
1. ✅ **Gateway** - API Gateway
2. ✅ **AuthService** - Authentication & Authorization
3. ✅ **UserService** - User Management
4. ✅ **ContentService** - Content & Media
5. ✅ **NotificationService** - Notifications
6. ✅ **SubscriptionService** - Subscription Management
7. ✅ **PaymentService** - Payment Processing
8. ✅ **PodcastRecommendationService** - AI Recommendations

---

## 🎯 CÁCH CHẠY CI/CD

### Option 1: GitHub Actions (Khuyến nghị)

```bash
# 1. Push code lên GitHub (nếu chưa)
git push origin main

# 2. Vào GitHub Actions
https://github.com/YOUR_USERNAME/dot-net-healink-back-end/actions

# 3. Chọn workflow "🚀 Deploy Healink - Free Tier"

# 4. Click "Run workflow"
   - Branch: main
   - ⏭️ Skip Docker build: false (lần đầu)
   - ⏭️ Skip stateful deploy: false (lần đầu)

# 5. Click "Run workflow" để bắt đầu
```

### Option 2: GitHub CLI (nhanh hơn)

```bash
# Install GitHub CLI nếu chưa có
brew install gh  # macOS
# hoặc: https://cli.github.com/

# Login
gh auth login

# Chạy workflow
gh workflow run "🚀 Deploy Healink - Free Tier" \
  --ref main \
  -f skip_build=false \
  -f skip_stateful=false

# Theo dõi tiến trình
gh run watch
```

### Option 3: Local Terraform (cho dev)

```bash
# Step 1: Deploy Stateful Infrastructure
cd terraform_healink/stateful-infra
terraform init -reconfigure
terraform workspace select free || terraform workspace new free
terraform apply -var-file=../free-tier.tfvars

# Step 2: Build & Push Images (manual)
# Xem phần "Build Local" bên dưới

# Step 3: Deploy Application
cd ../app-infra
terraform init -reconfigure
terraform workspace select free || terraform workspace new free
terraform apply \
  -var="image_tag=latest" \
  -var="environment=free" \
  -var-file=../free-tier.tfvars
```

---

## 📦 CHI TIẾT WORKFLOW

### Step 1: Stateful Infrastructure (5-10 phút)
```
✅ Create RDS PostgreSQL (db.t3.micro - FREE TIER)
✅ Create ElastiCache Redis (cache.t3.micro)
✅ Create Amazon MQ RabbitMQ (mq.t3.micro)
✅ Create 8 ECR Repositories
✅ Create Security Groups
```

### Step 2: Build Docker Images (15-20 phút)
```
✅ Build Gateway
✅ Build AuthService
✅ Build UserService
✅ Build ContentService
✅ Build NotificationService
✅ Build SubscriptionService
✅ Build PaymentService
✅ Build PodcastRecommendationService
✅ Push all to ECR with tags: latest, free, commit-sha
```

### Step 3: Application Infrastructure (10-15 phút)
```
✅ Create ECS Cluster (healink-cluster-free)
✅ Create 8 ECS Services
✅ Create 1 Application Load Balancer (Gateway only)
✅ 7 Internal Services (no ALB - cost optimized!)
✅ Create Target Group
✅ Setup CloudWatch Logs for all services
```

### Step 4: Health Check (2-3 phút)
```
✅ Wait 90 seconds for tasks to start
✅ Check ECS task status
✅ List running services
```

**Tổng thời gian:** ~30-50 phút

---

## 💰 CHI PHÍ DỰ KIẾN

### ✅ Đã tối ưu: Chỉ 1 ALB cho Gateway!

### Trong 12 tháng đầu (Free Tier):
| Resource | Config | Free Tier | Cost |
|----------|--------|-----------|------|
| RDS PostgreSQL | db.t3.micro, 20GB | 750 hrs/month | **$0** |
| ElastiCache Redis | cache.t3.micro | ❌ Not free | **~$12/month** |
| Amazon MQ | mq.t3.micro | ❌ Not free | **~$18/month** |
| ECS Fargate | 8 services × 256 CPU/512 MB | Partial | **~$30/month** |
| ALB | **1 ALB** (Gateway only) | 750 hrs/month | **$0** ✅ |
| **TOTAL** | | | **~$60/month** 🎉 |

### Sau 12 tháng:
- RDS thêm ~$15/month
- ALB thêm ~$16/month
- **TOTAL: ~$91/month**

### 💡 Tiết kiệm so với kiến trúc cũ:
- ❌ Cũ: 8 ALBs = $112/month
- ✅ Mới: 1 ALB = $0 (Free Tier) hoặc $16/month
- **Tiết kiệm: $96/month!**

### 💡 Cách tiết kiệm:
```bash
# 1. Tắt services khi không dùng
aws ecs update-service \
  --cluster healink-cluster-free \
  --service healink-auth-service-free \
  --desired-count 0

# 2. Hoặc dùng workflow "Nuke AWS" để xóa toàn bộ
gh workflow run "💣 Nuke AWS (Keep ECR & RDS)"

# 3. Deploy lại khi cần test
gh workflow run "🚀 Deploy Healink - Free Tier" -f skip_build=true
```

---

## 🔧 TÙY CHỌN DEPLOY

### Lần đầu deploy:
```bash
gh workflow run "🚀 Deploy Healink - Free Tier" \
  -f skip_build=false \
  -f skip_stateful=false
```

### Deploy lại với images cũ (chỉ update config):
```bash
gh workflow run "🚀 Deploy Healink - Free Tier" \
  -f skip_build=true \
  -f skip_stateful=true
```

### Chỉ rebuild images:
```bash
gh workflow run "🚀 Deploy Healink - Free Tier" \
  -f skip_build=false \
  -f skip_stateful=true
```

---

## 🐛 TROUBLESHOOTING

### Issue: ECR repository không tồn tại
```
Error: name unknown: The repository with name 'healink/gateway' does not exist
```

**Giải pháp:**
```bash
# Deploy stateful trước để tạo ECR repos
cd terraform_healink/stateful-infra
terraform apply -var-file=../free-tier.tfvars
```

### Issue: Docker build thất bại
```
Error: failed to solve: failed to compute cache key
```

**Giải pháp:**
```bash
# Verify Dockerfile tồn tại
ls -la src/AuthService/AuthService.API/Dockerfile

# Test build local
docker build -t test -f src/AuthService/AuthService.API/Dockerfile .
```

### Issue: ECS tasks không start
```
Error: Task failed to start
```

**Giải pháp:**
```bash
# Check CloudWatch logs
aws logs tail /ecs/healink-auth-service-free --follow --region ap-southeast-2

# Check ECS task status
aws ecs describe-tasks \
  --cluster healink-cluster-free \
  --region ap-southeast-2
```

### Issue: Terraform state lock
```
Error: Error acquiring the state lock
```

**Giải pháp:**
```bash
# Force unlock (cẩn thận!)
terraform force-unlock LOCK_ID

# Hoặc đợi 5-10 phút cho lock tự hết
```

---

## 📊 MONITORING

### Check deployment status:
```bash
# AWS Console
https://ap-southeast-2.console.aws.amazon.com/ecs/

# CloudWatch Logs
https://ap-southeast-2.console.aws.amazon.com/cloudwatch/

# ECR Images
https://ap-southeast-2.console.aws.amazon.com/ecr/
```

### Get service URLs:
```bash
cd terraform_healink/app-infra
terraform workspace select free
terraform output

# Output sẽ hiển thị:
# gateway_url = "http://healink-gateway-fre-*.elb.amazonaws.com"
# 7 internal services không có public URL
```

### Test health endpoints:
```bash
# Chỉ Gateway có public URL
GATEWAY_URL=$(cd terraform_healink/app-infra && terraform output -raw gateway_url)

# Test Gateway
curl $GATEWAY_URL/health

# Internal services chỉ accessible qua Gateway hoặc internal VPC
# Không thể test trực tiếp từ internet (cost optimized!)
```

---

## 🔄 UPDATE CODE VÀ REDEPLOY

### Workflow nhanh:
```bash
# 1. Update code
git add .
git commit -m "feat: update feature X"
git push origin main

# 2. Rebuild & deploy
gh workflow run "🚀 Deploy Healink - Free Tier" \
  -f skip_stateful=true

# 3. Theo dõi
gh run watch
```

### Chỉ update một service:
```bash
# Build local
docker build -t 855160720656.dkr.ecr.ap-southeast-2.amazonaws.com/healink/auth-service:latest \
  -f src/AuthService/AuthService.API/Dockerfile .

# Login ECR
aws ecr get-login-password --region ap-southeast-2 | \
  docker login --username AWS --password-stdin 855160720656.dkr.ecr.ap-southeast-2.amazonaws.com

# Push
docker push 855160720656.dkr.ecr.ap-southeast-2.amazonaws.com/healink/auth-service:latest

# Force new deployment
aws ecs update-service \
  --cluster healink-cluster-free \
  --service healink-auth-service-free \
  --force-new-deployment \
  --region ap-southeast-2
```

---

## 🧹 DỌN DẸP TÀI NGUYÊN

### Option 1: Xóa Application (giữ Stateful)
```bash
cd terraform_healink/app-infra
terraform workspace select free
terraform destroy -var-file=../free-tier.tfvars

# Tiết kiệm ~$142/month (chỉ giữ RDS, Redis, MQ)
```

### Option 2: Xóa toàn bộ
```bash
# Xóa Application trước
cd terraform_healink/app-infra
terraform destroy -var-file=../free-tier.tfvars

# Xóa Stateful
cd ../stateful-infra
terraform destroy -var-file=../free-tier.tfvars

# Tiết kiệm 100% chi phí
```

### Option 3: Dùng Nuke Workflow
```bash
gh workflow run "💣 Nuke AWS (Keep ECR & RDS)"
```

---

## 📝 FILE CẤU HÌNH

### free-tier.tfvars
```hcl
environment = "dev"
project_name = "healink-free"

# FREE TIER OPTIMIZED
db_instance_class = "db.t3.micro"      # FREE TIER
db_allocated_storage = 20               # FREE TIER limit
db_backup_retention_period = 1

redis_node_type = "cache.t3.micro"     # Smallest
mq_instance_type = "mq.t3.micro"       # Smallest
mq_deployment_mode = "SINGLE_INSTANCE"

ecs_task_cpu = "256"                   # 0.25 vCPU
ecs_task_memory = "512"                # 0.5 GB
ecs_desired_count = 1                  # 1 task per service

aspnetcore_environment = "Development"
allowed_origins = "http://localhost:3000,http://localhost:8080"
```

---

## ✅ CHECKLIST TRƯỚC KHI DEPLOY

- [ ] Code đã push lên GitHub
- [ ] AWS credentials configured
- [ ] S3 backend bucket tồn tại: `healink-tf-state-2025-oggycatdev`
- [ ] VPC và Subnets đã được tạo
- [ ] Tất cả 8 Dockerfiles tồn tại
- [ ] GitHub Actions có quyền access AWS (IAM Role)

---

## 🆘 HỖ TRỢ

### Documentation:
- [READY_TO_DEPLOY.md](./READY_TO_DEPLOY.md) - Checklist đầy đủ
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md) - Chi tiết từng bước
- [free-tier.tfvars](./free-tier.tfvars) - Cấu hình chi tiết

### AWS Console:
- [ECS Dashboard](https://ap-southeast-2.console.aws.amazon.com/ecs/)
- [CloudWatch Logs](https://ap-southeast-2.console.aws.amazon.com/cloudwatch/)
- [Cost Explorer](https://us-east-1.console.aws.amazon.com/cost-management/)

---

## 🌐 LẤY API ENDPOINTS CHO TEAM FRONTEND

### Option 1: Từ GitHub Actions (Khuyến nghị)

Sau khi workflow chạy xong:

```bash
# 1. Vào GitHub Actions workflow run
# 2. Scroll xuống "Artifacts" section
# 3. Download "api-endpoints-{commit-sha}"
# 4. Giải nén và copy file .env vào frontend project
```

Files trong artifact:
- `api-endpoints.json` - JSON configuration
- `.env.production` - React/Next.js env variables

### Option 2: Sử dụng script local

```bash
# Chạy script
./scripts/get-api-endpoints.sh

# Output trong thư mục: api-endpoints/
# - api-endpoints.json
# - .env.production.react
# - .env.production.next
# - .env.production.vue
# - api-config.ts
```

### Option 3: GitHub Actions Summary

Mỗi lần deploy, check **Summary** tab của workflow run:
- Gateway URL hiển thị rõ ràng
- Copy/paste trực tiếp vào frontend code
- Có sẵn examples cho React, Next.js, Vue

### Option 4: Terraform output trực tiếp

```bash
cd terraform_healink/app-infra
terraform workspace select free
terraform output gateway_url
```

---

## 🎉 DEPLOYMENT THÀNH CÔNG!

Sau khi workflow hoàn thành, bạn sẽ có:

✅ **8 Microservices** chạy trên ECS Fargate  
✅ **1 Public Gateway** với ALB (điểm vào duy nhất)  
✅ **7 Internal Services** (không public, tiết kiệm chi phí)  
✅ **RDS PostgreSQL** shared database  
✅ **Redis Cache** để tăng tốc  
✅ **RabbitMQ** cho messaging  
✅ **CloudWatch Logs** để monitoring  

**Kiến trúc:**
```
Internet → Gateway ALB → Gateway Service
                            ↓
                    Internal Services (7)
                    - AuthService
                    - UserService
                    - ContentService
                    - NotificationService
                    - SubscriptionService
                    - PaymentService
                    - PodcastRecommendationService
```

**Chi phí:** ~$60/month (Free Tier) hoặc ~$91/month (sau 12 tháng)  
**Tiết kiệm:** $96/month so với kiến trúc 8 ALBs!

**Next steps:**
1. Test health endpoints
2. Chạy integration tests
3. Monitor CloudWatch logs 24h
4. Optimize costs nếu cần

**Good luck! 🚀**
