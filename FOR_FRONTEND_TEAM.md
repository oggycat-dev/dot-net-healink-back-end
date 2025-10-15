# 🌐 API Endpoints - For Frontend Team

## 📋 Quick Start

### Method 1: Download from GitHub Actions (Easiest)

1. Go to [GitHub Actions](../../actions)
2. Click on latest "🚀 Deploy Healink - Free Tier" workflow run
3. Scroll to **Artifacts** section at bottom
4. Download `api-endpoints-{commit-sha}.zip`
5. Extract and copy `.env.production` to your project

### Method 2: Run Local Script

```bash
# In backend repository
./scripts/get-api-endpoints.sh

# Files will be generated in: api-endpoints/
```

### Method 3: Check Workflow Summary

1. Go to latest workflow run
2. Click **Summary** tab
3. Copy the Gateway URL and paste into your `.env`

---

## 🚀 Configuration Examples

### React / Create React App

```env
REACT_APP_API_URL=http://healink-gateway-fre-XXXXX.elb.amazonaws.com
REACT_APP_API_GATEWAY=http://healink-gateway-fre-XXXXX.elb.amazonaws.com
REACT_APP_AUTH_URL=http://healink-gateway-fre-XXXXX.elb.amazonaws.com/api/auth
```

**Usage in React:**
```javascript
import axios from 'axios';

const API_URL = process.env.REACT_APP_API_URL;

// Login
const login = async (email, password) => {
  const response = await axios.post(`${API_URL}/api/auth/login`, {
    email,
    password
  });
  return response.data;
};
```

### Next.js

```env
NEXT_PUBLIC_API_URL=http://healink-gateway-fre-XXXXX.elb.amazonaws.com
NEXT_PUBLIC_API_GATEWAY=http://healink-gateway-fre-XXXXX.elb.amazonaws.com
NEXT_PUBLIC_AUTH_URL=http://healink-gateway-fre-XXXXX.elb.amazonaws.com/api/auth
```

**Usage in Next.js:**
```typescript
// lib/api.ts
export const API_CONFIG = {
  baseUrl: process.env.NEXT_PUBLIC_API_URL!,
  auth: process.env.NEXT_PUBLIC_AUTH_URL!,
};

// Usage
import { API_CONFIG } from '@/lib/api';

fetch(`${API_CONFIG.baseUrl}/api/users`)
  .then(res => res.json())
  .then(data => console.log(data));
```

### Vue.js

```env
VUE_APP_API_URL=http://healink-gateway-fre-XXXXX.elb.amazonaws.com
VUE_APP_API_GATEWAY=http://healink-gateway-fre-XXXXX.elb.amazonaws.com
VUE_APP_AUTH_URL=http://healink-gateway-fre-XXXXX.elb.amazonaws.com/api/auth
```

**Usage in Vue:**
```javascript
// src/config/api.js
export default {
  baseURL: process.env.VUE_APP_API_URL,
  authURL: process.env.VUE_APP_AUTH_URL,
};

// Usage
import api from '@/config/api';
import axios from 'axios';

axios.get(`${api.baseURL}/api/users`)
  .then(response => console.log(response.data));
```

### TypeScript

Use the generated `api-config.ts`:

```typescript
/**
 * api-config.ts
 * Copy this to your project: src/config/api-config.ts
 */
export const API_CONFIG = {
  environment: 'free',
  gatewayUrl: 'http://healink-gateway-fre-XXXXX.elb.amazonaws.com',
  endpoints: {
    base: 'http://healink-gateway-fre-XXXXX.elb.amazonaws.com',
    health: 'http://healink-gateway-fre-XXXXX.elb.amazonaws.com/health',
    auth: 'http://healink-gateway-fre-XXXXX.elb.amazonaws.com/api/auth',
    users: 'http://healink-gateway-fre-XXXXX.elb.amazonaws.com/api/users',
    content: 'http://healink-gateway-fre-XXXXX.elb.amazonaws.com/api/content',
    notifications: 'http://healink-gateway-fre-XXXXX.elb.amazonaws.com/api/notifications',
    subscriptions: 'http://healink-gateway-fre-XXXXX.elb.amazonaws.com/api/subscriptions',
    payments: 'http://healink-gateway-fre-XXXXX.elb.amazonaws.com/api/payments',
    recommendations: 'http://healink-gateway-fre-XXXXX.elb.amazonaws.com/api/recommendations',
  },
} as const;
```

---

## 📋 Available Endpoints

All requests go through the **Gateway**. Internal services are not exposed to the internet.

| Service | Endpoint Path | Description |
|---------|---------------|-------------|
| Health Check | `/health` | API health status |
| Authentication | `/api/auth/*` | Login, register, refresh token |
| Users | `/api/users/*` | User management |
| Content | `/api/content/*` | Content & media |
| Notifications | `/api/notifications/*` | Push notifications |
| Subscriptions | `/api/subscriptions/*` | Subscription plans |
| Payments | `/api/payments/*` | Payment processing |
| Recommendations | `/api/recommendations/*` | AI podcast recommendations |

---

## 🧪 Testing the API

### Test Health Endpoint

```bash
# Replace with your actual Gateway URL
curl http://healink-gateway-fre-XXXXX.elb.amazonaws.com/health

# Expected response
{
  "status": "healthy",
  "timestamp": "2025-10-15T10:30:00Z"
}
```

### Test Authentication

```bash
# Login
curl -X POST http://healink-gateway-fre-XXXXX.elb.amazonaws.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'

# Expected response
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "123",
    "email": "user@example.com"
  }
}
```

---

## 🔐 CORS Configuration

The Gateway is configured to allow requests from:
- `http://localhost:3000` (React dev server)
- `http://localhost:8080` (Vue dev server)
- `http://localhost:5010` (Other dev servers)

For production, update `allowed_origins` in `terraform_healink/free-tier.tfvars`.

---

## 📊 API Response Format

All API responses follow this standard format:

### Success Response
```json
{
  "success": true,
  "data": {
    // Your data here
  },
  "message": "Operation successful"
}
```

### Error Response
```json
{
  "success": false,
  "error": {
    "code": "AUTH_001",
    "message": "Invalid credentials"
  }
}
```

---

## 🔄 When Gateway URL Changes

Gateway URL changes when:
- Backend is redeployed
- Infrastructure is destroyed and recreated

**To get new URL:**
1. Check latest GitHub Actions workflow run
2. Or run `./scripts/get-api-endpoints.sh` from backend repo
3. Update your `.env` file

---

## 💡 Tips for Frontend Development

### 1. Use Environment Variables
Never hardcode API URLs in your code.

✅ Good:
```javascript
const API_URL = process.env.REACT_APP_API_URL;
```

❌ Bad:
```javascript
const API_URL = 'http://healink-gateway-fre-123.elb.amazonaws.com';
```

### 2. Create API Service Layer
```javascript
// services/api.js
import axios from 'axios';

const api = axios.create({
  baseURL: process.env.REACT_APP_API_URL,
  timeout: 10000,
});

// Add auth token to all requests
api.interceptors.request.use(config => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default api;
```

### 3. Handle Errors Gracefully
```javascript
try {
  const response = await api.get('/api/users');
  setUsers(response.data.data);
} catch (error) {
  if (error.response) {
    // Server responded with error
    console.error('API Error:', error.response.data.error);
    setError(error.response.data.error.message);
  } else if (error.request) {
    // Request made but no response
    console.error('Network Error');
    setError('Unable to connect to server');
  }
}
```

---

## 📞 Need Help?

- Backend issues: Check CloudWatch logs
- API not responding: Verify Gateway URL is correct
- CORS errors: Check allowed origins configuration
- Auth errors: Ensure token is valid and not expired

**Contact Backend Team:**
- Check GitHub Issues
- Review API documentation in `/docs`
- Test endpoints using Postman collection in `/postman`

---

**Last Updated:** Auto-generated on each deployment  
**Environment:** free  
**Cost:** ~$60/month (AWS Free Tier optimized)

