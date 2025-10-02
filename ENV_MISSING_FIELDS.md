# Các Trường Còn Thiếu Trong File .env

## ⚠️ QUAN TRỌNG - Cần Bổ Sung Ngay

### 1. AWS S3 Configuration (Cho Upload File API)

Thêm các dòng sau vào phần `# === CONTENT SERVICE SETTINGS ===`:

```bash
# AWS S3 Advanced Configuration
AWS_S3_CLOUDFRONT_URL=                           # CloudFront CDN URL (để tăng tốc độ load file)
AWS_S3_ENABLE_ENCRYPTION=true                    # Bật mã hóa file trên S3
AWS_S3_DEFAULT_ACL=public-read                   # Quyền truy cập mặc định (public-read hoặc private)
AWS_S3_MAX_FILE_SIZE_BYTES=104857600            # 100MB (tính bằng bytes)
AWS_S3_ALLOWED_EXTENSIONS=.jpg,.jpeg,.png,.webp,.mp3,.wav,.m4a,.aac,.pdf,.txt,.docx,.doc
AWS_S3_FORCE_PATH_STYLE=false                   # Sử dụng virtual hosted-style URLs
AWS_S3_USE_HTTP=false                           # Sử dụng HTTPS (nên để false = dùng HTTPS)
AWS_S3_PRESIGNED_URL_EXPIRATION_MINUTES=60     # Thời gian hết hạn của presigned URL
```

### 2. NotificationService Database

Thêm vào phần `# === DATABASE SETTINGS ===`:

```bash
NOTIFICATION_DB_NAME=notificationservicedb
NOTIFICATION_DB_CONNECTION_STRING=Host=postgres;Database=notificationservicedb;Username=admin;Password=admin@123
```

### 3. Service URLs (Thiếu)

Bổ sung vào phần `# === SERVICE URLS (Gateway) ===`:

```bash
NOTIFICATION_SERVICE_URL=http://notificationservice-api
GATEWAY_URL=http://gateway-api
```

### 4. Health Check Settings (Thiếu hoàn toàn)

Thêm section mới:

```bash
# === HEALTH CHECK SETTINGS ===
HEALTH_CHECK_INTERVAL=30s
HEALTH_CHECK_TIMEOUT=10s
HEALTH_CHECK_RETRIES=3
HEALTH_CHECK_START_PERIOD=40s
```

### 5. Docker Settings (Thiếu hoàn toàn)

Thêm section mới:

```bash
# === DOCKER SETTINGS ===
COMPOSE_PROJECT_NAME=healink-microservices
DOCKER_RESTART_POLICY=unless-stopped
DOCKER_NETWORK_NAME=healink-network
```

## 📋 Optional - Có thể thêm sau

### 6. API Rate Limiting (Tùy chọn)

```bash
# === API RATE LIMITING ===
RATE_LIMIT_ENABLED=false
RATE_LIMIT_REQUESTS_PER_MINUTE=60
RATE_LIMIT_REQUESTS_PER_HOUR=1000
```

### 7. Content Moderation (Tùy chọn)

```bash
# === CONTENT MODERATION ===
CONTENT_MODERATION_ENABLED=false
CONTENT_AUTO_APPROVE_ENABLED=false
CONTENT_REQUIRE_MANUAL_REVIEW=true
```

## 🔧 Cách Cập Nhật

### Option 1: Copy-Paste Thủ Công

Mở file `.env` và thêm các dòng trên vào cuối file.

### Option 2: Sử dụng Script

Tạo file `update-env.sh`:

```bash
#!/bin/bash

# Backup existing .env
cp .env .env.backup

# Append missing configurations
cat >> .env << 'EOF'

# === AWS S3 ADVANCED CONFIGURATION ===
AWS_S3_CLOUDFRONT_URL=
AWS_S3_ENABLE_ENCRYPTION=true
AWS_S3_DEFAULT_ACL=public-read
AWS_S3_MAX_FILE_SIZE_BYTES=104857600
AWS_S3_ALLOWED_EXTENSIONS=.jpg,.jpeg,.png,.webp,.mp3,.wav,.m4a,.aac,.pdf,.txt,.docx,.doc
AWS_S3_FORCE_PATH_STYLE=false
AWS_S3_USE_HTTP=false
AWS_S3_PRESIGNED_URL_EXPIRATION_MINUTES=60

# === NOTIFICATION SERVICE DATABASE ===
NOTIFICATION_DB_NAME=notificationservicedb
NOTIFICATION_DB_CONNECTION_STRING=Host=postgres;Database=notificationservicedb;Username=admin;Password=admin@123

# === ADDITIONAL SERVICE URLS ===
NOTIFICATION_SERVICE_URL=http://notificationservice-api
GATEWAY_URL=http://gateway-api

# === HEALTH CHECK SETTINGS ===
HEALTH_CHECK_INTERVAL=30s
HEALTH_CHECK_TIMEOUT=10s
HEALTH_CHECK_RETRIES=3
HEALTH_CHECK_START_PERIOD=40s

# === DOCKER SETTINGS ===
COMPOSE_PROJECT_NAME=healink-microservices
DOCKER_RESTART_POLICY=unless-stopped
DOCKER_NETWORK_NAME=healink-network

EOF

echo "✅ .env file updated! Backup saved as .env.backup"
```

Chạy script:
```bash
chmod +x update-env.sh
./update-env.sh
```

## ⚠️ Lưu Ý Quan Trọng

1. **AWS Credentials**: Thay đổi `your_aws_access_key` và `your_aws_secret_key` bằng credentials thật của bạn
2. **S3 Region**: Nếu bucket của bạn ở Singapore, đổi `us-east-1` thành `ap-southeast-1`
3. **CloudFront**: Nếu bạn có CloudFront distribution, điền URL vào `AWS_S3_CLOUDFRONT_URL`
4. **Notification Database**: Cần phải thêm `NOTIFICATION_DB_NAME` vào `POSTGRES_MULTIPLE_DATABASES` trong `docker-compose.yml`

## 📊 So Sánh Trước/Sau

### Trước (Thiếu):
```bash
AWS_S3_BUCKET_NAME=healink-content-bucket
AWS_S3_REGION=us-east-1
AWS_S3_ACCESS_KEY=your_aws_access_key
AWS_S3_SECRET_KEY=your_aws_secret_key
```

### Sau (Đầy Đủ):
```bash
# Basic S3 Config
AWS_S3_BUCKET_NAME=healink-content-bucket
AWS_S3_REGION=ap-southeast-1
AWS_S3_ACCESS_KEY=AKIAIOSFODNN7EXAMPLE
AWS_S3_SECRET_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY

# Advanced S3 Config
AWS_S3_CLOUDFRONT_URL=https://d1234567890.cloudfront.net
AWS_S3_ENABLE_ENCRYPTION=true
AWS_S3_DEFAULT_ACL=public-read
AWS_S3_MAX_FILE_SIZE_BYTES=104857600
AWS_S3_ALLOWED_EXTENSIONS=.jpg,.jpeg,.png,.webp,.mp3,.wav,.m4a,.aac,.pdf,.txt,.docx,.doc
AWS_S3_FORCE_PATH_STYLE=false
AWS_S3_USE_HTTP=false
AWS_S3_PRESIGNED_URL_EXPIRATION_MINUTES=60
```

## 🚀 Sau Khi Cập Nhật

1. Restart tất cả services:
```bash
docker-compose down
docker-compose up -d --build
```

2. Verify configuration:
```bash
curl http://localhost:5000/api/content/health
```

3. Test upload file:
```bash
curl -X POST "http://localhost:5000/api/fileupload/upload?folderPath=test" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@/path/to/test.jpg"
```
