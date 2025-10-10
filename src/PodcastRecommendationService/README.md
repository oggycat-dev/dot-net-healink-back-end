# 🎯 Podcast Recommendation Service - Complete Setup Guide

## 📋 Overview

Service gợi ý podcast sử dụng AI model được train từ Kaggle, tích hợp với microservices architecture.

### Luồng nghiệp vụ hoàn chỉnh:

```
User với JWT Token
    ↓
[RecommendationsController.cs] - Extract UserId từ JWT
    ↓
[FastAPIRecommendationService.cs] - Gọi FastAPI
    ↓
[FastAPI Service (Python)] - Load AI model & calculate recommendations
    ↓
[ContentService] - Lấy podcast entities (optional enrichment)
    ↓
Response: Top 5-10 podcasts với predicted ratings
```

## 🔧 Configuration (từ .env)

Tất cả configuration đọc từ file `.env`:

```bash
# Service URLs
USER_SERVICE_URL=http://userservice-api
CONTENT_SERVICE_URL=http://contentservice-api
PODCAST_AI_SERVICE_URL=http://podcast-ai-service:8000

# Recommendation Settings
RECOMMENDATION_AI_SERVICE_BASE_URL=http://podcast-ai-service:8000
RECOMMENDATION_AI_SERVICE_TIMEOUT_SECONDS=30
RECOMMENDATION_CONTENT_SERVICE_URL=http://contentservice-api
RECOMMENDATION_USER_SERVICE_URL=http://userservice-api
RECOMMENDATION_DEFAULT_LIMIT=10
RECOMMENDATION_MAX_LIMIT=50
RECOMMENDATION_CACHE_EXPIRATION_MINUTES=60
RECOMMENDATION_ENABLE_CACHING=true

# FastAPI Python Service
PYTHON_UNBUFFERED=1
MODEL_PATH=/app/models
MODEL_RELOAD_ON_STARTUP=false

# JWT (required for authentication)
JWT_SECRET_KEY=your-secret-key
JWT_ISSUER=Healink
JWT_AUDIENCE=Healink.Users
```

**❌ KHÔNG SỬ DỤNG appsettings.json** - Tất cả settings từ environment variables

## 📦 Required Files

### 1. AI Model Files (từ Kaggle training)

Đặt trong: `src/PodcastRecommendationService/ai_service/models/`

```
models/
├── collaborative_filtering_model.h5  # TensorFlow model (optional)
├── mappings.pkl                      # User/Podcast ID mappings (REQUIRED)
├── podcasts.pkl                      # Training podcasts data
└── model_metadata.json              # Model performance metrics
```

**Nếu chưa có model files**: Service vẫn chạy được nhưng sẽ generate random recommendations dựa trên logic collaborative filtering.

## 🚀 Quick Start

### 1. Build Services

```bash
cd /Users/tuandat/Documents/WorkSpace/dot-net-healink-back-end

# Build cả 2 services
docker-compose build podcast-ai-service podcastrecommendation-api
```

### 2. Start FastAPI Service (Python AI Engine)

```bash
# Start chỉ FastAPI service để test
docker-compose up -d podcast-ai-service

# Kiểm tra health
curl http://localhost:8000/health

# Kiểm tra model info
curl http://localhost:8000/model/info
```

### 3. Start C# API Service

```bash
# Start C# service (cần các dependencies)
docker-compose up -d podcastrecommendation-api

# Hoặc start toàn bộ system
docker-compose up -d
```

### 4. Test Integration

```bash
# Chạy test script
./test-recommendation-service.sh
```

## 🧪 Testing Flow

### Test 1: FastAPI Direct (Không cần JWT)

```bash
# Health check
curl http://localhost:8000/health

# Get recommendations cho user
curl "http://localhost:8000/recommendations/user_123?num_recommendations=5"

# Response:
{
  "user_id": "user_123",
  "recommendations": [
    {
      "podcast_id": "p_00189",
      "title": "Bài học 189: Quản lý thời gian hiệu quả",
      "predicted_rating": 4.5,
      "category": "Career",
      "topics": "Quản lý thời gian",
      "duration_minutes": 20
    }
  ],
  "total_count": 5,
  "timestamp": "2025-10-10T07:00:00Z"
}
```

### Test 2: C# API với JWT Token

```bash
# 1. Lấy JWT token từ AuthService
TOKEN=$(curl -X POST http://localhost:8080/api/auth/user/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password123"}' \
  | jq -r '.data.accessToken')

# 2. Gọi recommendation endpoint
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5005/api/recommendations/me?limit=10"

# Response:
{
  "isSuccess": true,
  "data": {
    "userId": "user-guid-from-jwt",
    "recommendations": [...],
    "totalFound": 10,
    "filteredListened": false,
    "generatedAt": "2025-10-10T07:00:00Z"
  },
  "message": "Recommendations retrieved successfully"
}
```

### Test 3: Swagger UI

Mở browser: http://localhost:5005/swagger

1. Click "Authorize" button
2. Nhập JWT token: `Bearer {your-token}`
3. Test endpoint `/api/recommendations/me`

## 📊 API Endpoints

### FastAPI Service (Python - Port 8000)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Health check |
| GET | `/model/info` | Model metadata & statistics |
| GET | `/recommendations/{user_id}` | Get recommendations for user |
| POST | `/recommendations` | Get recommendations (POST body) |
| GET | `/users/real` | Get real users from UserService |
| GET | `/podcasts/real` | Get real podcasts from ContentService |

### C# API Service (Port 5005)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/recommendations/me` | ✅ Required | Get recommendations for current user (from JWT) |
| GET | `/api/recommendations/user/{userId}` | ✅ Admin only | Get recommendations for specific user |
| GET | `/api/recommendations/batch` | ✅ Admin only | Batch recommendations for multiple users |
| POST | `/api/recommendations/interaction` | ✅ Required | Track user interaction with recommended podcast |

## 🔐 Authentication Flow

### 1. User Login → Get JWT

```bash
curl -X POST http://localhost:8080/api/auth/user/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'
```

Response:
```json
{
  "isSuccess": true,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "...",
    "expiresIn": 3600
  }
}
```

### 2. JWT Token Structure

JWT contains:
```json
{
  "sub": "user-guid",           // UserId
  "email": "user@example.com",
  "roles": ["User"],
  "exp": 1696900000
}
```

### 3. Extract UserId from JWT (Auto)

Controller tự động extract:
```csharp
var userId = _currentUserService.UserId;  // From JWT token
```

## 🏗️ Architecture Components

### 1. C# Services Layer

```
PodcastRecommendationService.API/
├── Controllers/
│   └── RecommendationsController.cs    # API endpoints với JWT auth
├── Program.cs                          # Entry point, load .env
└── Configurations/
    └── ServiceConfiguration.cs         # DI setup

PodcastRecommendationService.Infrastructure/
├── Services/
│   ├── FastAPIRecommendationService.cs  # HTTP client gọi FastAPI
│   ├── RecommendationService.cs         # Business logic wrapper
│   └── DataFetchService.cs             # External service calls
```

### 2. FastAPI Service (Python)

```
ai_service/
├── fastapi_service.py       # Main FastAPI app
├── Dockerfile_fastapi       # Container config
├── requirements_fastapi.txt # Python dependencies
└── models/                  # AI model files
    ├── mappings.pkl
    ├── podcasts.pkl
    └── model_metadata.json
```

### 3. Environment Variables Flow

```
.env file
    ↓
Docker Compose (environment section)
    ↓
Container Environment Variables
    ↓
C# Configuration (IConfiguration)
    ↓
Services (via DI)
```

## 🔍 Troubleshooting

### Issue 1: FastAPI không trả về recommendations

**Nguyên nhân**: Không có podcasts data từ ContentService

**Giải pháp**:
```bash
# Check ContentService
curl http://localhost:8082/api/podcasts

# Nếu không có data, seed database:
docker-compose exec contentservice-api dotnet ef database update
```

### Issue 2: C# service không connect được FastAPI

**Nguyên nhân**: Service URLs không đúng

**Kiểm tra**:
```bash
# Check environment variables
docker-compose exec podcastrecommendation-api env | grep RECOMMENDATION

# Should see:
# RECOMMENDATION_AI_SERVICE_BASE_URL=http://podcast-ai-service:8000
```

**Giải pháp**: Cập nhật `.env` file và rebuild

### Issue 3: JWT authentication failed

**Nguyên nhân**: JWT_SECRET_KEY không khớp giữa services

**Kiểm tra**:
```bash
# Check JWT settings trong .env
cat .env | grep JWT_SECRET_KEY
```

**Giải pháp**: Đảm bảo tất cả services dùng cùng JWT_SECRET_KEY

### Issue 4: Port already in use

**Nguyên nhân**: Container cũ vẫn đang chạy

**Giải pháp**:
```bash
# Stop all old containers
docker ps | grep podcast
docker stop <container-id>
docker rm <container-id>

# Hoặc cleanup toàn bộ
docker-compose down
docker-compose up -d
```

## 📈 Performance & Optimization

### Caching Strategy

**Recommendations cache**: 60 phút (configurable)
```bash
RECOMMENDATION_CACHE_EXPIRATION_MINUTES=60
RECOMMENDATION_ENABLE_CACHING=true
```

**Model cache**: Load once khi start, keep in memory

### Timeouts

```bash
# FastAPI timeout
RECOMMENDATION_AI_SERVICE_TIMEOUT_SECONDS=30

# External service timeouts
UserService: 10s
ContentService: 10s
```

## 🎯 Production Checklist

- [ ] Model files đã copy vào `ai_service/models/`
- [ ] `.env` file đã configure đầy đủ
- [ ] JWT_SECRET_KEY đã set và giống nhau across services
- [ ] Database đã seed podcasts data
- [ ] All services health checks passing
- [ ] Test recommendations endpoint với real JWT token
- [ ] Monitor logs for errors
- [ ] Setup Redis caching (if enabled)
- [ ] Configure rate limiting
- [ ] Enable monitoring & alerting

## 🔗 Related Documentation

- [BUSINESS_FLOW.md](./BUSINESS_FLOW.md) - Detailed business logic
- [CLEANUP_SUMMARY.md](../../CLEANUP_SUMMARY.md) - Project structure
- [docker-compose.yml](../../docker-compose.yml) - Service orchestration
- [.env.example](../../.env.example) - Environment template

## 💡 Tips & Best Practices

1. **Always use .env for configuration** - Không hardcode URLs
2. **Test FastAPI separately first** - Đảm bảo AI engine work trước
3. **Use JWT from AuthService** - Không tự tạo JWT
4. **Monitor logs** - `docker-compose logs -f podcast-ai-service`
5. **Check health endpoints** - Before debugging integration
6. **Start services in order**: postgres → redis → rabbitmq → auth → user → content → podcast-ai → podcastrecommendation

## 🎉 Success Indicators

Khi system chạy đúng, bạn sẽ thấy:

✅ FastAPI health returns `{"status":"healthy","model_loaded":true}`
✅ C# API health returns `200 OK`
✅ Swagger UI accessible at `http://localhost:5005/swagger`
✅ Recommendations endpoint returns Vietnamese podcast titles
✅ JWT authentication working
✅ UserId extracted from token correctly
✅ Predictions có giá trị hợp lý (1.0 - 5.0)

---

**Ready to run!** 🚀

Bất kỳ vấn đề gì, check logs:
```bash
docker-compose logs -f podcast-ai-service
docker-compose logs -f podcastrecommendation-api
```
