# 🎯 Podcast Recommendation Service - Business Flow

## Kiến trúc tổng quan

```
User Request (JWT) 
    ↓
[PodcastRecommendation API - C#]
    ↓ (UserId from JWT)
[FastAPI Service - AI Model]
    ↓ (Podcast IDs)
[ContentService API]
    ↓ (Full Podcast Entities)
Response to User
```

## 📋 Luồng nghiệp vụ chi tiết

### 1. **User Request với JWT Token**
- User gửi request đến `/api/recommendations/me`
- JWT token chứa `UserId` được extract bởi `ICurrentUserService`
- Authentication middleware verify token validity

### 2. **PodcastRecommendation API (C#)**
**File**: `RecommendationsController.cs`

```csharp
// Lấy UserId từ JWT
var userId = _currentUserService.UserId;

// Gọi FastAPI Service
var result = await _recommendationService.GetRecommendationsAsync(userId, limit, includeListened);
```

**Business Rules**:
- ✅ User phải authenticated (có JWT valid)
- ✅ Limit: 1-50 podcasts (default: 10)
- ✅ Option: `includeListened` (include podcasts user đã nghe)

### 3. **FastAPI Service - AI Model**
**File**: `ai_service/fastapi_service.py`

**Nhiệm vụ**:
- Nhận `userId` từ C# service
- Load AI model (collaborative filtering model đã train từ Kaggle)
- Tính toán recommendations dựa trên:
  - User listening history
  - User preferences
  - Similar users behavior
  - Podcast similarity matrix
  
**Input**: 
```json
{
  "user_id": "user_123",
  "limit": 10,
  "include_listened": false
}
```

**Output**:
```json
{
  "user_id": "user_123",
  "recommendations": [
    {
      "podcast_id": "p_00189",
      "predicted_rating": 4.5,
      "confidence_score": 0.85,
      "category": "Career",
      "topics": "Quản lý thời gian hiệu quả"
    }
  ],
  "total_count": 10
}
```

### 4. **Integration với ContentService**
**Tùy chọn A**: FastAPI gọi ContentService để enrich data
- FastAPI nhận podcast IDs từ model
- Gọi ContentService API: `GET /api/podcasts/{id}`
- Trả về full podcast entity với complete information

**Tùy chọn B**: C# Service enrichment (Recommended)
- FastAPI chỉ trả về podcast IDs + predictions
- C# Service gọi ContentService để lấy full entities
- Map data và return response

### 5. **Response Format**
```json
{
  "userId": "user_123",
  "recommendations": [
    {
      "podcastId": "p_00189",
      "title": "Bài học 189: Quản lý thời gian hiệu quả",
      "topic": "Quản lý thời gian hiệu quả",
      "predictedRating": 4.5,
      "confidenceScore": 0.85,
      "recommendationReason": "Based on your listening history and similar users",
      "category": "Career",
      "durationMinutes": 20,
      "contentUrl": "https://..."
    }
  ],
  "totalFound": 10,
  "filteredListened": false,
  "generatedAt": "2025-10-10T07:00:00Z"
}
```

## 🔧 Configuration

### PodcastRecommendation API (appsettings.json)
```json
{
  "AIService": {
    "BaseUrl": "http://podcast-ai-service:8000",
    "Timeout": 30
  },
  "ContentService": {
    "BaseUrl": "http://contentservice-api",
    "Timeout": 10
  }
}
```

### FastAPI Service (Environment Variables)
```bash
USER_SERVICE_URL=http://userservice-api
CONTENT_SERVICE_URL=http://contentservice-api
MODEL_PATH=/app/models
```

## 📊 Business Rules

### Recommendation Logic
1. **New User (Cold Start)**:
   - Return popular podcasts từ ContentService
   - Filter theo categories phổ biến
   - Prioritize recent uploads

2. **Existing User with History**:
   - Collaborative filtering based on similar users
   - Content-based filtering theo topics listened
   - Hybrid approach combining both

3. **Filtering Rules**:
   - Exclude listened podcasts (unless `includeListened=true`)
   - Only published & active podcasts
   - Respect user blocked/hidden content
   - Apply content rating filters

4. **Ranking Factors**:
   - Predicted rating (from AI model): 40%
   - User preference match: 30%
   - Content popularity: 20%
   - Recency: 10%

## 🔐 Security & Authorization

### JWT Token Requirements
- **Required Claims**: `UserId`, `Email`, `Roles`
- **Token Validation**: Signature, Expiry, Issuer
- **Rate Limiting**: 10 requests/minute per user

### Role-Based Access
- **User**: Can only get own recommendations (`/me`)
- **Admin**: Can get any user's recommendations (`/user/{userId}`)
- **System**: Internal service calls (with service token)

## 📈 Performance Considerations

### Caching Strategy
1. **User Recommendations Cache**: 
   - TTL: 1 hour
   - Invalidate on: New listen event, User preference change
   
2. **AI Model Cache**:
   - Keep model in memory (FastAPI)
   - Reload only on deployment
   
3. **Content Cache**:
   - Cache podcast entities for 5 minutes
   - Reduce ContentService API calls

### Optimization
- **Batch Processing**: Support batch recommendations for multiple users
- **Async Operations**: All service calls use async/await
- **Connection Pooling**: Reuse HTTP connections
- **Circuit Breaker**: Fallback khi FastAPI service down

## 🧪 Testing Scenarios

### Test Case 1: New User Recommendation
```bash
curl -X GET "http://localhost:8083/api/recommendations/me?limit=5" \
  -H "Authorization: Bearer {jwt_token}"
```

**Expected**: Popular podcasts từ ContentService

### Test Case 2: Existing User with History
```bash
curl -X GET "http://localhost:8083/api/recommendations/me?limit=10" \
  -H "Authorization: Bearer {jwt_token}"
```

**Expected**: Personalized recommendations từ AI model

### Test Case 3: Admin Get User Recommendations
```bash
curl -X GET "http://localhost:8083/api/recommendations/user/{userId}?limit=5" \
  -H "Authorization: Bearer {admin_jwt_token}"
```

**Expected**: Recommendations for specified user

## 📝 Dependencies

### C# Services
- ✅ AuthService: JWT validation & user authentication
- ✅ ContentService: Podcast entities & metadata
- ✅ UserService: User profiles & preferences (optional)

### Python Services
- ✅ FastAPI: AI recommendation engine
- ✅ TensorFlow/PyTorch: ML model inference
- ✅ Pandas/NumPy: Data processing

### Infrastructure
- ✅ PostgreSQL: User data, podcast metadata
- ✅ Redis: Caching layer
- ✅ RabbitMQ: Event bus (for tracking interactions)

## 🚀 Deployment

### Docker Compose
```yaml
podcast-ai-service:
  build: ./src/PodcastRecommendationService/ai_service
  depends_on:
    - userservice-api
    - contentservice-api

podcastrecommendation-api:
  build: ./src/PodcastRecommendationService/PodcastRecommendationService.API
  depends_on:
    - podcast-ai-service
    - contentservice-api
```

## 🎯 Next Steps

1. ✅ **Phase 1 Complete**: FastAPI service with AI model working
2. 🔄 **Phase 2 Current**: C# integration with JWT authentication
3. 📋 **Phase 3 Next**: ContentService integration for full podcast entities
4. 🚀 **Phase 4 Future**: Caching, performance optimization, analytics
