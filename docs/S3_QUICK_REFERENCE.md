# S3 URL Transformation - Quick Reference

## 🚀 TL;DR (Too Long; Didn't Read)

**What:** Middleware tự động chuyển S3 URLs → Presigned URLs trong API responses  
**Why:** Bảo mật (private S3 bucket) + Đơn giản cho FE  
**When:** Chỉ áp dụng cho GET requests trả về JSON  
**Where:** ContentService & UserService  

---

## ✅ What's Working Now

```
Frontend calls: GET /api/content/user/podcasts

Response BEFORE middleware:
{
  "audioUrl": "https://bucket.s3.aws.com/audio.mp3" ❌ (403 Forbidden)
}

Response AFTER middleware:
{
  "audioUrl": "https://bucket.s3.aws.com/audio.mp3?X-Amz-Algorithm=..." ✅ (Works!)
}
```

---

## 🎯 Affected Fields

| Field Name | Where | Example |
|------------|-------|---------|
| `thumbnailUrl` | Podcasts | Podcast preview images |
| `audioUrl` | Podcasts | Podcast audio files |
| `imageUrl` | Flashcards, Postcards | Card images |
| `avatarUrl` | User profiles | Profile pictures |
| `documentUrls` | Creator apps | Application documents |

---

## 📝 For Backend Developers

### To Add Transformation to New Service:

```csharp
// In ServiceConfiguration.cs
public static WebApplication ConfigurePipeline(this WebApplication app)
{
    app.UseAuthentication();
    app.UseAuthorization();
    
    // ✨ Add this line
    app.UseS3UrlTransformation();
    
    app.MapControllers();
    return app;
}
```

### To Add New S3 Field:

**Option 1:** Use standard naming (auto-detected)
```csharp
public class MyDto
{
    public string ImageUrl { get; set; }  // ✅ Auto-detected
    public string VideoUrl { get; set; }  // ✅ Auto-detected
    public string FileUrl { get; set; }   // ✅ Auto-detected
}
```

**Option 2:** Update middleware patterns
```csharp
// In S3UrlTransformationMiddleware.cs
private static readonly HashSet<string> UrlPropertyNames = new()
{
    "thumbnailUrl", "audioUrl", "imageUrl",
    "myCustomUrl" // ✨ Add your field name here
};
```

---

## 📱 For Frontend Developers

### ✅ DO:
```typescript
// URLs are ready to use
const podcast = await fetch('/api/content/user/podcasts/123').then(r => r.json());

// Use directly in HTML
<audio src={podcast.audioUrl} controls />
<img src={podcast.thumbnailUrl} alt="" />

// Handle expiration (after 1 hour)
<audio 
  src={podcast.audioUrl} 
  onError={() => refetchPodcast(podcast.id)}
/>
```

### ❌ DON'T:
```typescript
// ❌ Don't manually call presigned URL endpoints
const presignedUrl = await fetch('/api/fileupload/presigned-url', {
  method: 'POST',
  body: JSON.stringify({ fileUrl: podcast.audioUrl })
});

// ❌ Don't store presigned URLs in localStorage
localStorage.setItem('audioUrl', podcast.audioUrl); // Will expire!

// ❌ Don't try to extract original S3 URL
const originalUrl = podcast.audioUrl.split('?')[0]; // Wrong!
```

---

## 🔧 Debugging

### Check if Transformation is Working:

```bash
# 1. Call API
curl http://localhost:5002/api/user/podcasts | jq

# 2. Look for query parameters in URLs
{
  "audioUrl": "https://bucket.s3.region.amazonaws.com/audio.mp3?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=..."
}

# ✅ If you see "?X-Amz-" → Working!
# ❌ If plain URL without "?" → Not working
```

### Check Logs:

```bash
# Look for transformation activity
docker-compose logs -f contentservice-api | grep "S3"

# Expected:
[Debug] Generated presigned URL for key: podcasts/audio.mp3
[Debug] S3 URLs transformed in response for /api/user/podcasts
```

### Common Issues:

| Issue | Cause | Fix |
|-------|-------|-----|
| URLs not transformed | Middleware not registered | Add `app.UseS3UrlTransformation()` |
| 403 on presigned URL | IAM permissions | Check S3 bucket policy |
| Slow responses | Too many URLs | Implement caching/pagination |

---

## 🎓 Key Concepts

### Presigned URL Expiration
- **Default:** 1 hour
- **What happens:** URL returns 403 after expiration
- **Solution:** Frontend re-fetches data to get fresh URLs

### Security Model
```
S3 Bucket (Private) 
    ↓
Backend generates presigned URL (1h expiration)
    ↓
Frontend uses URL to access file
    ↓
CloudWatch logs all access
```

### Performance
- **Latency added:** 50-100ms per request
- **Why:** Parallel presigned URL generation
- **Acceptable:** Yes, for better security

---

## 📚 Full Documentation

- **Implementation Guide:** `/docs/S3_URL_TRANSFORMATION_GUIDE.md`
- **Endpoints Inventory:** `/docs/S3_ENDPOINTS_INVENTORY.md`
- **Summary:** `/docs/S3_URL_TRANSFORMATION_SUMMARY.md`

---

## 🆘 Need Help?

- **Slack:** #backend-team
- **Code:** `src/SharedLibrary/Commons/Middlewares/S3UrlTransformationMiddleware.cs`
- **Contact:** @backend-lead

---

**Version:** 1.0.0  
**Last Updated:** October 14, 2025
