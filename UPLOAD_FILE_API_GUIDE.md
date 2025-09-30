# Upload File API với S3 - Hướng Dẫn Sử Dụng

## Tổng Quan

Upload File API đã được tạo trong **SharedLibrary** và có thể được sử dụng bởi tất cả các microservice trong hệ thống. API này hỗ trợ upload file lên AWS S3 với các tính năng:

- Upload single/multiple files
- Delete files
- Get file metadata
- Generate presigned URLs
- Check file existence
- Copy files

## Cấu Trúc Thư Mục

```
SharedLibrary/
├── Commons/
│   ├── Interfaces/
│   │   └── IFileStorageService.cs          # Interface chung cho file storage
│   ├── FileStorage/
│   │   └── S3FileStorageService.cs         # Implementation cho AWS S3
│   ├── Controllers/
│   │   └── FileUploadController.cs         # Base controller có thể kế thừa
│   ├── Configurations/
│   │   └── AwsS3Config.cs                  # Configuration class
│   └── DependencyInjection/
│       └── FileStorageServiceCollectionExtensions.cs  # DI extensions
```

## Cấu Hình

### 1. File .env

Thêm các biến môi trường sau vào file `.env`:

```bash
# AWS S3 Configuration
AWS_S3_BUCKET_NAME=healink-content-bucket
AWS_S3_REGION=ap-southeast-1
AWS_S3_ACCESS_KEY=your_aws_access_key_here
AWS_S3_SECRET_KEY=your_aws_secret_key_here
AWS_S3_CLOUDFRONT_URL=https://your-cloudfront-url.net  # Optional
AWS_S3_MAX_FILE_SIZE=104857600  # 100MB in bytes
AWS_S3_ALLOWED_EXTENSIONS=.jpg,.jpeg,.png,.webp,.mp3,.wav,.pdf,.txt,.docx
```

### 2. appsettings.json

```json
{
  "AwsS3Config": {
    "AccessKey": "${AWS_S3_ACCESS_KEY}",
    "SecretKey": "${AWS_S3_SECRET_KEY}",
    "Region": "${AWS_S3_REGION}",
    "BucketName": "${AWS_S3_BUCKET_NAME}",
    "CloudFrontUrl": "${AWS_S3_CLOUDFRONT_URL}",
    "EnableEncryption": true,
    "DefaultAcl": "public-read",
    "MaxFileSizeBytes": 104857600,
    "AllowedExtensions": ".jpg,.jpeg,.png,.webp,.mp3,.wav,.pdf,.txt,.docx"
  }
}
```

## Cách Sử Dụng

### Option 1: Kế Thừa FileUploadController (Khuyến Nghị)

Tạo controller trong service của bạn kế thừa từ `FileUploadController`:

```csharp
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Commons.Controllers;
using SharedLibrary.Commons.Interfaces;

namespace YourService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileUploadController : SharedLibrary.Commons.Controllers.FileUploadController
{
    public FileUploadController(
        IFileStorageService fileStorageService,
        ILogger<FileUploadController> logger) 
        : base(fileStorageService, logger)
    {
    }

    // Bạn có thể override các method hoặc thêm custom endpoints
    [HttpPost("custom")]
    public async Task<ActionResult<FileUploadResponse>> UploadCustomFile(IFormFile file)
    {
        // Custom validation
        var allowedExtensions = new[] { ".jpg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Invalid file type" });
        }

        return await UploadFile(file, "custom-folder", makePublic: true);
    }
}
```

### Option 2: Inject IFileStorageService Trực Tiếp

Sử dụng `IFileStorageService` trong business logic:

```csharp
public class YourService
{
    private readonly IFileStorageService _fileStorageService;

    public YourService(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    public async Task<string> ProcessFile(IFormFile file)
    {
        // Upload file
        var fileUrl = await _fileStorageService.UploadFileAsync(file, "your-folder");
        
        // Delete old file if needed
        if (!string.IsNullOrEmpty(oldFileUrl))
        {
            await _fileStorageService.DeleteFileAsync(oldFileUrl);
        }
        
        return fileUrl;
    }
}
```

### Dependency Injection Setup

Trong `ServiceConfiguration.cs` hoặc `Program.cs`:

```csharp
// Add S3 File Storage from SharedLibrary
builder.Services.AddS3FileStorage(builder.Configuration);

// Hoặc với explicit credentials
builder.Services.AddS3FileStorage(
    accessKey: "your-access-key",
    secretKey: "your-secret-key",
    region: "ap-southeast-1",
    bucketName: "your-bucket-name",
    cloudFrontUrl: "https://your-cloudfront-url.net" // Optional
);
```

## API Endpoints

### 1. Upload Single File

**POST** `/api/fileupload/upload?folderPath=podcasts&makePublic=true`

Request:
```
Content-Type: multipart/form-data
file: [your-file]
```

Response:
```json
{
  "success": true,
  "fileUrl": "https://your-bucket.s3.ap-southeast-1.amazonaws.com/podcasts/abc-123.mp3",
  "fileName": "audio.mp3",
  "fileSize": 5242880,
  "contentType": "audio/mpeg",
  "message": "File uploaded successfully"
}
```

### 2. Upload Multiple Files

**POST** `/api/fileupload/upload-multiple?folderPath=images`

Request:
```
Content-Type: multipart/form-data
files: [file1, file2, file3]
```

Response:
```json
{
  "success": true,
  "fileUrls": [
    "https://your-bucket.s3.ap-southeast-1.amazonaws.com/images/file1.jpg",
    "https://your-bucket.s3.ap-southeast-1.amazonaws.com/images/file2.jpg"
  ],
  "totalFiles": 3,
  "successfulUploads": 2,
  "failedUploads": 1,
  "message": "Uploaded 2 out of 3 files successfully"
}
```

### 3. Delete File

**DELETE** `/api/fileupload/delete?fileUrl=https://your-bucket.s3.amazonaws.com/path/file.jpg`

Response:
```json
{
  "success": true,
  "message": "File deleted successfully"
}
```

### 4. Get File Metadata

**GET** `/api/fileupload/metadata?fileUrl=https://your-bucket.s3.amazonaws.com/path/file.jpg`

Response:
```json
{
  "fileName": "original-file.jpg",
  "contentLength": 1024000,
  "contentType": "image/jpeg",
  "lastModified": "2025-09-27T10:30:00Z",
  "metadata": {
    "x-amz-meta-original-filename": "original-file.jpg",
    "x-amz-meta-upload-timestamp": "2025-09-27T10:30:00Z"
  }
}
```

### 5. Get Presigned URL

**GET** `/api/fileupload/presigned-url?fileKey=podcasts/file.mp3&expirationMinutes=60`

Response:
```json
{
  "url": "https://your-bucket.s3.amazonaws.com/podcasts/file.mp3?AWSAccessKeyId=...&Expires=...&Signature=...",
  "expiresAt": "2025-09-27T11:30:00Z"
}
```

### 6. Check File Exists

**GET** `/api/fileupload/exists?fileUrl=https://your-bucket.s3.amazonaws.com/path/file.jpg`

Response:
```json
{
  "exists": true,
  "fileUrl": "https://your-bucket.s3.amazonaws.com/path/file.jpg"
}
```

## ContentService Custom Endpoints

ContentService đã được cấu hình với các endpoint tùy chỉnh:

### Upload Podcast Audio
**POST** `/api/fileupload/podcast`
- Chỉ chấp nhận: .mp3, .wav, .m4a, .aac
- Folder: `podcasts/audio`
- Requires: Admin or ContentCreator role

### Upload Podcast Thumbnail
**POST** `/api/fileupload/podcast/thumbnail`
- Chỉ chấp nhận: .jpg, .jpeg, .png, .webp
- Folder: `podcasts/thumbnails`

### Upload Podcast Transcript
**POST** `/api/fileupload/podcast/transcript`
- Chỉ chấp nhận: .txt, .pdf, .docx, .doc
- Folder: `podcasts/transcripts`

### Upload Flashcard Image
**POST** `/api/fileupload/flashcard`
- Chỉ chấp nhận: .jpg, .jpeg, .png, .webp
- Folder: `flashcards`

### Upload Postcard Image
**POST** `/api/fileupload/postcard`
- Chỉ chấp nhận: .jpg, .jpeg, .png, .webp
- Folder: `postcards`

### Upload Community Story Image
**POST** `/api/fileupload/community/image`
- Chỉ chấp nhận: .jpg, .jpeg, .png, .webp
- Folder: `community`
- Requires: Any authenticated user

## Sử Dụng Trong Các Service Khác

### UserService - Upload Avatar

```csharp
[HttpPost("avatar")]
[DistributedAuthorize]
public async Task<ActionResult<FileUploadResponse>> UploadAvatar(IFormFile file)
{
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    
    if (!allowedExtensions.Contains(extension))
    {
        return BadRequest(new { message = "Invalid image format" });
    }

    return await UploadFile(file, "users/avatars", makePublic: true);
}
```

### NotificationService - Upload Email Attachments

```csharp
public class EmailService
{
    private readonly IFileStorageService _fileStorageService;

    public async Task<string> UploadAttachment(IFormFile attachment)
    {
        return await _fileStorageService.UploadFileAsync(
            attachment, 
            "email-attachments",
            makePublic: false // Private for security
        );
    }
}
```

## Best Practices

1. **Folder Structure**: Sử dụng cấu trúc thư mục rõ ràng
   - `podcasts/audio`, `podcasts/thumbnails`, `podcasts/transcripts`
   - `users/avatars`, `users/documents`
   - `community/images`

2. **Security**:
   - Validate file extensions
   - Set appropriate ACL (public-read for public content, private for sensitive data)
   - Use presigned URLs for temporary access to private files

3. **Performance**:
   - Sử dụng CloudFront CDN cho static assets
   - Compress images before upload khi có thể
   - Implement retry logic for failed uploads

4. **Cleanup**:
   - Xóa old files khi replace
   - Implement lifecycle policies trong S3 bucket

## Testing với cURL

### Upload File
```bash
curl -X POST "http://localhost:5000/api/fileupload/upload?folderPath=test" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@/path/to/your/file.jpg"
```

### Delete File
```bash
curl -X DELETE "http://localhost:5000/api/fileupload/delete?fileUrl=https://bucket.s3.amazonaws.com/test/file.jpg" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Troubleshooting

### Error: AWS Access Key is required
- Kiểm tra biến môi trường `AWS_S3_ACCESS_KEY` trong file `.env`
- Verify configuration section `AwsS3Config` trong `appsettings.json`

### Error: Failed to upload file
- Kiểm tra AWS credentials có đúng không
- Verify S3 bucket permissions (IAM policy)
- Check network connectivity to AWS

### Error: File size exceeds maximum
- Tăng `MaxFileSizeBytes` trong configuration
- Hoặc giảm kích thước file trước khi upload

## Migration từ ContentService

Nếu bạn đã sử dụng file upload cũ trong ContentService:

1. Xóa hoặc comment out `ContentService.Infrastructure.FileStorage.S3FileStorageService`
2. Xóa hoặc comment out `ContentService.Domain.Interfaces.IFileStorageService`
3. Update DI registration trong `ContentInfrastructureDependencyInjection.cs`
4. Update controller để kế thừa từ SharedLibrary controller

```csharp
// Old - Delete this
services.AddScoped<ContentService.Domain.Interfaces.IFileStorageService, 
                   ContentService.Infrastructure.FileStorage.S3FileStorageService>();

// New - Add this in ServiceConfiguration.cs
builder.Services.AddS3FileStorage(builder.Configuration);
```

## Kết Luận

Upload File API trong SharedLibrary cung cấp một giải pháp thống nhất và có thể tái sử dụng cho tất cả các microservice. Điều này giúp:

- Giảm code duplication
- Dễ dàng bảo trì và update
- Consistent behavior across services
- Centralized configuration management
