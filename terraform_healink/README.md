# 🚀 Healink Terraform - Siêu đơn giản!

> **1 script duy nhất** - Quản lý toàn bộ AWS infrastructure

## ⚡ Quick Start (30 giây)

```bash
# Deploy tất cả (lần đầu)
./deploy.sh quick-deploy

# Check status
./deploy.sh status

# Test app layer (an toàn)
./deploy.sh quick-test

# Xem help
./deploy.sh help
```

## 🎯 Tại sao đơn giản?

### ✅ Trước đây (phức tạp)
- 10+ scripts trong `/scripts/`
- Nhiều file config `.tfvars`
- Manual workspace management
- Risk destroy nhầm database

### ✅ Bây giờ (siêu đơn giản)
- **1 script**: `deploy.sh`
- **2 layers**: `stateful-infra` (DB) + `app-infra` (ECS)
- **Auto workspace**: dev/prod tự động
- **Safe operations**: Không thể destroy nhầm DB

## 🏗️ Cấu trúc siêu sạch

```
terraform_healink/
├── deploy.sh           # 🎯 1 script duy nhất
├── stateful-infra/     # 💾 Database, Redis, ECR (lâu dài)
├── app-infra/          # 🚀 ECS, ALB (có thể destroy)
└── modules/            # 📦 Reusable components
```

## 📋 Commands thường dùng

### 🏃‍♂️ Hàng ngày
```bash
./deploy.sh quick-deploy     # Deploy lần đầu
./deploy.sh app-apply        # Update app (sau khi build image)
./deploy.sh quick-test       # Test: destroy + recreate app
./deploy.sh status           # Xem gì đang chạy
```

### 🛠️ Troubleshooting
```bash
./deploy.sh clean           # Clean cache khi bị lỗi
./deploy.sh app-destroy     # Destroy app (an toàn)
./deploy.sh stateful-plan   # Check stateful sẽ thay đổi gì
```

### ⚠️ Advanced (cẩn thận)
```bash
./deploy.sh stateful-destroy  # Xóa database (NGUY HIỂM!)
```

## � Cost Control

```bash
# Tắt app khi không dùng (chỉ còn ~$3/month)
./deploy.sh app-destroy

# Bật lại khi cần
./deploy.sh app-apply
```

## 🔄 Workflow Development

### Thêm service mới
```bash
# 1. Thêm ECR repo vào stateful-infra/main.tf
./deploy.sh stateful-apply

# 2. Thêm service module vào app-infra/main.tf
./deploy.sh app-apply
```

### Deploy code changes
```bash
# 1. Build & push Docker images (dùng CI/CD hoặc manual)
# 2. Update app layer
./deploy.sh app-apply
```

### Testing/Debugging
```bash
# Destroy + recreate app (giữ nguyên DB)
./deploy.sh quick-test

# Check logs
./deploy.sh status
```

## 🌍 Environments

```bash
# Development (default)
./deploy.sh quick-deploy dev

# Production
./deploy.sh quick-deploy prod
```

## 🆘 Troubleshooting

### Script báo lỗi?
```bash
./deploy.sh clean
./deploy.sh stateful-init
./deploy.sh app-init
```

### Services không start?
```bash
./deploy.sh status
# Check terraform outputs để debug
```

### Chi phí AWS cao?
```bash
./deploy.sh app-destroy  # Tắt app, chỉ giữ DB
```

### Cần reset hoàn toàn?
```bash
./deploy.sh app-destroy
./deploy.sh stateful-destroy  # ⚠️ Sẽ mất data!
./deploy.sh clean
```

## 🎉 Kết quả

- ✅ **1 command** thay vì 10+ scripts
- ✅ **An toàn**: Không thể destroy nhầm DB
- ✅ **Cost-effective**: Tắt app khi không dùng
- ✅ **Professional**: Production-ready, enterprise-grade
- ✅ **Scalable**: Dễ thêm services mới

---

**Tóm lại**: Chỉ cần nhớ `./deploy.sh quick-deploy` và `./deploy.sh quick-test` là đủ! 🚀