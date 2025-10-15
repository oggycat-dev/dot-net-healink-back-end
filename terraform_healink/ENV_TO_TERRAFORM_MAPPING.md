# 🔄 Environment Variables → Terraform → ECS Mapping

Tài liệu này giải thích cách các biến từ file `.env` của bạn được chuyển sang AWS ECS.

---

## 🔐 CẢNH BÁO BẢO MẬT QUAN TRỌNG

**⚠️ AWS Credentials đã bị EXPOSE trong file env.txt!**

```
AWS_S3_ACCESS_KEY=AKIA******************  # ← ĐÃ BỊ LEAK!
AWS_S3_SECRET_KEY=***********************************  # ← ĐÃ BỊ LEAK!
```

**HÀNH ĐỘNG NGAY:**
1. Vào [AWS IAM Console](https://console.aws.amazon.com/iam/home#/security_credentials)
2. Deactivate/Delete old Access Key
3. Tạo Access Key mới
4. Add vào GitHub Secrets (KHÔNG commit!)

---

## 📊 Secrets Priority List

### 🔴 Critical (PHẢI có ngay)

| GitHub Secret Name | Value từ .env | Dùng cho service nào |
|-------------------|---------------|----------------------|
| `DB_PASSWORD` | `admin@123` | Tất cả services |
| `JWT_SECRET` | `HealinkMicroserviceSecretKeyIsLongEnoughToBeUsedWithJWT` | Auth, API Gateway |
| `RABBITMQ_PASSWORD` | `admin@123` | Tất cả services |
| `REDIS_PASSWORD` | `admin@123` | Tất cả services |

### 🟡 Important (Nên có)

| GitHub Secret Name | Value từ .env | Dùng cho |
|-------------------|---------------|----------|
| `AWS_S3_ACCESS_KEY` | **[ROTATE MỚI]** | Content Service |
| `AWS_S3_SECRET_KEY` | **[ROTATE MỚI]** | Content Service |
| `SMTP_PASSWORD` | `ezrn myqu nphw qqnz` | Notification Service |
| `MOMO_SECRET_KEY` | `ozTlfYxWaTPr3WrrlSvBvvKNvyc5fqCz` | Payment Service |
| `PASSWORD_ENCRYPTION_KEY` | `K9ltF1d2jlYvLsaN6AdmiaPHY8qwqUIW` | Auth Service |

### 🟢 Optional (Có thể hardcode hoặc để sau)

| GitHub Secret Name | Value từ .env | Note |
|-------------------|---------------|------|
| `ADMIN_PASSWORD` | `admin@123` | Admin account |
| `MOMO_ACCESS_KEY` | `Tyz9FMyviI6mOYEn` | Payment |
| `SMTP_USERNAME` | `nguyenhoainamvt99@gmail.com` | Email |

---

## 🎯 Luồng xử lý Secrets

### Cách hiện tại (LOCAL - Docker Compose)

```
.env file → docker-compose.yml → Container Environment Variables
```

### Cách mới (AWS ECS - Production)

```
GitHub Secrets 
  ↓
GitHub Actions Workflow
  ↓
Terraform Variables
  ↓
ECS Task Definition (Environment Variables)
  ↓
Container Runtime
```

---

## 📝 Các bước setup chi tiết

### Bước 1: Add Secrets vào GitHub

```bash
# Run script để xem danh sách
./scripts/convert-env-to-github-secrets.sh
```

Sau đó manually add từng secret vào:
👉 https://github.com/oggycat-dev/dot-net-healink-back-end/settings/secrets/actions

### Bước 2: Secrets sẽ được map như thế nào?

#### Database Connection String

**Local (.env)**:
```bash
DB_PASSWORD=admin@123
AUTH_DB_CONNECTION_STRING=Host=postgres;Database=authservicedb;Username=admin;Password=admin@123
```

**AWS ECS**:
```terraform
# Terraform tự động tạo connection string
environment_variables = [
  {
    name = "ConnectionStrings__DefaultConnection"
    value = "Host=${rds_endpoint};Database=${db_name};Username=${db_username};Password=${var.db_password};"
  }
]
```

**GitHub Workflow**:
```yaml
env:
  TF_VAR_db_password: ${{ secrets.DB_PASSWORD }}
```

#### JWT Configuration

**Local (.env)**:
```bash
JWT_SECRET_KEY=HealinkMicroserviceSecretKeyIsLongEnoughToBeUsedWithJWT
JWT_ISSUER=Healink
JWT_AUDIENCE=Healink.Users
```

**AWS ECS** (cần update Terraform):
```terraform
environment_variables = [
  {
    name  = "JWT__Secret"
    value = var.jwt_secret
  },
  {
    name  = "JWT__Issuer"
    value = var.jwt_issuer
  }
]
```

#### RabbitMQ Connection

**Local (.env)**:
```bash
RABBITMQ_HOST=rabbitmq
RABBITMQ_PASSWORD=admin@123
```

**AWS ECS**:
```terraform
{
  name  = "RabbitMQ__Host"
  value = data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint
},
{
  name  = "RabbitMQ__Password"
  value = var.rabbitmq_password
}
```

#### AWS S3 Credentials

**Local (.env)**:
```bash
AWS_S3_ACCESS_KEY=AKIA...
AWS_S3_SECRET_KEY=DN42...
AWS_S3_BUCKET_NAME=healink-upload-file
```

**AWS ECS**:
```terraform
{
  name  = "AWS__S3__AccessKey"
  value = var.aws_s3_access_key
},
{
  name  = "AWS__S3__SecretKey"  
  value = var.aws_s3_secret_key
},
{
  name  = "AWS__S3__BucketName"
  value = var.aws_s3_bucket_name
}
```

#### Email SMTP

**Local (.env)**:
```bash
EMAIL_SENDER_EMAIL=nguyenhoainamvt99@gmail.com
EMAIL_SENDER_PASSWORD=ezrn myqu nphw qqnz
```

**AWS ECS**:
```terraform
{
  name  = "Email__SenderEmail"
  value = var.smtp_username
},
{
  name  = "Email__SenderPassword"
  value = var.smtp_password
}
```

---

## 🔄 Migration Plan

### Phase 1: Critical Secrets (Làm ngay)

1. ✅ Add DB_PASSWORD to GitHub Secrets
2. ✅ Add JWT_SECRET to GitHub Secrets  
3. ✅ Add RABBITMQ_PASSWORD to GitHub Secrets
4. ✅ Add REDIS_PASSWORD to GitHub Secrets
5. ⚠️  Rotate & add AWS_S3_ACCESS_KEY
6. ⚠️  Rotate & add AWS_S3_SECRET_KEY

### Phase 2: Update Terraform (Tôi sẽ code)

Update `terraform_healink/app-infra/main.tf` để:
- Nhận secrets từ variables
- Inject vào ECS task definitions
- Map đúng tên environment variables

### Phase 3: Update Workflow (Tôi sẽ code)

Update `.github/workflows/full-deploy.yml` để:
- Pass GitHub Secrets vào Terraform
- Sử dụng `TF_VAR_` prefix

### Phase 4: Test & Verify

- Deploy lên AWS
- Verify containers nhận được env vars
- Test kết nối DB, Redis, RabbitMQ
- Test S3 upload
- Test email sending

---

## 🆘 Troubleshooting

### Lỗi: Container không kết nối được DB

**Nguyên nhân**: Password trong ECS task definition không khớp với RDS password

**Fix**:
1. Check GitHub Secret: `DB_PASSWORD`  
2. Check Terraform variable đã pass đúng chưa
3. Check RDS password trong AWS Console

### Lỗi: JWT token không valid

**Nguyên nhân**: JWT_SECRET không khớp giữa các services

**Fix**:
1. Đảm bảo TẤT CẢ services dùng CÙNG MỘT `JWT_SECRET`
2. Check environment variable name: `JWT__Secret` (double underscore)

### Lỗi: S3 upload failed - Access Denied

**Nguyên nhân**: AWS credentials sai hoặc không có quyền

**Fix**:
1. Verify AWS credentials đã rotate
2. Check IAM permissions cho S3 bucket
3. Verify bucket name đúng

---

## 📚 Best Practices

### ✅ DO

- ✅ Rotate AWS credentials ngay lập tức
- ✅ Dùng GitHub Secrets cho TẤT CẢ sensitive data
- ✅ Dùng different passwords cho từng environment (dev/prod)
- ✅ Enable MFA cho AWS account
- ✅ Regularly rotate secrets (3-6 tháng)

### ❌ DON'T

- ❌ KHÔNG BAO GIỜ commit secrets vào Git
- ❌ KHÔNG share secrets qua chat/email
- ❌ KHÔNG dùng weak passwords
- ❌ KHÔNG reuse passwords giữa services
- ❌ KHÔNG hardcode secrets trong code

---

## 🔐 Security Checklist

- [ ] AWS S3 credentials đã được rotate
- [ ] Tất cả secrets đã add vào GitHub Secrets
- [ ] File `.env` local không bị commit vào Git
- [ ] `.gitignore` đã ignore `.env`
- [ ] IAM user chỉ có permissions cần thiết (least privilege)
- [ ] Enable CloudTrail để audit AWS API calls
- [ ] Setup alerts cho suspicious S3 activities

---

## 📞 Next Steps

Sau khi bạn add secrets vào GitHub, tôi sẽ:

1. Update Terraform để nhận secrets từ variables
2. Update workflow để pass secrets vào Terraform
3. Test deployment với secrets mới
4. Document lại process

**Sẵn sàng tiếp tục chưa?** 🚀


