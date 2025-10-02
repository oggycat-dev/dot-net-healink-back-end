# 🚀 S3 Presigned URL - Quick Start (5 phút)

## ⚡ TL;DR

1. ✅ **Tạo S3 bucket** với "Block all public access" BẬT
2. ✅ **Cập nhật `.env`** với AWS credentials
3. ✅ **Upload file private**: `POST /api/fileupload/application-document`
4. ✅ **Lấy presigned URL**: `POST /api/fileupload/presigned-url`
5. ✅ **Frontend dùng presigned URL** để display file

---

## 📸 Screenshot Cấu Hình S3 Bucket

Theo screenshot bạn gửi, cấu hình đúng là:

```
✅ Block all public access: ON (CHECKED)
  ├── ✅ Block public access to buckets and objects granted through new ACLs
  ├── ✅ Block public access to buckets and objects granted through any ACLs  
  ├── ✅ Block public access to buckets and objects granted through new public bucket policies
  └── ✅ Block public and cross-account access through any public bucket policies
```

**✅ ĐÂY LÀ CẤU HÌNH TỐT NHẤT CHO PRESIGNED URL!**

---

## 🔧 Bước 1: Tạo S3 Bucket (2 phút)

### Option A: AWS Console (Dễ nhất)

1. Vào AWS Console → S3
2. Click "Create bucket"
3. Bucket name: `healink-upload-file`
4. Region: `ap-southeast-2` (Sydney)
5. **✅ BẬT "Block all public access"** (giống screenshot bạn)
6. Click "Create bucket"

### Option B: AWS CLI

```bash
# Create bucket
aws s3 mb s3://healink-upload-file --region ap-southeast-2

# Block public access (quan trọng!)
aws s3api put-public-access-block \
  --bucket healink-upload-file \
  --public-access-block-configuration \
    "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
```

---

## 🔑 Bước 2: Lấy AWS Credentials (1 phút)

1. AWS Console → IAM → Users → Your User
2. Security credentials tab
3. Click "Create access key"
4. Download credentials (chỉ hiện 1 lần!)

---

## ⚙️ Bước 3: Cập nhật `.env` (30 giây)

File `.env` của bạn đã có sẵn cấu trúc, chỉ cần điền AWS credentials:

```bash
# AWS S3 Configuration (update these values)
AWS_S3_BUCKET_NAME=healink-upload-file
AWS_S3_REGION=ap-southeast-2
AWS_S3_ACCESS_KEY=AKIAIOSFODNN7EXAMPLE        # ← Thay bằng access key của bạn
AWS_S3_SECRET_KEY=wJalrXUtn/K7MDENG/bPxRfi... # ← Thay bằng secret key của bạn
```

---

## 🎯 Bước 4: Test API (2 phút)

### 4.1. Start Services

```bash
# Nếu chưa chạy
./scripts/local-dev.sh start

# Đợi services ready
./scripts/local-dev.sh urls
```

### 4.2. Test Upload Private File

```bash
# Upload document (file sẽ PRIVATE trong S3)
curl -X POST http://localhost:5010/api/user/fileupload/application-document \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@test-document.pdf"
```

**Response (lưu fileUrl này):**

```json
{
  "success": true,
  "fileUrl": "https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/users/applications/abc123/test-document.pdf",
  "fileName": "test-document.pdf",
  "fileSize": 12345,
  "message": "File uploaded successfully"
}
```

### 4.3. Test Generate Presigned URL

```bash
# Lấy presigned URL (có thời hạn 60 phút)
curl -X POST http://localhost:5010/api/user/fileupload/presigned-url \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fileUrl": "https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/users/applications/abc123/test-document.pdf",
    "expirationMinutes": 60
  }'
```

**Response (dùng presignedUrl này trong FE):**

```json
{
  "success": true,
  "presignedUrl": "https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/users/applications/abc123/test-document.pdf?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=...",
  "originalUrl": "https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/users/applications/abc123/test-document.pdf",
  "expiresInMinutes": 60,
  "expiresAt": "2025-10-01T15:30:00Z",
  "message": "Presigned URL generated successfully"
}
```

### 4.4. Verify Private File

```bash
# Try access file directly (should FAIL ❌)
curl -I https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/users/applications/abc123/test-document.pdf
# Response: 403 Forbidden ❌

# Access with presigned URL (should SUCCEED ✅)
curl -I "https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/users/applications/abc123/test-document.pdf?X-Amz-Algorithm=..."
# Response: 200 OK ✅
```

---

## 💻 Frontend Integration (Copy-Paste Ready)

### React + TypeScript Example

```typescript
// services/fileService.ts
export async function getPresignedUrl(
  fileUrl: string, 
  expirationMinutes: number = 60
): Promise<string> {
  const token = localStorage.getItem('accessToken');
  
  const response = await fetch('http://localhost:5010/api/user/fileupload/presigned-url', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      fileUrl: fileUrl,
      expirationMinutes: expirationMinutes
    })
  });

  if (!response.ok) {
    throw new Error('Failed to get presigned URL');
  }

  const data = await response.json();
  return data.presignedUrl;
}

// components/FileViewer.tsx
import { useState, useEffect } from 'react';

interface Props {
  fileUrl: string; // URL from database
}

export default function FileViewer({ fileUrl }: Props) {
  const [presignedUrl, setPresignedUrl] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function load() {
      try {
        const url = await getPresignedUrl(fileUrl, 60);
        setPresignedUrl(url);
      } catch (error) {
        console.error('Failed to load file:', error);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [fileUrl]);

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      {/* For Images */}
      <img src={presignedUrl} alt="File" />
      
      {/* For PDF */}
      {/* <iframe src={presignedUrl} width="100%" height="600px" /> */}
      
      {/* For Audio */}
      {/* <audio controls src={presignedUrl} /> */}
    </div>
  );
}
```

---

## 📋 API Reference

### UserService Endpoints

| Method | Endpoint | Description | Public/Private |
|--------|----------|-------------|----------------|
| POST | `/api/fileupload/avatar` | Upload avatar | Public ✅ |
| DELETE | `/api/fileupload/avatar` | Delete avatar | Public ✅ |
| POST | `/api/fileupload/application-document` | Upload document | Private 🔒 |
| POST | `/api/fileupload/presigned-url` | Get presigned URL | - |

### ContentService Endpoints

| Method | Endpoint | Description | Public/Private |
|--------|----------|-------------|----------------|
| POST | `/api/fileupload/podcast` | Upload podcast audio | Private 🔒 |
| POST | `/api/fileupload/thumbnail` | Upload thumbnail | Public ✅ |
| POST | `/api/fileupload/transcript` | Upload transcript | Private 🔒 |
| POST | `/api/fileupload/flashcard/image` | Upload flashcard image | Public ✅ |
| POST | `/api/fileupload/postcard/image` | Upload postcard image | Public ✅ |
| POST | `/api/fileupload/community/image` | Upload community image | Public ✅ |
| POST | `/api/fileupload/presigned-url` | Get presigned URL | - |

---

## 🔐 Khi Nào Dùng Public vs Private?

### ✅ Public Files (makePublic: true)

- User avatars
- Podcast thumbnails
- Community images
- Flashcard images
- Postcard images

**Lý do**: Cần hiển thị nhanh, không cần bảo mật cao

### 🔒 Private Files (makePublic: false)

- Podcast audio files
- Application documents
- User private files
- Premium content
- Paid podcasts

**Lý do**: Cần bảo mật, kiểm soát truy cập, có thời hạn

---

## ⏱️ Thời Gian Expire Nên Dùng

| Use Case | Recommended Time |
|----------|------------------|
| Xem nhanh | 5-15 phút |
| Nghe podcast | 1-4 giờ |
| Tải về | 24 giờ |
| Share link | 7 ngày (max) |

---

## 🐛 Troubleshooting

### ❌ Upload failed: "Access Denied"

```bash
# Check AWS credentials
cat .env | grep AWS_S3

# Test credentials
aws s3 ls s3://healink-upload-file --region ap-southeast-2
```

### ❌ Presigned URL returns 403

**Nguyên nhân**: URL đã hết hạn hoặc file không tồn tại

**Giải pháp**: Generate presigned URL mới

```bash
# Check if file exists
aws s3 ls s3://healink-upload-file/users/applications/abc123/
```

### ❌ CORS error in browser

**Giải pháp**: Add CORS policy to S3 bucket

```json
{
  "CORSRules": [
    {
      "AllowedOrigins": ["http://localhost:3000", "http://localhost:5010"],
      "AllowedMethods": ["GET", "HEAD"],
      "AllowedHeaders": ["*"],
      "MaxAgeSeconds": 3000
    }
  ]
}
```

Áp dụng CORS:

```bash
aws s3api put-bucket-cors \
  --bucket healink-upload-file \
  --cors-configuration file://cors.json
```

---

## 💰 Chi Phí S3 (AWS Free Tier)

### Free Tier (12 tháng đầu)

- ✅ 5GB storage
- ✅ 20,000 GET requests/month
- ✅ 2,000 PUT requests/month
- ✅ 15GB data transfer OUT

### Sau Free Tier

- Storage: $0.023/GB/month (~₫23,000/GB/tháng)
- GET: $0.0004/1000 requests
- PUT: $0.005/1000 requests
- Data Transfer: $0.12/GB (first 1GB free)

**Ví dụ**: 10GB storage + 100,000 GET requests/month = $0.27/month (~₫6,800/tháng)

---

## ✅ Checklist

- [ ] S3 bucket created với "Block all public access"
- [ ] AWS credentials added to `.env`
- [ ] Services restarted với config mới
- [ ] Test upload private file
- [ ] Test generate presigned URL
- [ ] Test access file với presigned URL
- [ ] Frontend integrated

---

## 📚 Tài Liệu Đầy Đủ

- **Full Guide**: `S3_PRESIGNED_URL_GUIDE.md` (chi tiết hơn)
- **AWS S3 Docs**: https://docs.aws.amazon.com/s3/
- **Presigned URLs**: https://docs.aws.amazon.com/AmazonS3/latest/userguide/PresignedUrlUploadObject.html

---

**🎉 Xong! Bây giờ bạn có thể upload private files và chia sẻ an toàn với frontend!**

**Need help?** Đọc `S3_PRESIGNED_URL_GUIDE.md` để biết thêm chi tiết.

