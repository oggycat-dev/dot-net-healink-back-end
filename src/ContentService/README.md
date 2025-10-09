# 📚 Content Service

> **Quản lý toàn bộ nội dung của Healink**: Podcasts, Flashcards, Postcards, Letters to Myself, và Community Stories

## � **NEW: API Refactoring Complete**

ContentService đã được **refactor hoàn toàn** với cấu trúc API mới, phân tách rõ ràng theo vai trò:

- 👥 **User APIs** (`/api/user/*`) - Cho người dùng cuối (public)
- 🎨 **Creator APIs** (`/api/creator/*`) - Cho content creator (auth)
- 🛡️ **CMS APIs** (`/api/cms/*`) - Cho admin/moderator (admin auth)

📖 **Xem chi tiết**: [API Architecture](./API_ARCHITECTURE.md) | [Refactoring Complete](./REFACTORING_COMPLETE.md)

---

## �🎯 **Chức năng chính**

### **Content Management**
- ✅ **Podcasts**: Audio content với transcript, analytics
- ✅ **Flashcards**: Q&A cards liên kết với podcasts  
- ✅ **Postcards**: Inspirational images với quotes
- ✅ **Letters to Myself**: Personal reflection letters
- ✅ **Community Stories**: User-generated content

### **Moderation & Workflow**
- ✅ **Content approval** workflow (Draft → Review → Approved → Published)
- ✅ **Community moderation** cho user-generated content
- ✅ **Role-based access** control
- ✅ **Automated content categorization** (emotions, topics)

### **Analytics & Engagement**
- ✅ **View tracking**, likes, shares, comments
- ✅ **User interactions** và behavior analytics
- ✅ **Content recommendations** integration
- ✅ **Performance metrics** cho content creators

## 🏗️ **Architecture**

```
ContentService/
├── ContentService.Domain/       # Entities & Business Rules
│   ├── Entities/               # Content, Podcast, Flashcard, etc.
│   └── Enums/                  # ContentType, Status, Categories
├── ContentService.Application/  # Business Logic & DTOs  
│   ├── Features/               # CQRS handlers
│   │   ├── Podcasts/          # Podcast operations
│   │   └── Community/         # Community story operations
├── ContentService.Infrastructure/ # Data Access & External Services
│   ├── Context/               # EF Core DbContext
│   └── Repositories/          # Data layer
└── ContentService.API/         # Controllers & Configuration
    ├── Controllers/           # REST API endpoints
    └── Program.cs            # Startup configuration
```

## 🔌 **API Endpoints** (NEW Structure)

### 👥 **User APIs** - Public Access
Browse và consume content đã published

```http
# Podcasts
GET    /api/user/podcasts                         # Browse published podcasts
GET    /api/user/podcasts/{id}                    # Get podcast details
GET    /api/user/podcasts/by-emotion/{emotion}    # Filter by emotion
GET    /api/user/podcasts/by-topic/{topic}        # Filter by topic
GET    /api/user/podcasts/search?keyword=...      # Search podcasts
GET    /api/user/podcasts/trending                # Trending podcasts
GET    /api/user/podcasts/latest                  # Latest podcasts

# Community
GET    /api/user/community/stories                # Browse stories
GET    /api/user/community/stories/{id}           # Get story details
```

### 🎨 **Creator APIs** - Requires ContentCreator Role
Manage own content

```http
# My Podcasts
GET    /api/creator/podcasts/my-podcasts          # My podcast list
GET    /api/creator/podcasts/{id}                 # My podcast detail
POST   /api/creator/podcasts                      # Create new podcast
PUT    /api/creator/podcasts/{id}                 # Update my podcast
DELETE /api/creator/podcasts/{id}                 # Delete my podcast
GET    /api/creator/podcasts/{id}/stats           # Podcast statistics
GET    /api/creator/podcasts/dashboard            # Creator dashboard

# File Uploads
POST   /api/creator/upload/podcast/audio          # Upload audio (max 500MB)
POST   /api/creator/upload/podcast/thumbnail      # Upload thumbnail
POST   /api/creator/upload/podcast/transcript     # Upload transcript
POST   /api/creator/upload/community/image        # Upload community image

# My Community Stories
GET    /api/creator/community/my-stories          # My stories
POST   /api/creator/community/stories             # Create story
PUT    /api/creator/community/stories/{id}        # Update story
DELETE /api/creator/community/stories/{id}        # Delete story
```

### 🛡️ **CMS APIs** - Requires Moderator/Admin Role
Manage all content and moderation

```http
# Podcast Moderation
GET    /api/cms/podcasts                          # All podcasts
GET    /api/cms/podcasts/pending                  # Pending podcasts
GET    /api/cms/podcasts/{id}                     # Podcast detail (admin view)
POST   /api/cms/podcasts/{id}/approve             # Approve podcast
POST   /api/cms/podcasts/{id}/reject              # Reject podcast
GET    /api/cms/podcasts/{id}/analytics           # Detailed analytics
GET    /api/cms/podcasts/statistics               # Overall statistics
DELETE /api/cms/podcasts/{id}                     # Force delete (admin only)

# Community Moderation  
GET    /api/cms/community/stories                 # All stories
GET    /api/cms/community/stories/pending         # Pending stories
POST   /api/cms/community/stories/{id}/approve    # Approve story
POST   /api/cms/community/stories/{id}/reject     # Reject story
GET    /api/cms/community/statistics              # Statistics
```

📖 **Full API Documentation**: See [API_ARCHITECTURE.md](./API_ARCHITECTURE.md)

---

## 🔌 **Legacy API Endpoints** (To be deprecated)

### **Podcasts** (Old)
```
GET    /api/content/podcasts                    # List podcasts
GET    /api/content/podcasts/{id}               # Get specific podcast  
POST   /api/content/podcasts                    # Create podcast (Creator only)
PUT    /api/content/podcasts/{id}               # Update podcast
DELETE /api/content/podcasts/{id}               # Delete podcast
POST   /api/content/podcasts/{id}/approve       # Approve podcast (Moderator)
GET    /api/content/podcasts/{id}/related-flashcards # Get related content
```

### **Community Stories**
```
GET    /api/content/community/stories           # List community stories
POST   /api/content/community/stories           # Create story (Community Member)
POST   /api/content/community/stories/{id}/approve    # Approve (Moderator)
POST   /api/content/community/stories/{id}/reject     # Reject (Moderator)
POST   /api/content/community/stories/{id}/helpful    # Mark helpful
```

### **Flashcards** (Coming soon)
```
GET    /api/content/flashcards                  # List flashcards
POST   /api/content/flashcards                  # Create flashcard
GET    /api/content/flashcards/{id}/related-podcast  # Get related podcast
```

## 🎭 **User Roles & Permissions**

| Role | Podcasts | Flashcards | Community Stories | Moderation |
|------|----------|------------|------------------|------------|
| **Free User** | View | View | View | ❌ |
| **Premium User** | View + Download | View + Practice | View + Comment | ❌ |
| **Community Member** | View | View | Create + View | ❌ |
| **Content Creator** | Full CRUD | Full CRUD | View | ❌ |
| **Expert Collaborator** | Full CRUD | Full CRUD | View | ❌ |
| **Community Moderator** | View | View | Full Access | ✅ Community |
| **Content Editor** | Full CRUD | Full CRUD | Full Access | ✅ All Content |

## 🗄️ **Database Schema**

### **Core Tables**
- `Contents` - Base table for all content types
- `Podcasts` - Podcast-specific data
- `Flashcards` - Q&A content  
- `Postcards` - Visual inspiration content
- `LettersToMyself` - Personal reflection content
- `CommunityStories` - User-generated stories

### **Engagement Tables**
- `Comments` - User comments on content
- `ContentInteractions` - Views, likes, shares
- `ContentRatings` - User ratings 1-5 stars

## 🚀 **Quick Start**

### **Local Development**
```bash
# Start với Docker Compose
docker-compose up contentservice-api

# Hoặc run trực tiếp
cd src/ContentService/ContentService.API
dotnet run
```

### **API Documentation**
- Swagger UI: `http://localhost:5004`
- Health check: `http://localhost:5004/health`

### **Database Migrations**
```bash
cd src/ContentService/ContentService.Infrastructure
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 🔧 **Configuration**

### **Environment Variables**
```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=healink_content_db;Username=healink_user;Password=xxx"
JwtConfig__Key="your-jwt-secret"
RabbitMQ__HostName="localhost"
Redis__ConnectionString="localhost:6379"
```

### **Database Setup**
- PostgreSQL với separate database: `healink_content_db`
- Auto-created via `init-multiple-databases.sh`
- EF Core migrations cho schema management

## 📊 **Performance & Scalability**

### **Optimizations**
- ✅ **Database indexing** cho search queries
- ✅ **JSON storage** cho flexible metadata
- ✅ **Pagination** cho large content lists
- ✅ **Caching strategy** với Redis
- ✅ **Async operations** cho better throughput

### **Monitoring**
- Health checks endpoint
- Serilog structured logging  
- Performance metrics integration ready

## 🔮 **Roadmap**

### **Phase 2 Features**
- [ ] **Full-text search** với Elasticsearch
- [ ] **Content versioning** system
- [ ] **AI-powered tagging** automation  
- [ ] **Advanced analytics** dashboard
- [ ] **Content scheduling** system
- [ ] **Multi-language support**

### **Integration Plans**
- [ ] **Recommendation Service** integration
- [ ] **Notification Service** for content updates
- [ ] **Analytics Service** for detailed reporting
- [ ] **CDN integration** for media files

---

🎉 **ContentService is ready for development!** Start với basic CRUD operations và expand theo business needs.