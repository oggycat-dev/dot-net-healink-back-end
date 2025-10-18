# ⚡ Quick Start: Setup GitHub Secrets trong 5 phút

## 🎯 Mục tiêu
Add 6 secrets QUAN TRỌNG NHẤT vào GitHub để workflow có thể deploy được.

---

## 🔑 6 Secrets bắt buộc

### 1. Database Password
```
Name: DB_PASSWORD
Value: admin@123
```

### 2. JWT Secret
```
Name: JWT_SECRET
Value: HealinkMicroserviceSecretKeyIsLongEnoughToBeUsedWithJWT
```

### 3. RabbitMQ Password
```
Name: RABBITMQ_PASSWORD
Value: admin@123
```

### 4. Redis Password
```
Name: REDIS_PASSWORD
Value: admin@123
```

### 5. AWS S3 Access Key
```
Name: AWS_S3_ACCESS_KEY
Value: [ROTATE MỚI - KHÔNG DÙNG KEY CŨ!]
```

⚠️ **QUAN TRỌNG**: Key cũ `AKIA4OG4NBUIBJ4MVLM6` đã bị LEAK!
- Vào: https://console.aws.amazon.com/iam/home#/security_credentials
- Deactivate key cũ
- Create new Access Key
- Copy Access Key ID vào đây

### 6. AWS S3 Secret Key
```
Name: AWS_S3_SECRET_KEY
Value: [SECRET của key mới ở bước 5]
```

---

## 📝 Cách add vào GitHub

### Option 1: Web UI (Khuyến nghị cho lần đầu)

1. **Mở GitHub Secrets page**:
   ```
   https://github.com/oggycat-dev/dot-net-healink-back-end/settings/secrets/actions
   ```

2. **Click nút "New repository secret"**

3. **Add từng secret**:
   - Name: `DB_PASSWORD`
   - Secret: `admin@123`
   - Click "Add secret"

4. **Lặp lại cho 6 secrets**

### Option 2: GitHub CLI (Nhanh hơn nếu đã cài gh)

```bash
# Install GitHub CLI (nếu chưa có)
# macOS
brew install gh

# Login
gh auth login

# Add secrets
gh secret set DB_PASSWORD -b"admin@123"
gh secret set JWT_SECRET -b"HealinkMicroserviceSecretKeyIsLongEnoughToBeUsedWithJWT"
gh secret set RABBITMQ_PASSWORD -b"admin@123"
gh secret set REDIS_PASSWORD -b"admin@123"
gh secret set AWS_S3_ACCESS_KEY -b"YOUR_NEW_ACCESS_KEY"
gh secret set AWS_S3_SECRET_KEY -b"YOUR_NEW_SECRET_KEY"
```

---

## ✅ Verify Secrets

Sau khi add xong, check lại:

```bash
gh secret list
```

Hoặc vào Web UI, bạn sẽ thấy:

```
DB_PASSWORD                 Updated 1 minute ago
JWT_SECRET                  Updated 1 minute ago
RABBITMQ_PASSWORD          Updated 1 minute ago
REDIS_PASSWORD             Updated 1 minute ago
AWS_S3_ACCESS_KEY          Updated 1 minute ago
AWS_S3_SECRET_KEY          Updated 1 minute ago
```

---

## 🚀 Sau khi add xong secrets

### Bước 1: Commit & Push code mới (có fix ECR names)

Code đã có fix ECR repository names, chỉ cần trigger lại workflow:

```bash
git commit --allow-empty -m "chore: trigger deployment with secrets"
git push origin main
```

### Bước 2: Hoặc Re-run workflow hiện tại

Vào GitHub Actions → Chọn workflow run gần nhất → "Re-run all jobs"

---

## ⏭️ Secrets bổ sung (optional - có thể add sau)

Những secrets này không bắt buộc ngay, có thể add sau khi deployment thành công:

### Email SMTP
```bash
gh secret set SMTP_USERNAME -b"nguyenhoainamvt99@gmail.com"
gh secret set SMTP_PASSWORD -b"ezrn myqu nphw qqnz"
```

### MoMo Payment
```bash
gh secret set MOMO_ACCESS_KEY -b"Tyz9FMyviI6mOYEn"
gh secret set MOMO_SECRET_KEY -b"ozTlfYxWaTPr3WrrlSvBvvKNvyc5fqCz"
```

### Admin Account
```bash
gh secret set ADMIN_PASSWORD -b"admin@123"
```

### Password Encryption
```bash
gh secret set PASSWORD_ENCRYPTION_KEY -b"K9ltF1d2jlYvLsaN6AdmiaPHY8qwqUIW"
```

---

## 🔒 Security Notes

### ✅ DO
- ✅ Rotate AWS credentials NGAY sau khi leak
- ✅ Dùng strong passwords cho production
- ✅ Enable GitHub 2FA
- ✅ Review GitHub Actions logs (secrets sẽ bị mask)

### ❌ DON'T  
- ❌ Share secrets qua chat/email
- ❌ Commit secrets vào Git
- ❌ Dùng lại passwords leaked
- ❌ Screenshot secrets

---

## 🆘 Troubleshooting

### Lỗi: "Secret not found"

Kiểm tra:
1. Secret name đúng chính xác chưa (case-sensitive)
2. Đã add vào đúng repository chưa
3. Có quyền admin repo không

### Lỗi: "Invalid secret value"

- Secrets không được chứa newline ở cuối
- Copy trực tiếp value, không copy cả whitespace

### Workflow vẫn fail

Nếu workflow vẫn fail sau khi add secrets:
1. Check workflow logs để xem secret nào bị thiếu
2. Đảm bảo Terraform đã được update để nhận secrets (tôi sẽ làm bước này sau)

---

## 📊 Current Status

- ✅ ECR repository names đã được fix (`healink-free/...`)
- ✅ Scripts tạo secrets đã sẵn sàng
- ✅ Documentation đã đầy đủ
- ⏳ **BẠN CẦN LÀM**: Add 6 secrets vào GitHub
- ⏳ **TÔI SẼ LÀM**: Update Terraform để inject secrets vào ECS

---

**Sẵn sàng add secrets chưa?** Sau khi add xong, báo tôi để tiếp tục update Terraform! 🚀


