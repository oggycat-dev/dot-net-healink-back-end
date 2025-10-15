# Complete List of S3-Related Endpoints

## 🎯 Overview
Document này liệt kê **TẤT CẢ** endpoints có chứa S3 URLs và sẽ được tự động transform thành presigned URLs.

---

## 📦 ContentService

### Podcasts Endpoints

#### 1. User Podcasts (Public Access)
```
GET /api/content/user/podcasts
```
**Response DTO:** `PodcastResponseDto[]`
**S3 Fields:**
- `thumbnailUrl` (nullable)
- `audioUrl` (required)

**Example Response:**
```json
{
  "data": [
    {
      "id": "abc-123",
      "title": "Meditation Guide",
      "thumbnailUrl": "https://bucket.s3.region.amazonaws.com/thumbnails/abc-123.jpg?X-Amz-...",
      "audioUrl": "https://bucket.s3.region.amazonaws.com/podcasts/abc-123.mp3?X-Amz-...",
      "duration": "00:15:30"
    }
  ]
}
```

---

```
GET /api/content/user/podcasts/{id}
```
**Response DTO:** `PodcastResponseDto`
**S3 Fields:**
- `thumbnailUrl` (nullable)
- `audioUrl` (required)

---

```
GET /api/content/user/podcasts/latest
```
**Response DTO:** `PodcastResponseDto[]`
**S3 Fields:**
- `thumbnailUrl` (nullable)
- `audioUrl` (required)

---

```
GET /api/content/user/podcasts/trending
```
**Response DTO:** `PodcastResponseDto[]`
**S3 Fields:**
- `thumbnailUrl` (nullable)
- `audioUrl` (required)

---

#### 2. Creator Podcasts (Requires Auth + ContentCreator Role)
```
GET /api/content/creator/podcasts
```
**Response DTO:** `PodcastResponseDto[]`
**S3 Fields:**
- `thumbnailUrl` (nullable)
- `audioUrl` (required)

**Auth:** JWT Bearer Token + Policy="ContentCreator"

---

```
GET /api/content/creator/podcasts/{id}
```
**Response DTO:** `PodcastResponseDto`
**S3 Fields:**
- `thumbnailUrl` (nullable)
- `audioUrl` (required)

---

#### 3. CMS Podcasts (Requires Auth + CommunityModerator Role)
```
GET /api/content/cms/podcasts
```
**Response DTO:** `PodcastResponseDto[]`
**S3 Fields:**
- `thumbnailUrl` (nullable)
- `audioUrl` (required)

**Auth:** JWT Bearer Token + Policy="CommunityModerator"

---

```
GET /api/content/cms/podcasts/{id}
```
**Response DTO:** `PodcastResponseDto`
**S3 Fields:**
- `thumbnailUrl` (nullable)
- `audioUrl` (required)

---

### Flashcards Endpoints

```
GET /api/content/flashcards
```
**Response DTO:** `FlashcardDto[]`
**S3 Fields:**
- `imageUrl` (nullable)

**Auth:** JWT Bearer Token

---

```
GET /api/content/flashcards/{id}
```
**Response DTO:** `FlashcardDto`
**S3 Fields:**
- `imageUrl` (nullable)

---

### Postcards Endpoints

```
GET /api/content/postcards
```
**Response DTO:** `PostcardDto[]`
**S3 Fields:**
- `imageUrl` (required)

**Auth:** JWT Bearer Token

---

```
GET /api/content/postcards/{id}
```
**Response DTO:** `PostcardDto`
**S3 Fields:**
- `imageUrl` (required)

---

### Community Endpoints

```
GET /api/content/user/community/posts
```
**Response DTO:** `CommunityPostDto[]`
**S3 Fields:**
- `attachmentUrls[]` (array of S3 URLs)

**Note:** Nếu có attachments là images/videos

---

## 👤 UserService

### Profile Endpoints

```
GET /api/user/profile
```
**Response DTO:** `UserProfileDto`
**S3 Fields:**
- `avatarUrl` (nullable)

**Auth:** JWT Bearer Token

**Example Response:**
```json
{
  "userId": "user-123",
  "fullName": "John Doe",
  "avatarUrl": "https://bucket.s3.region.amazonaws.com/avatars/user-123.jpg?X-Amz-...",
  "email": "john@example.com"
}
```

---

```
GET /api/user/profile/{userId}
```
**Response DTO:** `UserProfileDto`
**S3 Fields:**
- `avatarUrl` (nullable)

---

```
GET /api/cms/users/{userId}
```
**Response DTO:** `UserProfileDto`
**S3 Fields:**
- `avatarUrl` (nullable)

**Auth:** JWT Bearer Token + Admin Role

---

### Creator Applications Endpoints

```
GET /api/creatorapplications/{id}
```
**Response DTO:** `CreatorApplicationDto`
**S3 Fields:**
- `documentUrls[]` (array of S3 URLs for application documents)

**Auth:** JWT Bearer Token

**Example Response:**
```json
{
  "id": "app-123",
  "userId": "user-456",
  "documentUrls": [
    "https://bucket.s3.region.amazonaws.com/documents/id-card.pdf?X-Amz-...",
    "https://bucket.s3.region.amazonaws.com/documents/certificate.pdf?X-Amz-..."
  ],
  "status": "Pending"
}
```

---

```
GET /api/creatorapplications/pending
```
**Response DTO:** `CreatorApplicationDto[]`
**S3 Fields:**
- `documentUrls[]` (array)

**Auth:** JWT Bearer Token + Admin Role

---

## 🚫 Endpoints NOT Affected (Write Operations)

### These endpoints are NOT transformed (by design):

#### Upload Endpoints (POST)
```
POST /api/content/creator/upload/audio
POST /api/content/creator/upload/thumbnail
POST /api/user/fileupload/avatar
POST /api/user/fileupload/application-document
```
**Reason:** POST requests return original S3 URLs for immediate use

---

#### Presigned URL Generation (POST)
```
POST /api/content/fileupload/presigned-url
POST /api/user/fileupload/presigned-url
```
**Reason:** Already returns presigned URLs

---

#### Delete Operations (DELETE)
```
DELETE /api/content/creator/upload/{fileUrl}
DELETE /api/user/fileupload/avatar
```
**Reason:** DELETE doesn't need transformation

---

## 📊 Summary Statistics

### Total Endpoints with S3 Transformation
| Service | Total Endpoints | S3 Fields |
|---------|----------------|-----------|
| ContentService | 14 | `thumbnailUrl`, `audioUrl`, `imageUrl` |
| UserService | 5 | `avatarUrl`, `documentUrls[]` |
| **Total** | **19** | **5 unique field types** |

### S3 Field Types
| Field Name | Type | Services Using | Common Use Case |
|------------|------|----------------|-----------------|
| `thumbnailUrl` | string? | ContentService | Podcast/content preview images |
| `audioUrl` | string | ContentService | Podcast audio files |
| `imageUrl` | string | ContentService | Flashcard/postcard images |
| `avatarUrl` | string? | UserService | User profile pictures |
| `documentUrls` | string[] | UserService | Creator application documents |

---

## 🔍 Detection Rules

### Middleware Auto-Detects:
1. **Property Names** (case-insensitive):
   - thumbnailUrl, audioUrl, imageUrl
   - fileUrl, avatarUrl, documentUrl
   - videoUrl, coverUrl, bannerUrl, attachmentUrl

2. **URL Patterns**:
   - `https://*.s3*.amazonaws.com/*`
   - `https://*.cloudfront.net/*`

3. **Request Types**:
   - Only `GET` requests
   - Response `Content-Type: application/json`
   - Status codes `200-299`

---

## 🧪 Testing Matrix

### Test Cases
| Test Case | Endpoint | Expected Behavior |
|-----------|----------|-------------------|
| GET podcast list | `/api/content/user/podcasts` | ✅ audioUrl & thumbnailUrl transformed |
| GET single podcast | `/api/content/user/podcasts/{id}` | ✅ audioUrl & thumbnailUrl transformed |
| GET user profile | `/api/user/profile` | ✅ avatarUrl transformed |
| POST create podcast | `/api/content/creator/podcasts` | ❌ No transformation (POST) |
| DELETE podcast | `/api/content/creator/podcasts/{id}` | ❌ No transformation (DELETE) |
| GET without auth | `/api/content/user/podcasts` | ✅ Transformed (public endpoint) |
| GET with expired JWT | `/api/user/profile` | ❌ 401 Unauthorized (before middleware) |

---

## 📝 Frontend Integration Examples

### React Example
```typescript
// Fetch podcast data
const response = await fetch('/api/content/user/podcasts');
const data = await response.json();

// URLs are already presigned - use directly!
{data.map(podcast => (
  <div key={podcast.id}>
    <img src={podcast.thumbnailUrl} alt={podcast.title} />
    <audio src={podcast.audioUrl} controls />
  </div>
))}

// Handle URL expiration (after 1 hour)
const handleAudioError = async (podcastId) => {
  // Re-fetch to get fresh presigned URL
  const fresh = await fetch(`/api/content/user/podcasts/${podcastId}`);
  const updated = await fresh.json();
  setPodcast(updated);
};
```

### Vue Example
```vue
<template>
  <div v-for="podcast in podcasts" :key="podcast.id">
    <img :src="podcast.thumbnailUrl" :alt="podcast.title" />
    <audio :src="podcast.audioUrl" controls @error="handleError(podcast.id)" />
  </div>
</template>

<script setup>
const podcasts = ref([]);

const loadPodcasts = async () => {
  const res = await fetch('/api/content/user/podcasts');
  podcasts.value = await res.json();
};

const handleError = async (id) => {
  // Refresh podcast data to get new presigned URL
  const res = await fetch(`/api/content/user/podcasts/${id}`);
  const updated = await res.json();
  const index = podcasts.value.findIndex(p => p.id === id);
  podcasts.value[index] = updated;
};
</script>
```

---

## 🎯 Migration Checklist for Frontend

- [ ] Remove any manual presigned URL fetching logic
- [ ] Update API integration to use URLs directly from responses
- [ ] Implement error handling for expired URLs (403 responses)
- [ ] Add retry logic to refresh data when URLs expire
- [ ] Test with various content types (audio, images, documents)
- [ ] Update documentation for FE team
- [ ] Remove any S3 SDK dependencies from frontend

---

**Last Updated:** October 14, 2025  
**Version:** 1.0.0  
**Maintained By:** Backend Team
