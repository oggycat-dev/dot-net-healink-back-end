# ⚡ AWS Free Tier Quick Start - Healink

## 🎯 Chi Phí Tối Ưu: $5-10/tháng

### 🔥 Critical Fix: RDS Instance Type

#### ❌ Đang dùng (SAI):
```
db.t4g.micro → KHÔNG Free Tier → $10/tháng ngay cả với Free Tier
```

#### ✅ Phải dùng (ĐÚNG):
```
db.t3.micro → Free Tier eligible → $0/tháng (12 tháng đầu)
```

**Đã fix trong**: `terraform_healink/dev.tfvars`

---

## 🚀 3 Strategies - Chọn 1

### 🏆 Strategy 1: Hybrid (RECOMMENDED) - $5-10/month
```bash
# Local development (FREE)
./scripts/local-dev.sh start

# Deploy to AWS only for testing (few hours/week)
GitHub Actions → Full Deploy → Test 2-3h → Nuke AWS

# Cost: ~$5-10/month (only Fargate for few hours)
```

### 💚 Strategy 2: Full Local - $0/month
```bash
# Everything local
./scripts/local-dev.sh start

# Never deploy to AWS
# Cost: $0/month
```

### 💰 Strategy 3: Smart Schedule - $9/month
```bash
# Deploy 8h/day (work hours only)
# Auto nuke at night and weekends
# Cost: ~$9/month (within Free Tier)
```

---

## ⚡ Quick Deploy with Free Tier

### Option A: Use Fixed dev.tfvars
```bash
# Already fixed: db.t4g.micro → db.t3.micro
./scripts/healink-manager.sh create dev
```

### Option B: Use free-tier.tfvars (Maximum Optimization)
```bash
cd terraform_healink
terraform init
terraform workspace select dev || terraform workspace new dev
terraform apply -var-file=free-tier.tfvars
```

### Option C: GitHub Actions
```bash
# Go to: GitHub → Actions → Full Deploy
# Select: environment = dev
# Run workflow
# (Will use fixed dev.tfvars with db.t3.micro)
```

---

## 💰 Cost Comparison (First 12 Months)

| Strategy | Monthly Cost | What You Pay For |
|----------|--------------|------------------|
| **Hybrid** | $5-10 | ECS Fargate (few hours) |
| **Full Local** | $0 | Nothing (all local) |
| **Smart Schedule** | $9 | ECS Fargate (8h/day) |
| **Full AWS** | $73 | Redis + RabbitMQ + Fargate |

---

## 📊 What's Free vs What's Not

### ✅ FREE (with AWS Free Tier - 12 months)
- ✅ RDS db.t3.micro (750 hours/month)
- ✅ ALB (750 hours/month)
- ✅ S3 (5 GB)
- ✅ Data Transfer (100 GB outbound)

### ❌ NOT FREE (must pay)
- ❌ ElastiCache/Redis (~$12/month)
- ❌ Amazon MQ/RabbitMQ (~$24/month)
- ❌ ECS Fargate (~$37/month for 5 services 24/7)
- ❌ NAT Gateway (~$32/month)

---

## 🎯 Recommended: Use Local Redis & RabbitMQ

**Save $36/month** by using local services:

```bash
# Local development includes:
- PostgreSQL (connects to AWS RDS)
- Redis (local - FREE)
- RabbitMQ (local - FREE)
- All microservices (local - FREE)

# Only RDS in cloud (FREE with Free Tier)
# Cost: $0-5/month
```

---

## ✅ Action Items

1. ✅ **DONE**: Fixed `dev.tfvars` (t4g → t3)
2. ✅ **DONE**: Created `free-tier.tfvars`
3. ✅ **DONE**: Created optimization guides
4. 📝 **TODO**: Choose your strategy
5. 📝 **TODO**: Deploy with new config
6. 📝 **TODO**: Set AWS Budget Alert ($10/month)
7. 📝 **TODO**: Monitor costs daily

---

## 📞 Need Help?

- **Full Guide**: [AWS_FREE_TIER_GUIDE.md](AWS_FREE_TIER_GUIDE.md)
- **CI/CD Docs**: [.github/workflows/README_FIRST.md](.github/workflows/README_FIRST.md)
- **Local Dev**: [scripts/README.md](scripts/README.md)

---

**TL;DR**: 
- Use `db.t3.micro` (FREE), not `db.t4g.micro` ($10/month)
- Use local Redis + RabbitMQ (save $36/month)
- Deploy to AWS only for testing (few hours)
- Expected cost: **$5-10/month**
