# 🔒 S3 Presigned URL - Complete Guide

## 📋 Overview

Presigned URLs allow you to grant temporary access to private S3 files **without making them public**. This is the most secure way to share files with your frontend.

---

## ✅ 1. S3 Bucket Configuration

### ⚠️ IMPORTANT: Block Public Access

**✅ ENABLE "Block all public access"** in your S3 bucket settings.

This is the **SAFEST** configuration for presigned URLs:

```
✅ Block all public access: ON
  ├── ✅ Block public access to buckets and objects granted through new ACLs
  ├── ✅ Block public access to buckets and objects granted through any ACLs
  ├── ✅ Block public access to buckets and objects granted through new public bucket or access point policies
  └── ✅ Block public and cross-account access to buckets and objects through any public bucket or access point policies
```

### Why Block Public Access?

- ✅ **Security**: Files are private by default
- ✅ **Control**: Only authorized users with presigned URLs can access files
- ✅ **Temporary**: URLs expire after a specified time
- ✅ **Auditable**: All access is logged via presigned URL generation

---

## 🔧 2. AWS Configuration in `.env`

Your `.env` file should already have these settings:

```bash
# AWS S3 Configuration
AWS_S3_BUCKET_NAME=healink-upload-file
AWS_S3_REGION=ap-southeast-2
AWS_S3_ACCESS_KEY=your-access-key
AWS_S3_SECRET_KEY=your-secret-key
```

### Create S3 Bucket

```bash
# Via AWS CLI
aws s3 mb s3://healink-upload-file --region ap-southeast-2

# Enable versioning (recommended)
aws s3api put-bucket-versioning \
  --bucket healink-upload-file \
  --versioning-configuration Status=Enabled

# Block public access
aws s3api put-public-access-block \
  --bucket healink-upload-file \
  --public-access-block-configuration \
    "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
```

Or use AWS Console:
1. Go to S3 Console
2. Click "Create bucket"
3. Name: `healink-upload-file`
4. Region: `ap-southeast-2` (Sydney)
5. **✅ Enable "Block all public access"**
6. Create bucket

---

## 🚀 3. API Endpoints

### 3.1. Upload File (Private)

**UserService - Upload Application Document (Private)**

```http
POST /api/fileupload/application-document
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [binary]
```

**ContentService - Upload Podcast (Private)**

```http
POST /api/fileupload/podcast
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [binary]
```

### 3.2. Generate Presigned URL

**UserService**

```http
POST /api/fileupload/presigned-url
Authorization: Bearer {token}
Content-Type: application/json

{
  "fileUrl": "https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/users/applications/abc123/document.pdf",
  "expirationMinutes": 60
}
```

**Response:**

```json
{
  "success": true,
  "presignedUrl": "https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/users/applications/abc123/document.pdf?X-Amz-Algorithm=...",
  "originalUrl": "https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/users/applications/abc123/document.pdf",
  "expiresInMinutes": 60,
  "expiresAt": "2025-10-01T15:30:00Z",
  "message": "Presigned URL generated successfully"
}
```

**ContentService**

```http
POST /api/fileupload/presigned-url
Authorization: Bearer {token}
Content-Type: application/json

{
  "fileUrl": "https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/podcasts/audio123.mp3",
  "expirationMinutes": 120
}
```

---

## 💻 4. Frontend Integration

### 4.1. Upload Private File

```typescript
async function uploadPrivateDocument(file: File): Promise<string> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await fetch('http://localhost:5010/api/user/fileupload/application-document', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${accessToken}`
    },
    body: formData
  });

  const data = await response.json();
  return data.fileUrl; // Store this URL in your database
}
```

### 4.2. Get Presigned URL (To Display File)

```typescript
async function getPresignedUrl(fileUrl: string, expirationMinutes: number = 60): Promise<string> {
  const response = await fetch('http://localhost:5010/api/user/fileupload/presigned-url', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${accessToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      fileUrl: fileUrl,
      expirationMinutes: expirationMinutes
    })
  });

  const data = await response.json();
  return data.presignedUrl; // Use this URL to display/download the file
}
```

### 4.3. Display File in Frontend

```typescript
// Example: Display PDF document
async function displayDocument(storedFileUrl: string) {
  // Get presigned URL (valid for 1 hour)
  const presignedUrl = await getPresignedUrl(storedFileUrl, 60);
  
  // Use presigned URL to display file
  // For PDF:
  window.open(presignedUrl, '_blank');
  
  // For Image:
  document.getElementById('img').src = presignedUrl;
  
  // For Audio:
  document.getElementById('audio').src = presignedUrl;
}
```

### 4.4. Complete React Example

```typescript
import React, { useState, useEffect } from 'react';

interface FileViewerProps {
  fileUrl: string; // Stored S3 URL from database
}

const FileViewer: React.FC<FileViewerProps> = ({ fileUrl }) => {
  const [presignedUrl, setPresignedUrl] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');

  useEffect(() => {
    async function loadFile() {
      try {
        setLoading(true);
        
        // Generate presigned URL
        const response = await fetch('http://localhost:5010/api/user/fileupload/presigned-url', {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            fileUrl: fileUrl,
            expirationMinutes: 60
          })
        });

        if (!response.ok) {
          throw new Error('Failed to get presigned URL');
        }

        const data = await response.json();
        setPresignedUrl(data.presignedUrl);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    loadFile();
  }, [fileUrl]);

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      {/* For Images */}
      <img src={presignedUrl} alt="File" />
      
      {/* For Audio */}
      {/* <audio controls src={presignedUrl} /> */}
      
      {/* For PDF */}
      {/* <iframe src={presignedUrl} width="100%" height="600px" /> */}
    </div>
  );
};

export default FileViewer;
```

---

## 🔐 5. Security Best Practices

### 5.1. File Upload Strategy

| File Type | makePublic | Access Method |
|-----------|-----------|---------------|
| User Avatar | `true` | Direct URL |
| Podcast Thumbnail | `true` | Direct URL |
| Podcast Audio | `false` | Presigned URL |
| Application Documents | `false` | Presigned URL |
| Private User Files | `false` | Presigned URL |

### 5.2. Expiration Times

| Use Case | Recommended Time |
|----------|------------------|
| Quick View | 5-15 minutes |
| Normal Access | 1 hour |
| Long Session | 4-6 hours |
| Download Links | 24 hours |
| Share Links | 7 days (max) |

### 5.3. Code Example: Upload Strategy

```csharp
// Public file (accessible to everyone)
var avatarUrl = await _fileStorageService.UploadFileAsync(
    file, 
    "users/avatars", 
    makePublic: true  // ✅ Can access directly via URL
);

// Private file (requires presigned URL)
var documentUrl = await _fileStorageService.UploadFileAsync(
    file, 
    "users/documents", 
    makePublic: false  // 🔒 Requires presigned URL to access
);
```

---

## 📊 6. Testing

### 6.1. Test Upload Private File

```bash
# Upload document (private)
curl -X POST http://localhost:5010/api/user/fileupload/application-document \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@document.pdf"

# Response:
{
  "success": true,
  "fileUrl": "https://healink-upload-file.s3.ap-southeast-1.amazonaws.com/users/applications/abc123/document.pdf",
  "fileName": "document.pdf",
  "fileSize": 12345,
  "contentType": "application/pdf",
  "message": "File uploaded successfully"
}
```

### 6.2. Test Generate Presigned URL

```bash
# Generate presigned URL
curl -X POST http://localhost:5010/api/user/fileupload/presigned-url \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fileUrl": "https://healink-upload-file.s3.ap-southeast-1.amazonaws.com/users/applications/abc123/document.pdf",
    "expirationMinutes": 60
  }'

# Response:
{
  "success": true,
  "presignedUrl": "https://healink-upload-file.s3.ap-southeast-1.amazonaws.com/users/applications/abc123/document.pdf?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=...",
  "originalUrl": "https://healink-upload-file.s3.ap-southeast-1.amazonaws.com/users/applications/abc123/document.pdf",
  "expiresInMinutes": 60,
  "expiresAt": "2025-10-01T15:30:00Z",
  "message": "Presigned URL generated successfully"
}
```

### 6.3. Test Access File

```bash
# Try to access private file directly (should FAIL)
curl -I https://healink-upload-file.s3.ap-southeast-1.amazonaws.com/users/applications/abc123/document.pdf

# Response: 403 Forbidden ❌

# Access with presigned URL (should SUCCEED)
curl -I "https://healink-upload-file.s3.ap-southeast-1.amazonaws.com/users/applications/abc123/document.pdf?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=..."

# Response: 200 OK ✅
```

---

## 🎯 7. Flow Diagram

### Private File Flow

```
┌─────────────┐
│   Backend   │
│   Upload    │
│makePublic:  │
│   false     │
└──────┬──────┘
       │
       ↓
┌─────────────┐
│  S3 Bucket  │
│  (Private)  │
│   🔒 BLOCK  │
│   PUBLIC    │
└──────┬──────┘
       │
       ↓ Store URL in DB
┌─────────────┐
│  Database   │
│  Save URL   │
└──────┬──────┘
       │
       ↓ Frontend requests access
┌─────────────┐
│  Frontend   │
│  POST /     │
│ presigned-  │
│    url      │
└──────┬──────┘
       │
       ↓
┌─────────────┐
│   Backend   │
│  Generate   │
│  Presigned  │
│     URL     │
└──────┬──────┘
       │
       ↓ Return temporary URL
┌─────────────┐
│  Frontend   │
│   Display   │
│    File     │
│  (60 min)   │
└─────────────┘
```

### Public File Flow (for comparison)

```
┌─────────────┐
│   Backend   │
│   Upload    │
│makePublic:  │
│    true     │
└──────┬──────┘
       │
       ↓
┌─────────────┐
│  S3 Bucket  │
│  (Public)   │
│   🌐 PUBLIC │
│    READ     │
└──────┬──────┘
       │
       ↓ Store URL in DB
┌─────────────┐
│  Database   │
│  Save URL   │
└──────┬──────┘
       │
       ↓ Use URL directly
┌─────────────┐
│  Frontend   │
│   Display   │
│    File     │
│  (Forever)  │
└─────────────┘
```

---

## ⚙️ 8. Environment Configuration

Update your `.env`:

```bash
# AWS S3 Configuration
AWS_S3_BUCKET_NAME=healink-upload-file
AWS_S3_REGION=ap-southeast-2
AWS_S3_ACCESS_KEY=AKIAIOSFODNN7EXAMPLE
AWS_S3_SECRET_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY

# Optional: CloudFront URL (for faster delivery)
# AWS_CLOUDFRONT_URL=https://d123456789.cloudfront.net
```

---

## 🐛 9. Troubleshooting

### Issue 1: "Access Denied" when uploading

**Solution**: Check AWS credentials in `.env`

```bash
# Test AWS credentials
aws s3 ls s3://healink-upload-file --region ap-southeast-2
```

### Issue 2: "File not found" when generating presigned URL

**Solution**: Ensure file exists in S3

```bash
# Check if file exists
aws s3 ls s3://healink-upload-file/users/applications/abc123/
```

### Issue 3: Presigned URL returns 403

**Possible causes**:
- URL has expired
- File was deleted
- AWS credentials changed
- Bucket policy changed

**Solution**: Generate a new presigned URL

### Issue 4: Can't access file even with presigned URL

**Solution**: Check S3 bucket CORS configuration

```bash
# Add CORS policy to bucket
aws s3api put-bucket-cors \
  --bucket healink-upload-file \
  --cors-configuration file://cors.json
```

`cors.json`:
```json
{
  "CORSRules": [
    {
      "AllowedOrigins": ["http://localhost:3000", "https://yourdomain.com"],
      "AllowedMethods": ["GET", "HEAD"],
      "AllowedHeaders": ["*"],
      "MaxAgeSeconds": 3000
    }
  ]
}
```

---

## 📝 10. Summary

### ✅ What You Have Now

1. **S3 Bucket**: Fully private with "Block all public access" enabled
2. **Upload APIs**: 
   - UserService: `/api/fileupload/avatar` (public), `/api/fileupload/application-document` (private)
   - ContentService: `/api/fileupload/podcast` (private), `/api/fileupload/thumbnail` (public)
3. **Presigned URL APIs**:
   - UserService: `POST /api/fileupload/presigned-url`
   - ContentService: `POST /api/fileupload/presigned-url`
4. **Security**: Files are private by default, only accessible via temporary presigned URLs

### 🎯 Next Steps

1. ✅ Create S3 bucket with "Block all public access"
2. ✅ Update `.env` with AWS credentials
3. ✅ Test file upload
4. ✅ Test presigned URL generation
5. ✅ Integrate with frontend

### 💰 Cost Optimization

- S3 Standard: $0.023/GB/month
- Data Transfer OUT: $0.12/GB (first 1GB free)
- API Requests: $0.0004/1000 GET requests
- **Tip**: Use CloudFront CDN to reduce S3 data transfer costs

---

## 📞 Need Help?

- AWS S3 Documentation: https://docs.aws.amazon.com/s3/
- AWS Presigned URLs: https://docs.aws.amazon.com/AmazonS3/latest/userguide/PresignedUrlUploadObject.html
- .NET AWS SDK: https://docs.aws.amazon.com/sdk-for-net/

---

**🎉 You're ready to use presigned URLs for secure file access!**

