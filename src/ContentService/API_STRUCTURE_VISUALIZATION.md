# ContentService API Structure Visualization

## 📊 Controller Architecture

```
ContentService.API/Controllers/
│
├── 👥 User/ (Public APIs - No Auth Required)
│   ├── UserPodcastsController.cs
│   │   └── 8 endpoints: Browse published podcasts
│   └── UserCommunityController.cs
│       └── 4 endpoints: Browse community stories
│
├── 🎨 Creator/ (Content Creator APIs - Auth Required)
│   ├── CreatorPodcastsController.cs
│   │   └── 7 endpoints: Manage my podcasts
│   ├── CreatorCommunityController.cs
│   │   └── 4 endpoints: Manage my stories
│   └── CreatorFileUploadController.cs
│       └── 7 endpoints: Upload content files
│
├── 🛡️ Cms/ (Admin/Moderator APIs - Admin Auth Required)
│   ├── CmsPodcastsController.cs
│   │   └── 10 endpoints: Moderate all podcasts
│   └── CmsCommunityController.cs
│       └── 5 endpoints: Moderate all stories
│
└── 🔧 Shared/ (Legacy - To be deprecated)
    ├── PodcastsController.cs [OLD]
    ├── CommunityController.cs [OLD]
    ├── FileUploadController.cs [OLD]
    ├── HealthController.cs [KEEP]
    └── EventTestController.cs [KEEP]
```

## 🔄 Request Flow

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │
       ├──────────────────────────────────────┐
       │                                      │
       v                                      v
┌──────────────┐                    ┌──────────────┐
│  No Auth     │                    │  With JWT    │
│  (Public)    │                    │  Token       │
└──────┬───────┘                    └──────┬───────┘
       │                                   │
       v                                   │
   ┌────────┐                             │
   │  User  │                             │
   │  APIs  │                             │
   └────────┘                             │
                                          │
                          ┌───────────────┴───────────────┐
                          │                               │
                          v                               v
                   ┌─────────────┐              ┌────────────────┐
                   │  Creator    │              │  Admin/        │
                   │  Role       │              │  Moderator     │
                   └──────┬──────┘              │  Role          │
                          │                     └────────┬───────┘
                          v                              │
                   ┌──────────┐                         │
                   │ Creator  │                         │
                   │  APIs    │                         v
                   └──────────┘                  ┌──────────┐
                                                 │   CMS    │
                                                 │   APIs   │
                                                 └──────────┘
```

## 🎯 API Endpoints Distribution

```
┌─────────────────────────────────────────────────────────────────┐
│                    ContentService APIs                          │
│                      (45 endpoints total)                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  👥 User APIs (12 endpoints)                                    │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ • 8 Podcast endpoints (browse, search, filter)            │ │
│  │ • 4 Community endpoints (browse, search)                  │ │
│  │ 🔓 Public Access                                          │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  🎨 Creator APIs (18 endpoints)                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ • 7 Podcast management endpoints                          │ │
│  │ • 4 Community management endpoints                        │ │
│  │ • 7 File upload endpoints                                 │ │
│  │ 🔐 Requires: ContentCreator Role                          │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  🛡️ CMS APIs (15 endpoints)                                     │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ • 10 Podcast moderation endpoints                         │ │
│  │ • 5 Community moderation endpoints                        │ │
│  │ 🔐 Requires: CommunityModerator or Admin Role             │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## 🔐 Authorization Matrix

```
┌──────────────────┬─────────┬──────────┬──────────┬─────────┐
│    Endpoint      │  User   │ Creator  │Moderator │  Admin  │
├──────────────────┼─────────┼──────────┼──────────┼─────────┤
│ /api/user/*      │   ✅    │    ✅    │    ✅    │   ✅    │
│ (Public)         │  (No)   │  (No)    │   (No)   │  (No)   │
│                  │  Auth   │  Auth    │   Auth   │  Auth   │
├──────────────────┼─────────┼──────────┼──────────┼─────────┤
│ /api/creator/*   │   ❌    │    ✅    │    ✅    │   ✅    │
│ (My Content)     │         │  (JWT)   │  (JWT)   │  (JWT)  │
├──────────────────┼─────────┼──────────┼──────────┼─────────┤
│ /api/cms/*       │   ❌    │    ❌    │    ✅    │   ✅    │
│ (All Content)    │         │          │  (JWT)   │  (JWT)  │
├──────────────────┼─────────┼──────────┼──────────┼─────────┤
│ /api/cms/bulk/*  │   ❌    │    ❌    │    ❌    │   ✅    │
│ (Bulk Ops)       │         │          │          │  (JWT)  │
└──────────────────┴─────────┴──────────┴──────────┴─────────┘

Legend:
✅ = Allowed
❌ = Denied
(JWT) = Requires JWT Token with specific role
(No Auth) = Public access, no authentication needed
```

## 📊 Content Status Flow

```
Creator                  Moderator               User
   │                         │                    │
   ├─► Create Podcast        │                    │
   │   (Draft)               │                    │
   │                         │                    │
   ├─► Submit for Review     │                    │
   │   (PendingModeration)   │                    │
   │                         │                    │
   │                    ┌────┴────┐               │
   │                    │ Review  │               │
   │                    └────┬────┘               │
   │                         │                    │
   │                    ┌────┴────┐               │
   │                    │ Approve │               │
   │                    └────┬────┘               │
   │                         │                    │
   │                    (Published)───────────────┤
   │                                              │
   │                                              ▼
   │                                        View Podcast
   │                                              │
   │                                              │
   │                    ┌─────────────────────────┤
   │                    │  Like / Favorite        │
   │                    └─────────────────────────┘
   │
   ├─► View Stats
   │   (Views, Likes)
   │
   
Alternative Flow:
   
   │                    ┌────┐
   │                    │Reject│
   │                    └────┬────┘
   │                         │
   │◄────────────────────────┤
   │  Notification           │
   │  (Need Changes)         │
   │                         │
   ├─► Edit & Resubmit       │
```

## 🎨 Swagger UI Groups

```
┌─────────────────────────────────────────────────────────┐
│              Swagger UI - ContentService                │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  [Dropdown: Select Definition]                          │
│  ┌─────────────────────────────────────────────────┐   │
│  │ 📱 ContentService - User APIs              ▼    │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ 🎨 ContentService - Creator APIs                │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ 🛡️ ContentService - CMS APIs                    │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ 📱 User APIs (Currently Selected)               │   │
│  ├─────────────────────────────────────────────────┤   │
│  │                                                 │   │
│  │  GET  /api/user/podcasts                       │   │
│  │  GET  /api/user/podcasts/{id}                  │   │
│  │  GET  /api/user/podcasts/by-emotion/{emotion}  │   │
│  │  GET  /api/user/podcasts/by-topic/{topic}      │   │
│  │  GET  /api/user/podcasts/search                │   │
│  │  GET  /api/user/podcasts/trending              │   │
│  │  GET  /api/user/podcasts/latest                │   │
│  │  ...                                            │   │
│  │                                                 │   │
│  │  [Authorize] 🔓 No Auth Required               │   │
│  │                                                 │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

## 📦 Data Flow Example

### User Browsing Podcasts

```
┌─────────┐
│ Mobile  │
│  App    │
└────┬────┘
     │
     │ GET /api/user/podcasts?page=1&pageSize=10
     ▼
┌────────────────┐
│ UserPodcasts   │
│  Controller    │
└────┬───────────┘
     │
     │ GetPodcastsQuery(Status=Published)
     ▼
┌────────────────┐
│   MediatR      │
│   Handler      │
└────┬───────────┘
     │
     │ GetPodcastsAsync(status=Published)
     ▼
┌────────────────┐
│  Repository    │
└────┬───────────┘
     │
     │ SELECT * FROM Podcasts WHERE Status = 5
     ▼
┌────────────────┐
│   Database     │
└────┬───────────┘
     │
     │ Return: List<Podcast>
     ▼
┌────────────────┐
│  Map to DTO    │
└────┬───────────┘
     │
     │ PodcastDto[]
     ▼
┌────────────────┐
│   JSON         │
│  Response      │
└────────────────┘
```

### Creator Uploading Podcast

```
┌─────────┐
│Creator  │
│Dashboard│
└────┬────┘
     │
     │ POST /api/creator/podcasts
     │ + JWT Token (ContentCreator role)
     │ + Audio File
     ▼
┌────────────────┐
│CreatorPodcasts │
│  Controller    │
└────┬───────────┘
     │
     │ Verify JWT & Role
     ▼
┌────────────────┐
│Authorization   │
│  Middleware    │
└────┬───────────┘
     │
     │ CreatePodcastCommand
     ▼
┌────────────────┐
│   MediatR      │
│   Handler      │
└────┬───────────┘
     │
     │ 1. Upload to S3
     ▼
┌────────────────┐
│  S3 Storage    │
└────┬───────────┘
     │
     │ 2. Save to DB (Status=PendingModeration)
     ▼
┌────────────────┐
│  Repository    │
└────┬───────────┘
     │
     │ 3. Publish Event
     ▼
┌────────────────┐
│  RabbitMQ      │
│ (Event Bus)    │
└────────────────┘
```

---

## 🎯 Key Metrics

```
Total Controllers:     10
├── New Controllers:   7 ✅
├── Legacy:           3 ⏳
└── Shared:           2 ✅

Total Endpoints:      45
├── User APIs:        12 (27%)
├── Creator APIs:     18 (40%)
└── CMS APIs:         15 (33%)

Authorization:
├── Public:           12 endpoints (27%)
├── Creator Auth:     18 endpoints (40%)
└── Admin Auth:       15 endpoints (33%)

Documentation:
├── API Docs:         3 files ✅
├── XML Comments:     100% ✅
└── Swagger Groups:   3 groups ✅
```

---

*This visualization helps understand the new API architecture at a glance.*
