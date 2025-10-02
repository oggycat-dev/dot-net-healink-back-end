# 💰 AWS Free Tier Optimization Guide - Healink

## 🎯 Mục Tiêu: Chi Phí Thấp Nhất Có Thể

**Tình huống**: Bạn có AWS Free Tier (12 tháng miễn phí)  
**Mục tiêu**: Tối ưu chi phí xuống **$5-10/tháng** hoặc ít hơn

---

## 📊 AWS Free Tier - Những Gì Miễn Phí

### ✅ Miễn Phí 12 Tháng Đầu

| Dịch vụ | Giới hạn Free Tier | Giá trị |
|---------|-------------------|---------|
| **RDS** | 750 giờ/tháng db.t3.micro | ~$12/tháng |
| **ALB** | 750 giờ/tháng + 15 GB data | ~$16/tháng |
| **EC2** | 750 giờ/tháng t2.micro | ~$8/tháng |
| **S3** | 5 GB storage + 20k GET + 2k PUT | ~$0.50/tháng |
| **CloudWatch** | 10 metrics + 10 alarms | ~$1/tháng |
| **Data Transfer** | 100 GB outbound | ~$9/tháng |
| **Total** | | **~$46.50/tháng MIỄN PHÍ** |

### ❌ KHÔNG Miễn Phí (Phải Trả)

| Dịch vụ | Instance Nhỏ Nhất | Chi Phí/Tháng |
|---------|-------------------|---------------|
| **ElastiCache (Redis)** | cache.t3.micro | ~$12 |
| **Amazon MQ (RabbitMQ)** | mq.t3.micro | ~$24 |
| **ECS Fargate** | 0.25 vCPU + 0.5 GB | ~$7/service |
| **NAT Gateway** | Single AZ | ~$32 |

---

## 🚨 Lỗi Quan Trọng Trong Config Hiện Tại

### ❌ Đang Dùng: `db.t4g.micro`
```terraform
db_instance_class = "db.t4g.micro"  # ❌ KHÔNG Free Tier!
```

**Vấn đề**:
- `t4g` = ARM-based (Graviton2) - rẻ hơn nhưng **KHÔNG** nằm trong Free Tier
- Chi phí: ~$10/tháng ngay cả trong 12 tháng đầu

### ✅ Phải Dùng: `db.t3.micro`
```terraform
db_instance_class = "db.t3.micro"  # ✅ Free Tier eligible!
```

**Lợi ích**:
- `t3` = x86-based - nằm trong Free Tier
- Chi phí: **$0/tháng** (12 tháng đầu)
- Sau 12 tháng: ~$12/tháng

---

## 💡 Chiến Lược Tối Ưu Chi Phí

### 🏆 **Option 1: Hybrid Development (RECOMMENDED)**
**Chi phí: $5-10/tháng**

```yaml
Môi trường:
  Local (Docker Compose):
    ✅ PostgreSQL       # Free
    ✅ Redis            # Free  
    ✅ RabbitMQ         # Free
    ✅ All Services     # Free
  
  AWS (Chỉ khi test):
    ✅ RDS t3.micro     # Free (12 months)
    ✅ ALB              # Free (12 months)
    ❌ ElastiCache      # Skip (use local)
    ❌ Amazon MQ        # Skip (use local)
    ❌ ECS Fargate      # Deploy only when testing

Workflow:
  1. Develop locally với docker-compose (FREE)
  2. Test locally trước (FREE)
  3. Deploy lên AWS chỉ khi cần test cloud (FEW HOURS)
  4. Nuke AWS sau khi test xong (SAVE MONEY)
```

**Commands**:
```bash
# Daily development (local - FREE)
./scripts/local-dev.sh start
# ... develop and test ...
./scripts/local-dev.sh stop

# Weekly AWS testing (deploy few hours)
# GitHub Actions → Full Deploy → Wait 2 hours for testing
# GitHub Actions → Nuke AWS → Done

# Monthly cost: ~$5-10 (only ECS Fargate for few hours)
```

---

### 💰 **Option 2: Full Local Development**
**Chi phí: $0/tháng**

```yaml
Tất cả chạy local:
  ✅ docker-compose.yml
  ✅ Không deploy AWS
  ✅ Chỉ deploy khi demo hoặc production

Chi phí:
  - Development: $0/tháng
  - Testing: $0/tháng
  - Deploy for demo: $5-10/demo (destroy after)
```

**Khi nào dùng**: Đang phát triển, chưa cần cloud testing

---

### ⚡ **Option 3: Smart Deploy Schedule**
**Chi phí: $15-25/tháng (trong 12 tháng đầu)**

```yaml
Lịch deploy thông minh:
  - Chỉ deploy 8 giờ/ngày (work hours)
  - Nuke mỗi tối và cuối tuần
  - 8h × 22 ngày = 176 giờ/tháng
  
Tính toán:
  - RDS: FREE (750h limit, dùng 176h)
  - ALB: FREE (750h limit, dùng 176h)  
  - ECS Fargate: $37/month × (176/730) = ~$9/month
  - ElastiCache: Skip (use local) = $0
  - Amazon MQ: Skip (use local) = $0
  - Total: ~$9/month
```

**Automation**:
```bash
# Sáng (8 AM): Auto deploy
./scripts/healink-manager.sh start dev

# Tối (6 PM): Auto destroy
./scripts/healink-manager.sh destroy dev
```

---

## 🛠️ Cấu Hình Tối Ưu

### 1️⃣ Cập Nhật `dev.tfvars` (Đã Fix)

```terraform
# ✅ ĐÚNG - Free Tier Eligible
db_instance_class = "db.t3.micro"      # FREE for 12 months
db_allocated_storage = 20              # FREE limit
db_backup_retention_period = 1         # Minimize backup

# ❌ SAI - Không Free Tier
# db_instance_class = "db.t4g.micro"   # NOT Free Tier
```

### 2️⃣ Tạo `free-tier.tfvars` (Đã Tạo)

File mới với cấu hình tối ưu 100% Free Tier:
```bash
./scripts/healink-manager.sh create dev -var-file=free-tier.tfvars
```

### 3️⃣ Sử Dụng Local Services

**Update `docker-compose.yml`**:
```yaml
services:
  # Local development (FREE)
  postgres:
    image: postgres:16
    # ... configuration ...
  
  redis:
    image: redis:7-alpine
    # ... configuration ...
  
  rabbitmq:
    image: rabbitmq:3-management-alpine
    # ... configuration ...
```

**Deploy Strategy**:
```bash
# Option A: Full local (FREE)
./scripts/local-dev.sh start

# Option B: Hybrid (RDS in cloud, rest local)
# 1. Deploy only RDS to AWS
# 2. Connect from local services
# 3. Cost: $0 for 12 months (Free Tier)
```

---

## 📊 So Sánh Chi Phí

### Tháng 1-12 (Free Tier Active)

| Chiến Lược | RDS | Redis | RabbitMQ | Fargate | ALB | **Total** |
|------------|-----|-------|----------|---------|-----|-----------|
| **Full AWS** | $0 | $12 | $24 | $37 | $0 | **$73/month** |
| **Hybrid** | $0 | $0 | $0 | $9 | $0 | **$9/month** |
| **Full Local** | $0 | $0 | $0 | $0 | $0 | **$0/month** |

### Tháng 13+ (After Free Tier)

| Chiến Lược | RDS | Redis | RabbitMQ | Fargate | ALB | **Total** |
|------------|-----|-------|----------|---------|-----|-----------|
| **Full AWS** | $12 | $12 | $24 | $37 | $16 | **$101/month** |
| **Hybrid** | $12 | $0 | $0 | $9 | $0 | **$21/month** |
| **Full Local** | $0 | $0 | $0 | $0 | $0 | **$0/month** |

---

## 🎯 Action Plan - Tiết Kiệm Tối Đa

### Bước 1: Fix RDS Configuration ✅
```bash
# Đã fix trong dev.tfvars
# db.t4g.micro → db.t3.micro
```

### Bước 2: Chọn Chiến Lược

#### 🏆 Recommended: **Hybrid Development**
```bash
# 1. Setup local development
./scripts/local-dev.sh start

# 2. Develop locally (most of the time)
# ... code, test, commit ...

# 3. Deploy to AWS only for cloud testing (once a week)
# GitHub Actions → Full Deploy
# ... test for 2-3 hours ...
# GitHub Actions → Nuke AWS

# 4. Monthly cost: $5-10 (mostly free)
```

### Bước 3: Deploy với Free Tier Config

```bash
# Option A: Use dev.tfvars (đã fix)
./scripts/healink-manager.sh create dev

# Option B: Use free-tier.tfvars (optimal)
cd terraform_healink
terraform init
terraform workspace select dev || terraform workspace new dev
terraform apply -var-file=free-tier.tfvars

# GitHub Actions: Update workflow to use free-tier.tfvars
```

### Bước 4: Monitor Costs

```bash
# AWS Console → Billing Dashboard
# Set Budget Alert: $10/month
# Track daily costs
# Destroy when not using!
```

---

## 🚀 GitHub Actions với Free Tier

### Update `full-deploy.yml`

```yaml
# Add Free Tier deployment option
on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Environment'
        required: true
        type: choice
        options:
          - dev
          - free-tier  # NEW: Use free-tier.tfvars
          - prod
```

### Update Terraform Apply Step

```yaml
- name: 🚀 Deploy Stateful Infrastructure
  run: |
    if [ "${{ inputs.environment }}" == "free-tier" ]; then
      terraform apply -var-file=free-tier.tfvars -auto-approve
    else
      terraform apply -var-file=${{ inputs.environment }}.tfvars -auto-approve
    fi
```

---

## 📋 Checklist Tối Ưu Free Tier

### ✅ RDS Configuration
- [x] Đổi `db.t4g.micro` → `db.t3.micro`
- [x] Storage: 20 GB (Free Tier limit)
- [x] Backup retention: 1 day (minimize costs)
- [x] Multi-AZ: Disabled (not free)
- [x] Public access: Disabled (security + free)

### ✅ ElastiCache (Redis)
- [ ] Option 1: Skip (use local Redis) - **RECOMMENDED**
- [ ] Option 2: Use cache.t3.micro ($12/month)
- [ ] Option 3: Use Memcached (cheaper alternative)

### ✅ Amazon MQ (RabbitMQ)
- [ ] Option 1: Skip (use local RabbitMQ) - **RECOMMENDED**
- [ ] Option 2: Use mq.t3.micro ($24/month)
- [ ] Option 3: Use SNS/SQS (cheaper alternative)

### ✅ ECS Fargate
- [x] Use minimum: 256 CPU + 512 MB
- [x] Desired count: 1 task per service
- [ ] Consider Fargate Spot (70% cheaper)
- [ ] Deploy only when testing

### ✅ ALB
- [x] Use 1 ALB for all services (not 5)
- [x] Free Tier: 750 hours/month
- [x] After 12 months: $16/month

### ✅ Monitoring
- [x] CloudWatch: 10 free metrics
- [x] Set budget alert: $10/month
- [x] Track daily costs
- [x] Review monthly bill

---

## 💡 Pro Tips

### 1. Sử Dụng Local Development
```bash
# 90% thời gian: Local (FREE)
./scripts/local-dev.sh start

# 10% thời gian: AWS (deploy khi cần test cloud)
# GitHub Actions → Full Deploy → Test → Nuke
```

### 2. Deploy Schedule
```bash
# Chỉ deploy khi:
- Demo cho khách hàng (1-2 giờ)
- Testing cloud integration (2-3 giờ/tuần)
- Staging trước production (1 ngày/tháng)

# Còn lại: Destroy để save money!
```

### 3. Use RDS Snapshots
```bash
# Trước khi destroy:
aws rds create-db-snapshot \
  --db-instance-identifier healink-dev \
  --db-snapshot-identifier healink-dev-$(date +%Y%m%d)

# Restore sau:
aws rds restore-db-instance-from-db-snapshot \
  --db-instance-identifier healink-dev \
  --db-snapshot-identifier healink-dev-20250930
```

### 4. Monitor Free Tier Usage
```bash
# AWS Console → Billing → Free Tier
# Check remaining hours:
- RDS: 750 hours/month
- ALB: 750 hours/month
- Data Transfer: 100 GB/month
```

---

## 🎯 Expected Costs Summary

### Scenario 1: Development Phase (Hybrid)
```
Month 1-12:  $5-10/month
Month 13+:   $15-25/month
```

### Scenario 2: Full Local
```
Month 1-12:  $0/month
Month 13+:   $0/month
```

### Scenario 3: Full AWS (Not Recommended)
```
Month 1-12:  $73/month
Month 13+:   $101/month
```

---

## 🚨 Lưu Ý Quan Trọng

### ⚠️ Free Tier Limits
- **750 giờ/tháng** = 31.25 ngày
- Nếu chạy 24/7 → Vượt limit
- **Giải pháp**: Nuke overnight → ~12 giờ/ngày = 360 giờ/tháng (safe)

### ⚠️ Hidden Costs
- **NAT Gateway**: $32/month (KHÔNG free)
- **Data Transfer**: Vượt 100 GB → $0.09/GB
- **Backups**: Vượt 20 GB → $0.095/GB
- **Logs**: CloudWatch Logs → $0.50/GB

### ⚠️ After 12 Months
- RDS: $0 → $12/month
- ALB: $0 → $16/month
- Plan ahead hoặc chuyển sang architecture khác

---

## 📞 Getting Help

**Cost too high?**
```bash
# 1. Check AWS Cost Explorer
aws ce get-cost-and-usage --time-period Start=2025-09-01,End=2025-09-30

# 2. Check current resources
./scripts/healink-manager.sh status dev

# 3. Destroy if not using
./scripts/healink-manager.sh destroy dev
```

**Need support?**
- AWS Free Tier FAQ: https://aws.amazon.com/free/
- AWS Cost Calculator: https://calculator.aws/
- Healink CI/CD Docs: `.github/workflows/README_FIRST.md`

---

**Last Updated**: September 30, 2025  
**Status**: ✅ Optimized for Free Tier  
**Expected Cost**: $5-10/month (first 12 months)
