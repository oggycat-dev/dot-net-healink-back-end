# ContentService API Architecture

## 📚 Tổng quan

ContentService đã được tái cấu trúc để phân tách rõ ràng các API theo vai trò người dùng:

- **User APIs**: Cho người dùng bình thường (người nghe/xem nội dung)
- **Creator APIs**: Cho người tạo nội dung (Content Creator)
- **CMS APIs**: Cho quản trị viên và moderator

## 🎯 Cấu trúc API

### 1. User APIs (`/api/user/*`)

**Mục đích**: Cho phép người dùng bình thường truy cập và xem nội dung đã được phê duyệt

#### Podcasts - `/api/user/podcasts`

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/` | Lấy danh sách podcast đã published | ❌ No |
| GET | `/{id}` | Xem chi tiết podcast | ❌ No |
| GET | `/by-emotion/{emotion}` | Lọc theo cảm xúc | ❌ No |
| GET | `/by-topic/{topic}` | Lọc theo chủ đề | ❌ No |
| GET | `/series/{seriesName}` | Lọc theo series | ❌ No |
| GET | `/search?keyword={keyword}` | Tìm kiếm podcast | ❌ No |
| GET | `/trending` | Podcast phổ biến | ❌ No |
| GET | `/latest` | Podcast mới nhất | ❌ No |

**Đặc điểm**:
- Tất cả endpoints đều public (AllowAnonymous)
- Chỉ trả về content có status = `Published`
- Phù hợp cho frontend người dùng cuối

#### Community - `/api/user/community`

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/stories` | Xem community stories | ❌ No |
| GET | `/stories/{id}` | Chi tiết story | ❌ No |
| GET | `/stories/trending` | Stories phổ biến | ❌ No |
| GET | `/stories/search` | Tìm kiếm stories | ❌ No |

---

### 2. Creator APIs (`/api/creator/*`)

**Mục đích**: Cho phép content creator tạo và quản lý nội dung của họ

#### Podcasts - `/api/creator/podcasts`

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/my-podcasts` | Xem podcast của tôi | ✅ ContentCreator |
| GET | `/{id}` | Chi tiết podcast của tôi | ✅ ContentCreator |
| POST | `/` | Tạo podcast mới | ✅ ContentCreator |
| PUT | `/{id}` | Cập nhật podcast | ✅ ContentCreator |
| DELETE | `/{id}` | Xóa podcast | ✅ ContentCreator |
| GET | `/{id}/stats` | Thống kê podcast | ✅ ContentCreator |
| GET | `/dashboard` | Dashboard tổng quan | ✅ ContentCreator |

**Đặc điểm**:
- Yêu cầu role: `ContentCreator`, `Expert`, hoặc `Admin`
- Creator chỉ thấy và quản lý nội dung của mình
- Có thể xem tất cả status (draft, pending, approved, rejected)

#### Community - `/api/creator/community`

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/my-stories` | Xem stories của tôi | ✅ Authenticated |
| POST | `/stories` | Tạo story mới | ✅ Authenticated |
| PUT | `/stories/{id}` | Cập nhật story | ✅ Authenticated |
| DELETE | `/stories/{id}` | Xóa story | ✅ Authenticated |

#### File Upload - `/api/creator/upload`

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/podcast/audio` | Upload audio file | ✅ ContentCreator |
| POST | `/podcast/thumbnail` | Upload thumbnail | ✅ ContentCreator |
| POST | `/podcast/transcript` | Upload transcript | ✅ ContentCreator |
| POST | `/flashcard` | Upload flashcard image | ✅ ContentCreator |
| POST | `/postcard` | Upload postcard image | ✅ ContentCreator |
| POST | `/community/image` | Upload community image | ✅ Authenticated |
| POST | `/presigned-url` | Generate presigned URL | ✅ ContentCreator |

**Supported Formats**:
- Audio: `.mp3`, `.wav`, `.m4a`, `.aac` (max 500MB)
- Images: `.jpg`, `.jpeg`, `.png`, `.webp` (max 10MB)
- Documents: `.txt`, `.pdf`, `.docx`, `.srt`

---

### 3. CMS APIs (`/api/cms/*`)

**Mục đích**: Cho phép admin/moderator quản lý toàn bộ nội dung trong hệ thống

#### Podcasts - `/api/cms/podcasts`

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/` | Xem tất cả podcast | ✅ Moderator |
| GET | `/pending` | Podcast chờ duyệt | ✅ Moderator |
| GET | `/{id}` | Chi tiết podcast | ✅ Moderator |
| POST | `/{id}/approve` | Duyệt podcast | ✅ Moderator |
| POST | `/{id}/reject` | Từ chối podcast | ✅ Moderator |
| GET | `/{id}/analytics` | Thống kê chi tiết | ✅ Moderator |
| GET | `/statistics` | Thống kê tổng quan | ✅ Moderator |
| GET | `/moderation-log` | Lịch sử duyệt | ✅ Admin |
| POST | `/bulk-approve` | Duyệt hàng loạt | ✅ Admin |
| DELETE | `/{id}` | Xóa podcast (force) | ✅ Admin |

**Đặc điểm**:
- Yêu cầu role: `CommunityModerator` hoặc `Admin`
- Có thể xem và quản lý tất cả content
- Có thể xem tất cả status và thống kê

#### Community - `/api/cms/community`

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/stories` | Xem tất cả stories | ✅ Moderator |
| GET | `/stories/pending` | Stories chờ duyệt | ✅ Moderator |
| POST | `/stories/{id}/approve` | Duyệt story | ✅ Moderator |
| POST | `/stories/{id}/reject` | Từ chối story | ✅ Moderator |
| GET | `/statistics` | Thống kê | ✅ Moderator |

---

## 🔐 Authorization Policies

### ContentCreator Policy
```csharp
RequireRole("ContentCreator", "Expert", "Admin")
```

### CommunityModerator Policy
```csharp
RequireRole("CommunityModerator", "Admin")
```

### AdminOnly Policy
```csharp
RequireRole("Admin")
```

---

## 📊 Content Status Flow

```
Draft (1)
    ↓
PendingReview (2)
    ↓
PendingModeration (3)
    ↓
    ├─→ Approved (4) → Published (5)
    └─→ Rejected (6)
```

- **Draft**: Creator đang soạn thảo
- **PendingReview**: Chờ review tự động
- **PendingModeration**: Chờ moderator duyệt
- **Approved**: Đã được duyệt
- **Published**: Công khai cho user
- **Rejected**: Bị từ chối (cần sửa)
- **Archived**: Đã lưu trữ

---

## 🎨 Swagger UI

Swagger được chia thành 3 nhóm riêng biệt:

1. **User APIs**: `http://localhost:5003/swagger` → Chọn "ContentService - User APIs"
2. **Creator APIs**: `http://localhost:5003/swagger` → Chọn "ContentService - Creator APIs"
3. **CMS APIs**: `http://localhost:5003/swagger` → Chọn "ContentService - CMS APIs"

---

## 🚀 Testing Endpoints

### User - Browse Podcasts (No Auth)
```bash
# Get published podcasts
curl http://localhost:5003/api/user/podcasts

# Search podcasts
curl http://localhost:5003/api/user/podcasts/search?keyword=meditation

# Get by emotion
curl http://localhost:5003/api/user/podcasts/by-emotion/1
```

### Creator - Manage Content (Auth Required)
```bash
# Get my podcasts
curl -H "Authorization: Bearer {token}" \
  http://localhost:5003/api/creator/podcasts/my-podcasts

# Create podcast
curl -X POST -H "Authorization: Bearer {token}" \
  -F "title=My Podcast" \
  -F "description=Description" \
  -F "audioFile=@audio.mp3" \
  -F "duration=300" \
  http://localhost:5003/api/creator/podcasts
```

### CMS - Moderate Content (Auth Required)
```bash
# Get pending podcasts
curl -H "Authorization: Bearer {token}" \
  http://localhost:5003/api/cms/podcasts/pending

# Approve podcast
curl -X POST -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"approvalNotes": "Good content"}' \
  http://localhost:5003/api/cms/podcasts/{id}/approve
```

---

## 📁 File Structure

```
ContentService.API/
├── Controllers/
│   ├── User/
│   │   ├── UserPodcastsController.cs
│   │   └── UserCommunityController.cs
│   ├── Creator/
│   │   ├── CreatorPodcastsController.cs
│   │   ├── CreatorCommunityController.cs
│   │   └── CreatorFileUploadController.cs
│   ├── Cms/
│   │   ├── CmsPodcastsController.cs
│   │   └── CmsCommunityController.cs
│   ├── HealthController.cs (shared)
│   └── [Old Controllers - To be deprecated]
│       ├── PodcastsController.cs
│       ├── CommunityController.cs
│       └── FileUploadController.cs
├── Configurations/
│   ├── ServiceConfiguration.cs
│   └── SwaggerExtensions.cs
└── DTOs/
    └── PodcastDTOs.cs
```

---

## 🔄 Migration Plan

### Phase 1: ✅ Completed
- Tạo structure mới với User/Creator/CMS controllers
- Cấu hình Swagger với API groups
- Document API architecture

### Phase 2: 🚧 In Progress
- Test các endpoints mới
- Implement các features còn thiếu (analytics, interactions)
- Update frontend để sử dụng API mới

### Phase 3: 📋 Planned
- Deprecate old controllers
- Clean up code
- Add comprehensive tests

---

## 🎯 Best Practices

### User APIs
- ✅ Luôn filter theo status = Published
- ✅ Support pagination
- ✅ AllowAnonymous cho public content
- ✅ Include detailed logging

### Creator APIs
- ✅ Verify ownership before operations
- ✅ Return appropriate error codes (401, 403, 404)
- ✅ Validate file uploads
- ✅ Track creator activities

### CMS APIs
- ✅ Require moderator/admin roles
- ✅ Log all moderation actions
- ✅ Provide comprehensive analytics
- ✅ Support bulk operations

---

## 📝 TODO

- [ ] Implement interaction endpoints (like, favorite, view tracking)
- [ ] Add comprehensive analytics for creators and admins
- [ ] Implement GetPodcastsByCreatorId query
- [ ] Add sorting options (by date, popularity, etc.)
- [ ] Implement comment system
- [ ] Add notification when content is approved/rejected
- [ ] Create dashboard with charts and metrics
- [ ] Add rate limiting for uploads
- [ ] Implement content recommendation engine

---

## 🤝 Contributing

Khi thêm API mới, hãy đặt vào đúng folder:
- Public content browsing → `User/`
- Content creation/management → `Creator/`
- Moderation/administration → `Cms/`

Và nhớ set `ApiExplorerSettings(GroupName = "User|Creator|CMS")` cho controller.
