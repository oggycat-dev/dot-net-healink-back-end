# Hướng Dẫn Cấu Hình Môi Trường

Dự án sử dụng một file `.env` duy nhất để quản lý tất cả biến môi trường và cấu hình cho các microservice. Điều này giúp việc quản lý và triển khai dễ dàng hơn.

## Tổng Quan

Hệ thống sử dụng các công cụ sau để quản lý cấu hình:

1. **File `.env`**: Chứa tất cả biến môi trường
2. **Script `generate-appsettings.sh`**: Tạo các file `appsettings.json` từ template và biến môi trường
3. **Script `healink-manager.sh`**: Công cụ quản lý toàn diện

## Cấu Hình Ban Đầu

1. Copy `.env.example` thành `.env` và cập nhật các giá trị phù hợp:
   ```bash
   cp .env.example .env
   nano .env
   ```

2. Đảm bảo đã cấp đủ quyền cho các script:
   ```bash
   chmod +x scripts/generate-appsettings.sh
   chmod +x scripts/healink-manager.sh
   ```

## Sử Dụng Healink Manager

Script `healink-manager.sh` giờ đây bao gồm chức năng tạo cấu hình:

```bash
# Tạo cấu hình từ file .env
./scripts/healink-manager.sh config

# Khởi động môi trường local (sẽ tự động tạo cấu hình trước)
./scripts/healink-manager.sh start local

# Dừng môi trường local
./scripts/healink-manager.sh stop local
```

## Danh Sách Biến Môi Trường

### Cấu Hình Database
- `DB_HOST`: Host PostgreSQL (mặc định: postgres)
- `DB_PORT`: Port PostgreSQL (mặc định: 5432)
- `AUTH_DB_NAME`: Tên database cho AuthService
- `USER_DB_NAME`: Tên database cho UserService
- `CONTENT_DB_NAME`: Tên database cho ContentService
- `NOTIFICATION_DB_NAME`: Tên database cho NotificationService
- `DB_USER`: Tên người dùng database
- `DB_PASSWORD`: Mật khẩu database

### Cấu Hình JWT
- `JWT_SECRET_KEY`: Khóa bí mật cho JWT (phải đủ dài)
- `JWT_ISSUER`: Tổ chức phát hành token
- `JWT_AUDIENCE`: Đối tượng sử dụng token
- `JWT_EXPIRES_MINUTES`: Thời gian hiệu lực của token (phút)

### Cấu Hình Redis
- `REDIS_HOST`: Host Redis (mặc định: redis)
- `REDIS_PORT`: Port Redis (mặc định: 6379)
- `REDIS_PASSWORD`: Mật khẩu Redis

### Cấu Hình RabbitMQ
- `RABBITMQ_HOST`: Host RabbitMQ (mặc định: rabbitmq)
- `RABBITMQ_PORT`: Port RabbitMQ (mặc định: 5672)
- `RABBITMQ_USER`: Tên người dùng RabbitMQ
- `RABBITMQ_PASSWORD`: Mật khẩu RabbitMQ
- `RABBITMQ_VHOST`: Virtual host RabbitMQ (mặc định: /)
- `RABBITMQ_EXCHANGE`: Exchange name RabbitMQ

### Các URL Service
- `AUTH_SERVICE_URL`: URL đến AuthService
- `USER_SERVICE_URL`: URL đến UserService
- `CONTENT_SERVICE_URL`: URL đến ContentService
- `NOTIFICATION_SERVICE_URL`: URL đến NotificationService
- `GATEWAY_URL`: URL đến API Gateway

### Cấu Hình Email (SMTP)
- `EMAIL_SMTP_SERVER`: SMTP server
- `EMAIL_PORT`: Port SMTP server
- `EMAIL_USERNAME`: Tên đăng nhập email
- `EMAIL_PASSWORD`: Mật khẩu email
- `EMAIL_FROM`: Địa chỉ email người gửi
- `EMAIL_FROM_NAME`: Tên người gửi

### Cấu Hình AWS S3
- `AWS_ACCESS_KEY`: AWS Access Key
- `AWS_SECRET_KEY`: AWS Secret Key
- `AWS_REGION`: AWS Region
- `AWS_S3_BUCKET`: Tên S3 bucket

## Thêm Biến Môi Trường Mới

Khi cần thêm biến môi trường mới:

1. Thêm vào file `.env` và `.env.example`
2. Cập nhật template trong `src/appsettings.template.json`
3. Cập nhật script `scripts/generate-appsettings.sh` để xử lý biến mới
4. Cập nhật `docker-compose.yml` để sử dụng biến mới

## Cấu Hình Cho Môi Trường Sản Phẩm

Cho môi trường production, hãy tạo một file `.env.prod` riêng với các giá trị an toàn và bảo mật. Không lưu trữ thông tin nhạy cảm trong mã nguồn.

## Khắc Phục Sự Cố

Nếu gặp lỗi khi khởi động các service:

1. Kiểm tra file `.env` đã được cấu hình đúng chưa
2. Chạy lại script tạo cấu hình: `./scripts/healink-manager.sh config`
3. Kiểm tra logs: `docker-compose logs -f [service-name]`
