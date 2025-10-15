# 🔐 Setup GitHub Secrets cho CI/CD

## 📋 Tổng quan

Workflow cần các **secrets** để deploy lên AWS. Secrets này sẽ được inject vào ECS containers như environment variables.

---

## 🔑 Danh sách Secrets cần thiết

### 1️⃣ Database Secrets

| Secret Name | Mô tả | Example Value |
|-------------|-------|---------------|
| `DB_USERNAME` | PostgreSQL username | `healink_admin` |
| `DB_PASSWORD` | PostgreSQL password | `MySecurePassword123!` |

### 2️⃣ JWT & Authentication

| Secret Name | Mô tả | Example Value |
|-------------|-------|---------------|
| `JWT_SECRET` | JWT signing key | `your-super-secret-jwt-key-min-32-chars` |
| `JWT_ISSUER` | JWT issuer | `https://healink.com` |
| `JWT_AUDIENCE` | JWT audience | `https://healink.com` |

### 3️⃣ RabbitMQ (Amazon MQ)

| Secret Name | Mô tả | Example Value |
|-------------|-------|---------------|
| `RABBITMQ_USERNAME` | RabbitMQ username | `healink_mq` |
| `RABBITMQ_PASSWORD` | RabbitMQ password | `MyMQPassword123!` |

### 4️⃣ Email Service (Optional)

| Secret Name | Mô tả | Example Value |
|-------------|-------|---------------|
| `SMTP_HOST` | SMTP server | `smtp.gmail.com` |
| `SMTP_PORT` | SMTP port | `587` |
| `SMTP_USERNAME` | Email username | `noreply@healink.com` |
| `SMTP_PASSWORD` | Email password | `email-app-password` |

### 5️⃣ AWS S3 (Optional - nếu dùng S3 cho file storage)

| Secret Name | Mô tả | Example Value |
|-------------|-------|---------------|
| `AWS_S3_BUCKET` | S3 bucket name | `healink-media-storage` |
| `AWS_S3_REGION` | S3 region | `ap-southeast-2` |

---

## 📝 Cách thêm Secrets vào GitHub

### Bước 1: Mở GitHub Repository Settings

1. Vào repository: https://github.com/oggycat-dev/dot-net-healink-back-end
2. Click **Settings** tab
3. Sidebar bên trái → **Secrets and variables** → **Actions**

### Bước 2: Add Repository Secrets

1. Click **"New repository secret"**
2. Điền:
   - **Name**: (tên secret từ bảng trên, VD: `DB_PASSWORD`)
   - **Secret**: (giá trị thực tế, VD: `MySecurePassword123!`)
3. Click **"Add secret"**
4. Lặp lại cho tất cả secrets trong danh sách

---

## 🎲 Script tạo random secure passwords

Chạy script này để tạo secure random passwords:

```bash
# Generate DB Password
echo "DB_PASSWORD=$(openssl rand -base64 32)"

# Generate JWT Secret (minimum 32 characters)
echo "JWT_SECRET=$(openssl rand -base64 48)"

# Generate RabbitMQ Password
echo "RABBITMQ_PASSWORD=$(openssl rand -base64 32)"

# Generate SMTP Password (nếu cần)
echo "SMTP_PASSWORD=$(openssl rand -base64 32)"
```

**Lưu ý**: Copy các passwords này và add vào GitHub Secrets!

---

## ✅ Verify Secrets đã được thêm

Sau khi add xong, bạn sẽ thấy list secrets như này:

```
Repository secrets (8)
✓ DB_USERNAME
✓ DB_PASSWORD
✓ JWT_SECRET
✓ JWT_ISSUER
✓ JWT_AUDIENCE
✓ RABBITMQ_USERNAME
✓ RABBITMQ_PASSWORD
✓ SMTP_HOST (optional)
```

**Lưu ý**: GitHub chỉ hiển thị **tên** secrets, không hiển thị **giá trị** (vì bảo mật).

---

## 🔄 Workflow sẽ sử dụng Secrets như thế nào?

### 1. GitHub Actions workflow đọc secrets:

```yaml
env:
  TF_VAR_db_password: ${{ secrets.DB_PASSWORD }}
  TF_VAR_jwt_secret: ${{ secrets.JWT_SECRET }}
  # ...
```

### 2. Terraform nhận từ environment variables:

```hcl
variable "db_password" {
  type      = string
  sensitive = true
}
```

### 3. Terraform inject vào ECS containers:

```hcl
environment_variables = [
  {
    name  = "ConnectionStrings__DefaultConnection"
    value = "Host=...;Password=${var.db_password};"
  }
]
```

### 4. .NET app đọc từ environment variables:

```csharp
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
```

---

## 🆘 Troubleshooting

### ❓ Secret không work?

1. **Check tên secret**: Phải khớp chính xác (case-sensitive)
2. **Re-run workflow**: Sau khi add secret, cần re-run workflow
3. **Check logs**: GitHub Actions logs sẽ hiển thị `***` cho secret values

### ❓ Làm sao update secret value?

1. Vào **Settings** → **Secrets and variables** → **Actions**
2. Click secret muốn update
3. Click **"Update secret"**
4. Nhập value mới
5. Click **"Update secret"**

### ❓ Secret bị leak lên logs?

- GitHub tự động **mask** secret values trong logs
- Nếu thấy full value → Secret name sai hoặc không được declare đúng

---

## 📚 Best Practices

### ✅ DO

- ✅ Dùng passwords mạnh (min 16 characters, random)
- ✅ Rotate secrets định kỳ (3-6 tháng)
- ✅ Dùng different passwords cho từng service
- ✅ Document lại secrets ở đâu (password manager)

### ❌ DON'T

- ❌ Commit secrets vào Git
- ❌ Share secrets qua email/chat
- ❌ Dùng weak passwords (`password123`, `admin`, etc.)
- ❌ Reuse passwords giữa dev và prod

---

## 🔄 Rotate Secrets (Thay đổi passwords)

Khi cần rotate secrets:

1. **Generate new secret**:
   ```bash
   openssl rand -base64 32
   ```

2. **Update GitHub Secret**:
   - Settings → Actions secrets → Update secret

3. **Update Terraform state**:
   ```bash
   # Trigger workflow để update ECS với secrets mới
   git commit --allow-empty -m "chore: rotate secrets" && git push
   ```

4. **Update RDS/RabbitMQ passwords** (nếu cần):
   - Vào AWS Console → RDS/MQ → Modify → Change password

---

## 📞 Support

Nếu gặp vấn đề với secrets setup:

1. Check workflow logs: `https://github.com/oggycat-dev/dot-net-healink-back-end/actions`
2. Verify secrets đã add: Settings → Secrets and variables → Actions
3. Re-run workflow sau khi add secrets

---

**⚠️ LƯU Ý AN TOÀN**:
- KHÔNG bao giờ commit secrets vào Git
- KHÔNG share secrets qua chat/email
- LUÔN dùng GitHub Secrets hoặc AWS Secrets Manager
- Rotate secrets định kỳ


