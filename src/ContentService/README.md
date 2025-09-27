# 📚 Content Service

> **Quản lý toàn bộ nội dung của Healink**: Podcasts, Flashcards, Postcards, Letters to Myself, và Community Stories

## 🎯 **Chức năng chính**

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

## 🔌 **API Endpoints**

### **Podcasts**
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