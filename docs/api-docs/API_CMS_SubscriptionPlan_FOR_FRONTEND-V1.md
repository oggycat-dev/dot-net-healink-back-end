# 📡 CMS Subscription Plan API Documentation

> **API Reference for Managing Subscription Plans in CMS**  
> Base URL: `http://localhost:5010/api/cms/subscription-plans`

---

## 📋 Table of Contents
- [Authentication](#authentication)
- [API Endpoints](#api-endpoints)
  1. [Get All Subscription Plans](#1-get-all-subscription-plans)
  2. [Get Subscription Plan by ID](#2-get-subscription-plan-by-id)
  3. [Create Subscription Plan](#3-create-subscription-plan)
  4. [Update Subscription Plan](#4-update-subscription-plan)
  5. [Delete Subscription Plan](#5-delete-subscription-plan)
- [Common Response Format](#common-response-format)
- [Error Codes](#error-codes)
- [Validation Rules](#validation-rules)

---

## 🔐 Authentication

**All endpoints require authentication with Admin or Staff role.**

### Headers Required:
```http
Authorization: Bearer <access_token>
Content-Type: application/json
```

### How to get Access Token:
1. Login via `/api/cms/auth/login` 
2. Use returned `accessToken` in Authorization header

---

## 📡 API Endpoints

### 1. Get All Subscription Plans

**GET** `/api/cms/subscription-plans`

#### Purpose:
Lấy danh sách subscription plans với filter và pagination. Dùng để hiển thị table trong CMS.

#### Request Headers:
```http
Authorization: Bearer <access_token>
```

#### Query Parameters:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Page number (≥ 1) |
| `pageSize` | int | No | 10 | Items per page (1-100) |
| `searchTerm` | string | No | null | Search by name or displayName |
| `sortBy` | string | No | "createdAt" | Sort field: createdAt, amount, name |
| `sortDirection` | string | No | "desc" | Sort direction: asc, desc |
| `isActive` | bool | No | null | Filter active plans: true/false |
| `billingPeriodUnit` | int | No | null | Filter by billing period: 1 (Month), 2 (Year) |
| `minAmount` | decimal | No | null | Minimum price filter |
| `maxAmount` | decimal | No | null | Maximum price filter |
| `hasTrialPeriod` | bool | No | null | Filter plans with trial: true/false |
| `minTrialDays` | int | No | null | Minimum trial days filter |
| `currency` | string | No | null | Filter by currency: VND, USD |

#### Example Request (Postman):
```http
GET http://localhost:5010/api/cms/subscription-plans?pageNumber=1&pageSize=10&isActive=true&billingPeriodUnit=1&sortBy=amount&sortDirection=asc
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

#### Success Response (200 OK):
```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "basic",
        "displayName": "Basic Plan",
        "description": "Basic features for small creators",
        "isActive": true,
        "featureConfig": "{\"maxVideos\":10,\"maxStorage\":\"5GB\"}",
        "currency": "VND",
        "billingPeriodCount": 1,
        "billingPeriodUnit": 1,
        "billingPeriodUnitName": "Month",
        "amount": 99000,
        "trialDays": 7,
        "createdAt": "2025-10-01T10:00:00Z",
        "updatedAt": "2025-10-02T10:00:00Z"
      },
      {
        "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
        "name": "premium",
        "displayName": "Premium Plan",
        "description": "Advanced features for professional creators",
        "isActive": true,
        "featureConfig": "{\"maxVideos\":100,\"maxStorage\":\"50GB\"}",
        "currency": "VND",
        "billingPeriodCount": 1,
        "billingPeriodUnit": 1,
        "billingPeriodUnitName": "Month",
        "amount": 299000,
        "trialDays": 14,
        "createdAt": "2025-10-01T11:00:00Z",
        "updatedAt": null
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalRecords": 2,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "message": "Get subscription plans successfully",
  "statusCode": 200
}
```

#### Error Responses:

**401 Unauthorized** - Missing or invalid token:
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Unauthorized",
  "statusCode": 401
}
```

**403 Forbidden** - User is not Admin or Staff:
```json
{
  "isSuccess": false,
  "data": null,
  "message": "You do not have permission to access this resource",
  "statusCode": 403
}
```

---

### 2. Get Subscription Plan by ID

**GET** `/api/cms/subscription-plans/{id}`

#### Purpose:
Lấy chi tiết 1 subscription plan theo ID. Dùng để hiển thị detail page hoặc edit form.

#### Request Headers:
```http
Authorization: Bearer <access_token>
```

#### Path Parameters:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Subscription plan ID |

#### Example Request (Postman):
```http
GET http://localhost:5010/api/cms/subscription-plans/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

#### Success Response (200 OK):
```json
{
  "isSuccess": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "basic",
    "displayName": "Basic Plan",
    "description": "Basic features for small creators",
    "isActive": true,
    "featureConfig": "{\"maxVideos\":10,\"maxStorage\":\"5GB\"}",
    "currency": "VND",
    "billingPeriodCount": 1,
    "billingPeriodUnit": 1,
    "billingPeriodUnitName": "Month",
    "amount": 99000,
    "trialDays": 7,
    "createdAt": "2025-10-01T10:00:00Z",
    "updatedAt": "2025-10-02T10:00:00Z"
  },
  "message": "Get subscription plan successfully",
  "statusCode": 200
}
```

#### Error Responses:

**404 Not Found** - Plan not found:
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Subscription plan not found",
  "statusCode": 404
}
```

---

### 3. Create Subscription Plan

**POST** `/api/cms/subscription-plans`

#### Purpose:
Tạo subscription plan mới. Admin/Staff tạo các gói subscription cho creators.

#### Request Headers:
```http
Authorization: Bearer <access_token>
Content-Type: application/json
```

#### Request Body:
```json
{
  "name": "basic",
  "displayName": "Basic Plan",
  "description": "Basic features for small creators",
  "featureConfig": "{\"maxVideos\":10,\"maxStorage\":\"5GB\",\"canLivestream\":false}",
  "currency": "VND",
  "billingPeriodCount": 1,
  "billingPeriodUnit": 1,
  "amount": 99000,
  "trialDays": 7,
  "status": 1
}
```

#### Field Descriptions:
| Field | Type | Required | Description | Example |
|-------|------|----------|-------------|---------|
| `name` | string | Yes | Plan identifier (unique, lowercase, no spaces) | "basic", "premium" |
| `displayName` | string | Yes | Plan display name | "Basic Plan" |
| `description` | string | Yes | Plan description | "Basic features..." |
| `featureConfig` | string (JSON) | No | Feature configuration in JSON format | See below |
| `currency` | string | Yes | Currency code (3 chars) | "VND", "USD" |
| `billingPeriodCount` | int | Yes | Billing period count (> 0) | 1, 3, 6, 12 |
| `billingPeriodUnit` | int | Yes | 1 = Month, 2 = Year | 1 or 2 |
| `amount` | decimal | Yes | Plan price (≥ 0) | 99000, 299000 |
| `trialDays` | int | Yes | Trial period days (0-90) | 0, 7, 14, 30 |
| `status` | int | Yes | 1 = Active, 2 = Inactive | 1 or 2 |

#### Feature Config Examples:
```json
// Basic Plan
{
  "maxVideos": 10,
  "maxStorage": "5GB",
  "canLivestream": false,
  "canMonetize": false,
  "analyticsAccess": "basic"
}

// Premium Plan
{
  "maxVideos": 100,
  "maxStorage": "50GB",
  "canLivestream": true,
  "canMonetize": true,
  "analyticsAccess": "advanced",
  "prioritySupport": true
}

// Enterprise Plan
{
  "maxVideos": -1,
  "maxStorage": "unlimited",
  "canLivestream": true,
  "canMonetize": true,
  "analyticsAccess": "full",
  "prioritySupport": true,
  "dedicatedAccount": true,
  "customBranding": true
}
```

#### Example Request (Postman):
```http
POST http://localhost:5010/api/cms/subscription-plans
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "name": "basic",
  "displayName": "Basic Plan",
  "description": "Perfect for individual creators just starting out",
  "featureConfig": "{\"maxVideos\":10,\"maxStorage\":\"5GB\",\"canLivestream\":false}",
  "currency": "VND",
  "billingPeriodCount": 1,
  "billingPeriodUnit": 1,
  "amount": 99000,
  "trialDays": 7,
  "status": 1
}
```

#### Success Response (201 Created):
```json
{
  "isSuccess": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "basic",
    "displayName": "Basic Plan",
    "description": "Perfect for individual creators just starting out",
    "isActive": true,
    "featureConfig": "{\"maxVideos\":10,\"maxStorage\":\"5GB\",\"canLivestream\":false}",
    "currency": "VND",
    "billingPeriodCount": 1,
    "billingPeriodUnit": 1,
    "billingPeriodUnitName": "Month",
    "amount": 99000,
    "trialDays": 7,
    "createdAt": "2025-10-02T10:00:00Z",
    "updatedAt": null
  },
  "message": "Create subscription plan successfully",
  "statusCode": 201
}
```

#### Error Responses:

**400 Bad Request** - Validation error:
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Validation failed",
  "errors": {
    "Name": ["Name is required"],
    "Amount": ["Amount must be greater than or equal to 0"],
    "BillingPeriodUnit": ["Billing period unit must be 1 (Month) or 2 (Year)"]
  },
  "statusCode": 400
}
```

**409 Conflict** - Plan name already exists:
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Subscription plan with name 'basic' already exists",
  "statusCode": 409
}
```

---

### 4. Update Subscription Plan

**PUT** `/api/cms/subscription-plans/{id}`

#### Purpose:
Cập nhật thông tin subscription plan. Hỗ trợ partial update (chỉ cần gửi fields cần update).

#### Request Headers:
```http
Authorization: Bearer <access_token>
Content-Type: application/json
```

#### Path Parameters:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Subscription plan ID to update |

#### Request Body (Full Update):
```json
{
  "name": "basic-updated",
  "displayName": "Basic Plan - Updated",
  "description": "Updated description with new features",
  "featureConfig": "{\"maxVideos\":15,\"maxStorage\":\"10GB\",\"canLivestream\":false}",
  "currency": "VND",
  "billingPeriodCount": 1,
  "billingPeriodUnit": 1,
  "amount": 119000,
  "trialDays": 14,
  "status": 1
}
```

#### Request Body (Partial Update - Only Price):
```json
{
  "name": "basic",
  "displayName": "Basic Plan",
  "description": "Basic features for small creators",
  "currency": "VND",
  "billingPeriodCount": 1,
  "billingPeriodUnit": 1,
  "amount": 89000,
  "trialDays": 7,
  "status": 1
}
```

#### Example Request (Postman):
```http
PUT http://localhost:5010/api/cms/subscription-plans/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "name": "basic",
  "displayName": "Basic Plan - Special Offer",
  "description": "Basic features for small creators with special pricing",
  "featureConfig": "{\"maxVideos\":10,\"maxStorage\":\"5GB\",\"canLivestream\":false}",
  "currency": "VND",
  "billingPeriodCount": 1,
  "billingPeriodUnit": 1,
  "amount": 79000,
  "trialDays": 14,
  "status": 1
}
```

#### Success Response (200 OK):
```json
{
  "isSuccess": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "basic",
    "displayName": "Basic Plan - Special Offer",
    "description": "Basic features for small creators with special pricing",
    "isActive": true,
    "featureConfig": "{\"maxVideos\":10,\"maxStorage\":\"5GB\",\"canLivestream\":false}",
    "currency": "VND",
    "billingPeriodCount": 1,
    "billingPeriodUnit": 1,
    "billingPeriodUnitName": "Month",
    "amount": 79000,
    "trialDays": 14,
    "createdAt": "2025-10-01T10:00:00Z",
    "updatedAt": "2025-10-02T15:30:00Z"
  },
  "message": "Update subscription plan successfully",
  "statusCode": 200
}
```

#### Error Responses:

**400 Bad Request** - Validation error:
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Validation failed",
  "errors": {
    "TrialDays": ["Trial days must not exceed 90 days"]
  },
  "statusCode": 400
}
```

**404 Not Found** - Plan not found:
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Subscription plan not found",
  "statusCode": 404
}
```

---

### 5. Delete Subscription Plan

**DELETE** `/api/cms/subscription-plans/{id}`

#### Purpose:
Xóa (soft delete) subscription plan. Plan sẽ bị set `IsDeleted = true`, không thực sự xóa khỏi DB.

**⚠️ IMPORTANT:** Không thể xóa plan đang có active subscriptions.

#### Request Headers:
```http
Authorization: Bearer <access_token>
```

#### Path Parameters:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | Guid | Yes | Subscription plan ID to delete |

#### Example Request (Postman):
```http
DELETE http://localhost:5010/api/cms/subscription-plans/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

#### Success Response (200 OK):
```json
{
  "isSuccess": true,
  "data": null,
  "message": "Delete subscription plan successfully",
  "statusCode": 200
}
```

#### Error Responses:

**400 Bad Request** - Plan has active subscriptions:
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Cannot delete subscription plan because it has 5 active subscriptions",
  "statusCode": 400
}
```

**404 Not Found** - Plan not found:
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Subscription plan not found",
  "statusCode": 404
}
```

---

## 📦 Common Response Format

### Success Response Structure:
```json
{
  "isSuccess": true,
  "data": { /* Response data */ },
  "message": "Success message",
  "statusCode": 200
}
```

### Error Response Structure:
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Error message",
  "errors": { /* Validation errors (optional) */ },
  "statusCode": 400
}
```

### Pagination Response Structure:
```json
{
  "isSuccess": true,
  "data": {
    "items": [ /* Array of items */ ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalRecords": 25,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "message": "Success message",
  "statusCode": 200
}
```

---

## ⚠️ Error Codes

| Status Code | Description | Common Causes |
|-------------|-------------|---------------|
| 400 | Bad Request | Validation failed, invalid data format |
| 401 | Unauthorized | Missing or invalid access token |
| 403 | Forbidden | User lacks required role (Admin/Staff) |
| 404 | Not Found | Plan ID doesn't exist |
| 409 | Conflict | Plan name already exists |
| 500 | Internal Server Error | Server-side error (contact backend team) |

---

## ✅ Validation Rules

### Field Validation:

#### `name`:
- ✅ Required
- ✅ Max length: 100 characters
- ✅ Must be unique (case-insensitive)
- ✅ Best practice: lowercase, no spaces (e.g., "basic", "premium")

#### `displayName`:
- ✅ Required
- ✅ Max length: 200 characters
- ✅ Can contain spaces and special characters

#### `description`:
- ✅ Required
- ✅ Max length: 1000 characters

#### `amount`:
- ✅ Required
- ✅ Must be ≥ 0
- ✅ Can be 0 for free plans

#### `currency`:
- ✅ Required
- ✅ Must be exactly 3 characters
- ✅ Examples: VND, USD, EUR

#### `billingPeriodCount`:
- ✅ Required
- ✅ Must be > 0
- ✅ Examples: 1 (monthly), 3 (quarterly), 6 (semi-annual), 12 (yearly)

#### `billingPeriodUnit`:
- ✅ Required
- ✅ Must be 1 (Month) or 2 (Year)
- ❌ Invalid: 0, 3, -1

#### `trialDays`:
- ✅ Required
- ✅ Must be 0-90
- ✅ Use 0 for no trial period

#### `featureConfig`:
- ⚪ Optional
- ✅ Must be valid JSON if provided
- ✅ Will be stored as string in DB

#### `status`:
- ✅ Required
- ✅ Must be 1 (Active) or 2 (Inactive)

---

## 🔍 Common Use Cases

### Use Case 1: Display All Plans in CMS Table
```http
GET /api/cms/subscription-plans?pageNumber=1&pageSize=20&sortBy=amount&sortDirection=asc
```

### Use Case 2: Search Plans by Name
```http
GET /api/cms/subscription-plans?searchTerm=premium&pageSize=10
```

### Use Case 3: Filter Active Monthly Plans
```http
GET /api/cms/subscription-plans?isActive=true&billingPeriodUnit=1&pageSize=50
```

### Use Case 4: Create Free Trial Plan
```json
POST /api/cms/subscription-plans
{
  "name": "free-trial",
  "displayName": "Free Trial",
  "description": "7-day free trial with basic features",
  "currency": "VND",
  "billingPeriodCount": 1,
  "billingPeriodUnit": 1,
  "amount": 0,
  "trialDays": 7,
  "status": 1
}
```

### Use Case 5: Update Plan Price (Special Promotion)
```json
PUT /api/cms/subscription-plans/{id}
{
  "name": "premium",
  "displayName": "Premium Plan - Black Friday Sale",
  "description": "50% off for Black Friday",
  "currency": "VND",
  "billingPeriodCount": 1,
  "billingPeriodUnit": 1,
  "amount": 149500,
  "trialDays": 14,
  "status": 1
}
```

---

## 📝 Testing Checklist for Frontend

### ✅ Authentication:
- [ ] Test with valid Admin token → Should work
- [ ] Test with valid Staff token → Should work
- [ ] Test with Creator token → Should get 403 Forbidden
- [ ] Test without token → Should get 401 Unauthorized
- [ ] Test with expired token → Should get 401 Unauthorized

### ✅ GET All Plans:
- [ ] Test pagination (page 1, 2, 3)
- [ ] Test page size limits (1, 10, 50, 100)
- [ ] Test search by name
- [ ] Test filter by isActive
- [ ] Test filter by billingPeriodUnit
- [ ] Test sorting (by amount asc/desc, by name, by date)
- [ ] Test empty results

### ✅ GET Plan by ID:
- [ ] Test with valid ID → Get 200
- [ ] Test with invalid ID → Get 404
- [ ] Test with deleted plan ID → Get 404

### ✅ CREATE Plan:
- [ ] Test with all required fields → Get 201
- [ ] Test with missing required fields → Get 400
- [ ] Test with duplicate name → Get 409
- [ ] Test with invalid JSON in featureConfig → Get 400
- [ ] Test with negative amount → Get 400
- [ ] Test with invalid billingPeriodUnit (0, 3) → Get 400
- [ ] Test with trialDays > 90 → Get 400

### ✅ UPDATE Plan:
- [ ] Test full update → Get 200
- [ ] Test partial update (only price) → Get 200
- [ ] Test with invalid ID → Get 404
- [ ] Test with validation errors → Get 400

### ✅ DELETE Plan:
- [ ] Test delete unused plan → Get 200
- [ ] Test delete plan with active subscriptions → Get 400
- [ ] Test delete non-existent plan → Get 404

---

## 🎯 Best Practices for Frontend

1. **Always check `isSuccess` field** before accessing `data`
2. **Handle all error status codes** (400, 401, 403, 404, 409, 500)
3. **Display validation errors** from `errors` object
4. **Show loading states** while waiting for API response
5. **Implement retry logic** for 500 errors
6. **Cache access token** and refresh when expired
7. **Use pagination** for large datasets
8. **Debounce search input** to reduce API calls
9. **Validate input client-side** before sending to API
10. **Log API errors** to monitoring service (Sentry, LogRocket)

---

## 📞 Support

**Backend Team Contact:**
- API Gateway: `http://localhost:5010`
- Subscription Service: `http://localhost:5004`
- Issue Tracker: GitHub Issues

**Last Updated:** October 2, 2025  
**Version:** 1.0  
**Maintained by:** Backend Team
