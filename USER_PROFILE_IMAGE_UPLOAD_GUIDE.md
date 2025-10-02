# User Profile Image Upload - Hướng Dẫn Sử Dụng

## Tổng Quan

UserService đã được tích hợp với S3 File Storage từ SharedLibrary để hỗ trợ upload ảnh đại diện (avatar) và các file liên quan đến user.

## 🎯 Features

- ✅ Upload user avatar (profile image)
- ✅ Delete user avatar
- ✅ Upload application documents (for creator applications)
- ✅ Automatic old avatar cleanup
- ✅ File validation (type, size)
- ✅ S3 storage integration
- ✅ CloudFront CDN support

## 📋 API Endpoints

### 1. Upload User Avatar

**POST** `/api/user/fileupload/avatar`

Upload or update user profile image.

**Authentication**: Required (Bearer Token)

**Request**:
```http
POST http://localhost:5010/api/user/fileupload/avatar
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: multipart/form-data

file: [your-image-file.jpg]
```

**File Requirements**:
- **Formats**: `.jpg`, `.jpeg`, `.png`, `.webp`
- **Max Size**: 5MB
- **Storage**: `users/avatars/{userId}/`

**Response** (200 OK):
```json
{
  "success": true,
  "avatarUrl": "https://healink-content-bucket.s3.ap-southeast-1.amazonaws.com/users/avatars/123e4567-e89b-12d3-a456-426614174000/abc123.jpg",
  "fileName": "profile.jpg",
  "fileSize": 245678,
  "message": "Avatar uploaded successfully"
}
```

**Features**:
- Automatically deletes old avatar when uploading new one
- Updates `UserProfile.AvatarPath` in database
- Public access for easy display

### 2. Delete User Avatar

**DELETE** `/api/user/fileupload/avatar`

Remove user profile image.

**Authentication**: Required (Bearer Token)

**Request**:
```http
DELETE http://localhost:5010/api/user/fileupload/avatar
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Avatar deleted successfully"
}
```

### 3. Upload Application Document

**POST** `/api/user/fileupload/application-document`

Upload supporting documents for creator application.

**Authentication**: Required (Bearer Token)

**Request**:
```http
POST http://localhost:5010/api/user/fileupload/application-document
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: multipart/form-data

file: [your-document.pdf]
```

**File Requirements**:
- **Formats**: `.pdf`, `.doc`, `.docx`, `.txt`
- **Max Size**: 10MB
- **Storage**: `users/applications/{userId}/` (private)

**Response** (200 OK):
```json
{
  "success": true,
  "fileUrl": "https://healink-content-bucket.s3.ap-southeast-1.amazonaws.com/users/applications/123e4567-e89b-12d3-a456-426614174000/document.pdf",
  "fileName": "portfolio.pdf",
  "fileSize": 1245678,
  "contentType": "application/pdf",
  "message": "File uploaded successfully"
}
```

## 🚀 Usage Examples

### Example 1: Upload Avatar with cURL

```bash
# Step 1: Login to get JWT token
curl -X POST "http://localhost:5010/api/user/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "YourPassword123@"
  }'

# Response will contain: { "token": "eyJhbGc..." }

# Step 2: Upload avatar
curl -X POST "http://localhost:5010/api/user/fileupload/avatar" \
  -H "Authorization: Bearer eyJhbGc..." \
  -F "file=@/path/to/your/avatar.jpg"
```

### Example 2: Upload Avatar with Postman

1. **Set Authorization**:
   - Type: Bearer Token
   - Token: Your JWT token

2. **Request Details**:
   - Method: POST
   - URL: `http://localhost:5010/api/user/fileupload/avatar`
   - Body: form-data
     - Key: `file`
     - Type: File
     - Value: Select your image

3. **Send Request**

### Example 3: Upload Avatar with JavaScript/Fetch

```javascript
async function uploadAvatar(file, token) {
  const formData = new FormData();
  formData.append('file', file);

  const response = await fetch('http://localhost:5010/api/user/fileupload/avatar', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    },
    body: formData
  });

  const result = await response.json();
  
  if (result.success) {
    console.log('Avatar uploaded:', result.avatarUrl);
    return result.avatarUrl;
  } else {
    console.error('Upload failed:', result.message);
    throw new Error(result.message);
  }
}

// Usage
const fileInput = document.getElementById('avatar-input');
const file = fileInput.files[0];
const avatarUrl = await uploadAvatar(file, userToken);
```

### Example 4: Upload Avatar with React

```jsx
import { useState } from 'react';

function AvatarUpload() {
  const [uploading, setUploading] = useState(false);
  const [avatarUrl, setAvatarUrl] = useState(null);
  const [error, setError] = useState(null);

  const handleUpload = async (event) => {
    const file = event.target.files[0];
    
    if (!file) return;

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      setError('Invalid file type. Please upload JPG, PNG, or WEBP image.');
      return;
    }

    // Validate file size (5MB)
    if (file.size > 5 * 1024 * 1024) {
      setError('File size exceeds 5MB limit.');
      return;
    }

    setUploading(true);
    setError(null);

    try {
      const formData = new FormData();
      formData.append('file', file);

      const token = localStorage.getItem('jwt_token');
      
      const response = await fetch('http://localhost:5010/api/user/fileupload/avatar', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        },
        body: formData
      });

      const result = await response.json();

      if (result.success) {
        setAvatarUrl(result.avatarUrl);
        console.log('Avatar uploaded successfully:', result.avatarUrl);
      } else {
        setError(result.message || 'Upload failed');
      }
    } catch (err) {
      setError('Network error: ' + err.message);
    } finally {
      setUploading(false);
    }
  };

  const handleDelete = async () => {
    if (!window.confirm('Are you sure you want to delete your avatar?')) {
      return;
    }

    setUploading(true);
    
    try {
      const token = localStorage.getItem('jwt_token');
      
      const response = await fetch('http://localhost:5010/api/user/fileupload/avatar', {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      const result = await response.json();

      if (result.success) {
        setAvatarUrl(null);
        console.log('Avatar deleted successfully');
      } else {
        setError(result.message || 'Delete failed');
      }
    } catch (err) {
      setError('Network error: ' + err.message);
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="avatar-upload">
      <h2>Profile Image</h2>
      
      {avatarUrl && (
        <div className="avatar-preview">
          <img src={avatarUrl} alt="User Avatar" style={{ width: 150, height: 150, borderRadius: '50%', objectFit: 'cover' }} />
          <button onClick={handleDelete} disabled={uploading}>
            Delete Avatar
          </button>
        </div>
      )}
      
      <div className="upload-controls">
        <input 
          type="file" 
          accept=".jpg,.jpeg,.png,.webp" 
          onChange={handleUpload}
          disabled={uploading}
        />
        {uploading && <p>Uploading...</p>}
        {error && <p style={{ color: 'red' }}>{error}</p>}
      </div>
    </div>
  );
}

export default AvatarUpload;
```

## 📁 Database Schema

### UserProfile Entity

```csharp
public class UserProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string? Address { get; set; }
    
    // Avatar field
    public string? AvatarPath { get; set; }  // Stores S3 URL
    
    public DateTime? LastLoginAt { get; set; }
}
```

## 🔧 Configuration

### Required Environment Variables (in `.env`)

```bash
# AWS S3 Configuration
AWS_S3_BUCKET_NAME=healink-content-bucket
AWS_S3_REGION=ap-southeast-1
AWS_S3_ACCESS_KEY=your_aws_access_key
AWS_S3_SECRET_KEY=your_aws_secret_key
AWS_S3_CLOUDFRONT_URL=https://d1234567890.cloudfront.net  # Optional

# Advanced S3 Settings (Optional)
AWS_S3_ENABLE_ENCRYPTION=true
AWS_S3_DEFAULT_ACL=public-read
AWS_S3_MAX_FILE_SIZE_BYTES=104857600  # 100MB
```

### UserService Configuration

In `UserService.API/Configurations/ServiceConfiguration.cs`:

```csharp
// Add S3 File Storage from SharedLibrary
builder.Services.AddS3FileStorage(builder.Configuration);
```

## 🔒 Security & Validation

### File Validation

**Avatar Upload**:
- Allowed formats: JPG, JPEG, PNG, WEBP
- Max size: 5MB
- Automatic old file cleanup

**Application Documents**:
- Allowed formats: PDF, DOC, DOCX, TXT
- Max size: 10MB
- Private storage (not publicly accessible)

### Authorization

All endpoints require JWT authentication via `DistributedAuthorize` attribute.

## 🎨 Frontend Integration

### Display User Avatar

```javascript
// Get user profile with avatar
async function getUserProfile(token) {
  const response = await fetch('http://localhost:5010/api/user/profile/me', {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  const profile = await response.json();
  
  // Display avatar
  if (profile.avatarPath) {
    document.getElementById('avatar-img').src = profile.avatarPath;
  } else {
    // Show default avatar
    document.getElementById('avatar-img').src = '/images/default-avatar.png';
  }
}
```

### Avatar Placeholder

When user has no avatar, you can use:
- Default avatar image
- User initials
- Icon placeholder

```jsx
function Avatar({ avatarUrl, fullName }) {
  if (avatarUrl) {
    return <img src={avatarUrl} alt={fullName} />;
  }
  
  // Generate initials
  const initials = fullName
    .split(' ')
    .map(name => name[0])
    .join('')
    .toUpperCase()
    .substring(0, 2);
  
  return (
    <div className="avatar-placeholder">
      {initials}
    </div>
  );
}
```

## 📊 S3 Storage Structure

```
healink-content-bucket/
├── users/
│   ├── avatars/
│   │   ├── {userId1}/
│   │   │   └── abc123.jpg
│   │   ├── {userId2}/
│   │   │   └── def456.png
│   │   └── ...
│   └── applications/
│       ├── {userId1}/
│       │   ├── portfolio.pdf
│       │   └── resume.docx
│       └── ...
```

## 🐛 Troubleshooting

### Error: "User not authenticated"
- Check if JWT token is valid
- Verify token is included in Authorization header
- Ensure token hasn't expired

### Error: "Invalid file type"
- Check file extension
- Avatars: only JPG, JPEG, PNG, WEBP
- Documents: only PDF, DOC, DOCX, TXT

### Error: "File size exceeds limit"
- Avatars: max 5MB
- Documents: max 10MB
- Compress image before upload

### Error: "Failed to upload file"
- Check AWS credentials in `.env`
- Verify S3 bucket exists and has correct permissions
- Check network connectivity to AWS

### Error: "User profile not found"
- Ensure user profile exists in database
- Check if user completed registration process

## 🔄 Workflow

1. **User Registration** → UserProfile created (no avatar)
2. **Upload Avatar** → File saved to S3, URL stored in `AvatarPath`
3. **Update Avatar** → Old file deleted, new file uploaded
4. **Delete Avatar** → File removed from S3, `AvatarPath` set to null
5. **Display Profile** → Load avatar from `AvatarPath` URL

## ✅ Testing

### Test Avatar Upload

```bash
# 1. Register user
curl -X POST "http://localhost:5010/api/user/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123@",
    "confirm_password": "Test123@",
    "full_name": "Test User",
    "phone_number": "1234567890",
    "otp_sent_channel": 1
  }'

# 2. Verify OTP (you'll receive OTP via email/phone)
curl -X POST "http://localhost:5010/api/user/auth/verify-otp" \
  -H "Content-Type: application/json" \
  -d '{
    "contact": "test@example.com",
    "otp_code": "123456",
    "contact_type": 1
  }'

# 3. Login
curl -X POST "http://localhost:5010/api/user/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123@"
  }'

# 4. Upload avatar
curl -X POST "http://localhost:5010/api/user/fileupload/avatar" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@/path/to/test-avatar.jpg"

# 5. Verify avatar URL in response
# 6. Check if old avatar is deleted when uploading new one
```

## 🎉 Kết Luận

Upload profile image đã được tích hợp hoàn chỉnh vào UserService thông qua SharedLibrary S3 File Storage. Hệ thống cung cấp:

- ✅ Easy-to-use API endpoints
- ✅ Automatic file management
- ✅ Security & validation
- ✅ CDN support via CloudFront
- ✅ Database integration
- ✅ RESTful design
