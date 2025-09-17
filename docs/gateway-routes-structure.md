# Gateway Routes Structure

## Overview
Cấu hình routing gateway được tổ chức thành 2 nhóm chính để phân biệt rõ ràng giữa CMS (admin/staff) và User (end-user) interfaces.

## Route Groups

### 1. CMS Routes (Admin/Staff Only)
Các routes dành cho admin panel và staff management.

**Base Path**: `/api/cms/`

#### Authentication Service
- `POST /api/cms/auth/login` - Public (Login)
- `* /api/cms/auth/*` - Authenticated (Logout, refresh token, etc.)

#### User Management Service  
- `* /api/cms/users/*` - Authenticated (CRUD operations for user management)

#### Product Management Service (Future)
- `* /api/cms/products/*` - Authenticated (CRUD operations for product management)
- `* /api/cms/categories/*` - Authenticated (CRUD operations for category management)

**Authentication**: All CMS routes (except login) require Bearer token authentication.

### 2. User Routes (End Users)
Các routes dành cho end-user facing applications.

**Base Path**: `/api/user/`

#### Authentication Service
- `POST /api/user/auth/register` - Public (User registration)
- `POST /api/user/auth/login` - Public (User login)
- `* /api/user/auth/*` - Authenticated (Profile management, password change, etc.)

#### User Profile Service
- `* /api/user/profile/*` - Authenticated (User profile CRUD operations)

#### Product Browse Service (Future)
- `GET /api/user/products/*` - Public (Product browsing, search)
- `GET /api/user/categories/*` - Public (Category browsing)

**Authentication**: 
- Public routes: registration, login, product browsing
- Authenticated routes: profile management, advanced features

### 3. Health Check Routes
Monitoring và health check endpoints.

- `GET /api/health/auth` - Auth Service health check
- `GET /api/health/users` - User Service health check  
- `GET /api/health/products` - Product Service health check (Future)

## Service Mapping

| Service | Container Name | Port |
|---------|---------------|------|
| AuthService | authservice-api | 80 |
| UserService | userservice-api | 80 |
| ProductService | productservice-api | 80 |

## Adding New Services

Khi thêm service mới, hãy tuân theo pattern này:

### CMS Routes (Admin)
```json
{
  "DownstreamPathTemplate": "/api/cms/{servicename}/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [
    {
      "Host": "{servicename}-api",
      "Port": 80
    }
  ],
  "UpstreamPathTemplate": "/api/cms/{servicename}/{everything}",
  "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer",
    "AllowedScopes": []
  }
}
```

### User Routes (End Users)
```json
{
  "DownstreamPathTemplate": "/api/user/{servicename}/{everything}",
  "DownstreamScheme": "http", 
  "DownstreamHostAndPorts": [
    {
      "Host": "{servicename}-api",
      "Port": 80
    }
  ],
  "UpstreamPathTemplate": "/api/user/{servicename}/{everything}",
  "UpstreamHttpMethod": [ "GET" ]
}
```

**Note**: Thêm `AuthenticationOptions` nếu cần authentication.

### Health Check Route
```json
{
  "DownstreamPathTemplate": "/health",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [
    {
      "Host": "{servicename}-api", 
      "Port": 80
    }
  ],
  "UpstreamPathTemplate": "/api/health/{servicename}",
  "UpstreamHttpMethod": [ "GET" ]
}
```

## Route Ordering
Ocelot sử dụng first-match wins, nên thứ tự routes quan trọng:

1. **Specific routes trước** (ví dụ: `/api/cms/auth/login`)
2. **Wildcard routes sau** (ví dụ: `/api/cms/auth/{everything}`)
3. **Health checks cuối cùng**

## Security Considerations

- **CMS routes**: Tất cả đều cần authentication trừ login
- **User routes**: Phân biệt public và authenticated endpoints
- **Health checks**: Không cần authentication (internal monitoring)
- **Bearer tokens**: Sử dụng JWT với key "Bearer"

## Testing Routes

### CMS Routes
```bash
# Login (Public)
curl -X POST http://localhost:5000/api/cms/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin@123","grant_type":0}'

# User Management (Authenticated)  
curl -X GET http://localhost:5000/api/cms/users \
  -H "Authorization: Bearer {token}"
```

### User Routes
```bash
# Registration (Public)
curl -X POST http://localhost:5000/api/user/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"User@123"}'

# Profile (Authenticated)
curl -X GET http://localhost:5000/api/user/profile \
  -H "Authorization: Bearer {token}"
```

### Health Checks
```bash
curl -X GET http://localhost:5000/api/health/auth
curl -X GET http://localhost:5000/api/health/users
```
