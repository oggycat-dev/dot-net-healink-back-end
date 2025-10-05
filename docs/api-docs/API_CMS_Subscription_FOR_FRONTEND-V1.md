# API Documentation: CMS Subscription Management (v1.0)

> **Mục đích**: Tài liệu API chi tiết cho Frontend Developer để implement các chức năng quản lý user subscriptions trong CMS
> 
> **Base URL**: `https://your-domain.com/api/cms/subscriptions`
> 
> **Authentication**: Yêu cầu Bearer Token với role `Admin` hoặc `Staff`

---

## 📋 Table of Contents
1. [Authentication & Authorization](#authentication--authorization)
2. [API Endpoints Overview](#api-endpoints-overview)
3. [Data Models](#data-models)
4. [API Details](#api-details)
   - [GET /api/cms/subscriptions](#1-get-all-subscriptions)
   - [GET /api/cms/subscriptions/{id}](#2-get-subscription-by-id)
   - [PUT /api/cms/subscriptions/{id}](#3-update-subscription)
   - [POST /api/cms/subscriptions/{id}/cancel](#4-cancel-subscription)
5. [Error Handling](#error-handling)
6. [Testing Checklist](#testing-checklist)

---

## 🔐 Authentication & Authorization

### Headers Required
```http
Authorization: Bearer {your_jwt_token}
Content-Type: application/json
```

### Roles Required
- **Admin**: Full access to all subscription operations
- **Staff**: Full access to all subscription operations

### Error Response (401 Unauthorized)
```json
{
  "isSuccess": false,
  "message": "Unauthorized access",
  "errorCode": 401
}
```

### Error Response (403 Forbidden)
```json
{
  "isSuccess": false,
  "message": "You do not have permission to access this resource",
  "errorCode": 403
}
```

---

## 📊 API Endpoints Overview

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/cms/subscriptions` | Lấy danh sách subscriptions với filter & pagination | Admin, Staff |
| GET | `/api/cms/subscriptions/{id}` | Lấy chi tiết 1 subscription theo ID | Admin, Staff |
| PUT | `/api/cms/subscriptions/{id}` | Cập nhật subscription settings | Admin, Staff |
| POST | `/api/cms/subscriptions/{id}/cancel` | Hủy subscription | Admin, Staff |

---

## 📦 Data Models

### SubscriptionResponse
Response model cho tất cả các API trả về subscription data.

```typescript
interface SubscriptionResponse {
  id: string;                      // GUID - Subscription ID
  userProfileId: string;           // GUID - User profile ID
  subscriptionPlanId: string;      // GUID - Subscription plan ID
  planName: string;                // Plan code name (e.g., "premium-monthly")
  planDisplayName: string;         // Plan display name (e.g., "Premium Monthly")
  subscriptionStatus: number;      // Enum: 1=InTrial, 2=Active, 3=PastDue, 4=Canceled, 5=Paused
  subscriptionStatusName: string;  // Human-readable status name
  currentPeriodStart: string | null;  // ISO 8601 date - Start of current billing period
  currentPeriodEnd: string | null;    // ISO 8601 date - End of current billing period
  cancelAt: string | null;         // ISO 8601 date - Scheduled cancellation date
  canceledAt: string | null;       // ISO 8601 date - Actual cancellation timestamp
  cancelAtPeriodEnd: boolean;      // True if scheduled to cancel at period end
  renewalBehavior: number;         // Enum: 1=AutoRenew, 2=Manual
  renewalBehaviorName: string;     // Human-readable renewal behavior
  createdAt: string;               // ISO 8601 date - Creation timestamp
  updatedAt: string | null;        // ISO 8601 date - Last update timestamp
}
```

### SubscriptionFilter (Query Parameters)
Filter parameters cho GET list API.

```typescript
interface SubscriptionFilter {
  // Pagination
  pageNumber?: number;             // Default: 1
  pageSize?: number;               // Default: 10, Max: 100
  
  // Sorting
  sortBy?: string;                 // Field name to sort by (e.g., "createdAt")
  sortOrder?: 'asc' | 'desc';      // Default: 'desc'
  
  // Filters
  userProfileId?: string;          // GUID - Filter by user
  subscriptionPlanId?: string;     // GUID - Filter by plan
  subscriptionStatus?: number;     // Filter by status (1-5)
  renewalBehavior?: number;        // Filter by renewal (1-2)
  isActive?: boolean;              // Filter active subscriptions only
  hasCancelScheduled?: boolean;    // Filter subscriptions with scheduled cancellation
  startDate?: string;              // ISO 8601 date - Filter by period start >= this date
  endDate?: string;                // ISO 8601 date - Filter by period end <= this date
  
  // Search
  keyword?: string;                // Search in plan name or display name
}
```

### UpdateSubscriptionRequest
Request body cho PUT update API.

```typescript
interface UpdateSubscriptionRequest {
  subscriptionStatus?: number;     // Optional: 1=InTrial, 2=Active, 3=PastDue, 4=Canceled, 5=Paused
  renewalBehavior?: number;        // Optional: 1=AutoRenew, 2=Manual
  cancelAtPeriodEnd?: boolean;     // Optional: Schedule cancellation flag
  currentPeriodStart?: string;     // Optional: ISO 8601 date
  currentPeriodEnd?: string;       // Optional: ISO 8601 date (must be future)
  cancelAt?: string;               // Optional: ISO 8601 date (must be future)
}
```

### Enums Reference

#### SubscriptionStatus
```typescript
enum SubscriptionStatus {
  InTrial = 1,    // Đang trong trial period
  Active = 2,     // Đang active, có thể sử dụng
  PastDue = 3,    // Quá hạn thanh toán
  Canceled = 4,   // Đã bị hủy
  Paused = 5      // Tạm dừng
}
```

#### RenewalBehavior
```typescript
enum RenewalBehavior {
  AutoRenew = 1,  // Tự động gia hạn
  Manual = 2      // Gia hạn thủ công
}
```

---

## 🔧 API Details

### 1. Get All Subscriptions

**GET** `/api/cms/subscriptions`

#### Mục đích
- Lấy danh sách tất cả user subscriptions với filter và pagination
- Dùng cho CMS admin/staff để quản lý và theo dõi subscriptions
- Hỗ trợ filter theo user, plan, status, renewal behavior, date range

#### Request

**Query Parameters:**
```
GET /api/cms/subscriptions?pageNumber=1&pageSize=10&sortBy=createdAt&sortOrder=desc&subscriptionStatus=2
```

**Example với TypeScript/Axios:**
```typescript
const params: SubscriptionFilter = {
  pageNumber: 1,
  pageSize: 10,
  sortBy: 'createdAt',
  sortOrder: 'desc',
  subscriptionStatus: 2, // Active only
  isActive: true
};

const response = await axios.get('/api/cms/subscriptions', {
  params,
  headers: {
    'Authorization': `Bearer ${token}`
  }
});
```

#### Response Success (200 OK)

```json
{
  "isSuccess": true,
  "message": "Subscriptions retrieved successfully",
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "userProfileId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        "subscriptionPlanId": "9b7e4a6d-b9b5-4c8e-8f6e-5d8a7c9e6f5a",
        "planName": "premium-monthly",
        "planDisplayName": "Premium Monthly",
        "subscriptionStatus": 2,
        "subscriptionStatusName": "Active",
        "currentPeriodStart": "2025-10-01T00:00:00Z",
        "currentPeriodEnd": "2025-10-31T23:59:59Z",
        "cancelAt": null,
        "canceledAt": null,
        "cancelAtPeriodEnd": false,
        "renewalBehavior": 1,
        "renewalBehaviorName": "AutoRenew",
        "createdAt": "2025-10-01T00:00:00Z",
        "updatedAt": "2025-10-03T10:30:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalRecords": 45,
    "totalPages": 5
  }
}
```

#### Response Error (500 Internal Server Error)

```json
{
  "isSuccess": false,
  "message": "An error occurred while retrieving subscriptions",
  "errorCode": 500
}
```

#### Validation Rules
- `pageNumber`: Minimum 1
- `pageSize`: Minimum 1, Maximum 100
- `subscriptionStatus`: Must be valid enum value (1-5)
- `renewalBehavior`: Must be valid enum value (1-2)
- `startDate`, `endDate`: Must be valid ISO 8601 date format

---

### 2. Get Subscription By ID

**GET** `/api/cms/subscriptions/{id}`

#### Mục đích
- Lấy chi tiết 1 subscription cụ thể
- Dùng cho CMS để xem thông tin chi tiết subscription của user
- Cần có ID cụ thể

#### Request

**Path Parameters:**
- `id` (required): GUID - Subscription ID

**Example Request:**
```typescript
const subscriptionId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

const response = await axios.get(`/api/cms/subscriptions/${subscriptionId}`, {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});
```

#### Response Success (200 OK)

```json
{
  "isSuccess": true,
  "message": "Subscription retrieved successfully",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userProfileId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "subscriptionPlanId": "9b7e4a6d-b9b5-4c8e-8f6e-5d8a7c9e6f5a",
    "planName": "premium-monthly",
    "planDisplayName": "Premium Monthly",
    "subscriptionStatus": 2,
    "subscriptionStatusName": "Active",
    "currentPeriodStart": "2025-10-01T00:00:00Z",
    "currentPeriodEnd": "2025-10-31T23:59:59Z",
    "cancelAt": null,
    "canceledAt": null,
    "cancelAtPeriodEnd": false,
    "renewalBehavior": 1,
    "renewalBehaviorName": "AutoRenew",
    "createdAt": "2025-10-01T00:00:00Z",
    "updatedAt": "2025-10-03T10:30:00Z"
  }
}
```

#### Response Error (404 Not Found)

```json
{
  "isSuccess": false,
  "message": "Subscription not found",
  "errorCode": 404
}
```

#### Validation Rules
- `id`: Must be valid GUID format
- `id`: Must exist in database

---

### 3. Update Subscription

**PUT** `/api/cms/subscriptions/{id}`

#### Mục đích
- Cập nhật settings của subscription (status, renewal behavior, cancel schedule, billing period)
- Dùng cho CMS admin/staff để quản lý subscription lifecycle
- Hỗ trợ partial update (chỉ cần gửi fields muốn update)
- **Lưu ý**: API này sẽ ghi log activity vào UserActivityLogs với IP và UserAgent

#### Request

**Path Parameters:**
- `id` (required): GUID - Subscription ID

**Request Body:**
```json
{
  "subscriptionStatus": 2,
  "renewalBehavior": 1,
  "cancelAtPeriodEnd": false,
  "currentPeriodEnd": "2025-11-30T23:59:59Z"
}
```

**Example với TypeScript/Axios:**
```typescript
const subscriptionId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

const updateRequest: UpdateSubscriptionRequest = {
  subscriptionStatus: 2,        // Active
  renewalBehavior: 1,           // AutoRenew
  cancelAtPeriodEnd: false
};

const response = await axios.put(
  `/api/cms/subscriptions/${subscriptionId}`,
  updateRequest,
  {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  }
);
```

#### Request Body Fields

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `subscriptionStatus` | number | No | Subscription status | Must be 1-5 (enum value) |
| `renewalBehavior` | number | No | Renewal behavior | Must be 1-2 (enum value) |
| `cancelAtPeriodEnd` | boolean | No | Schedule cancellation flag | true/false |
| `currentPeriodStart` | string | No | Billing period start | ISO 8601 date |
| `currentPeriodEnd` | string | No | Billing period end | ISO 8601 date, must be future |
| `cancelAt` | string | No | Scheduled cancellation date | ISO 8601 date, must be future |

#### Response Success (200 OK)

```json
{
  "isSuccess": true,
  "message": "Subscription updated successfully",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userProfileId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "subscriptionPlanId": "9b7e4a6d-b9b5-4c8e-8f6e-5d8a7c9e6f5a",
    "planName": "premium-monthly",
    "planDisplayName": "Premium Monthly",
    "subscriptionStatus": 2,
    "subscriptionStatusName": "Active",
    "currentPeriodStart": "2025-10-01T00:00:00Z",
    "currentPeriodEnd": "2025-11-30T23:59:59Z",
    "cancelAt": null,
    "canceledAt": null,
    "cancelAtPeriodEnd": false,
    "renewalBehavior": 1,
    "renewalBehaviorName": "AutoRenew",
    "createdAt": "2025-10-01T00:00:00Z",
    "updatedAt": "2025-10-03T11:00:00Z"
  }
}
```

#### Response Error (400 Bad Request - Validation Failed)

```json
{
  "isSuccess": false,
  "message": "Validation failed",
  "errorCode": 400,
  "errors": [
    "Invalid subscription status",
    "Current period end must be in the future"
  ]
}
```

#### Response Error (404 Not Found)

```json
{
  "isSuccess": false,
  "message": "Subscription not found",
  "errorCode": 404
}
```

#### Validation Rules
- **Subscription ID**: Required, must be valid GUID, must exist
- **subscriptionStatus**: Must be valid enum (1=InTrial, 2=Active, 3=PastDue, 4=Canceled, 5=Paused)
- **renewalBehavior**: Must be valid enum (1=AutoRenew, 2=Manual)
- **currentPeriodEnd**: Must be future date if provided
- **cancelAt**: Must be future date if provided
- All fields are optional (partial update supported)

#### Business Rules
- Chỉ update các fields được gửi lên (null/undefined fields sẽ bị ignore)
- Không thể update subscription đã bị deleted
- System sẽ tự động ghi log activity với thông tin:
  - Activity Type: "SubscriptionUpdated"
  - IP Address và User Agent của người thực hiện
  - Metadata chứa thông tin chi tiết về subscription được update

---

### 4. Cancel Subscription

**POST** `/api/cms/subscriptions/{id}/cancel`

#### Mục đích
- Hủy subscription của user
- Hỗ trợ 2 modes: Hủy ngay lập tức hoặc hủy khi hết billing period
- Cho phép ghi reason để tracking
- **Lưu ý**: API này sẽ ghi log activity vào UserActivityLogs với IP và UserAgent

#### Request

**Path Parameters:**
- `id` (required): GUID - Subscription ID

**Query Parameters:**
- `cancelAtPeriodEnd` (optional): boolean - Default: `true`
  - `true`: Schedule cancellation at end of current period (user có thể dùng đến hết period)
  - `false`: Cancel immediately (user mất quyền truy cập ngay)
- `reason` (optional): string - Cancellation reason (for tracking/analytics)

**Example Request:**
```typescript
// Scenario 1: Cancel at period end (recommended)
const response1 = await axios.post(
  `/api/cms/subscriptions/${subscriptionId}/cancel?cancelAtPeriodEnd=true&reason=User request`,
  null,
  {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  }
);

// Scenario 2: Cancel immediately (for violations)
const response2 = await axios.post(
  `/api/cms/subscriptions/${subscriptionId}/cancel?cancelAtPeriodEnd=false&reason=Terms violation`,
  null,
  {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  }
);
```

#### Response Success (200 OK) - Cancel at Period End

```json
{
  "isSuccess": true,
  "message": "Subscription will be canceled at the end of the current period",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userProfileId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "subscriptionPlanId": "9b7e4a6d-b9b5-4c8e-8f6e-5d8a7c9e6f5a",
    "planName": "premium-monthly",
    "planDisplayName": "Premium Monthly",
    "subscriptionStatus": 2,
    "subscriptionStatusName": "Active",
    "currentPeriodStart": "2025-10-01T00:00:00Z",
    "currentPeriodEnd": "2025-10-31T23:59:59Z",
    "cancelAt": "2025-10-31T23:59:59Z",
    "canceledAt": null,
    "cancelAtPeriodEnd": true,
    "renewalBehavior": 1,
    "renewalBehaviorName": "AutoRenew",
    "createdAt": "2025-10-01T00:00:00Z",
    "updatedAt": "2025-10-03T11:00:00Z"
  }
}
```

#### Response Success (200 OK) - Cancel Immediately

```json
{
  "isSuccess": true,
  "message": "Subscription canceled immediately",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userProfileId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "subscriptionPlanId": "9b7e4a6d-b9b5-4c8e-8f6e-5d8a7c9e6f5a",
    "planName": "premium-monthly",
    "planDisplayName": "Premium Monthly",
    "subscriptionStatus": 4,
    "subscriptionStatusName": "Canceled",
    "currentPeriodStart": "2025-10-01T00:00:00Z",
    "currentPeriodEnd": "2025-10-31T23:59:59Z",
    "cancelAt": null,
    "canceledAt": "2025-10-03T11:00:00Z",
    "cancelAtPeriodEnd": false,
    "renewalBehavior": 1,
    "renewalBehaviorName": "AutoRenew",
    "createdAt": "2025-10-01T00:00:00Z",
    "updatedAt": "2025-10-03T11:00:00Z"
  }
}
```

#### Response Error (400 Bad Request - Already Canceled)

```json
{
  "isSuccess": false,
  "message": "Subscription is already canceled",
  "errorCode": 400
}
```

#### Response Error (404 Not Found)

```json
{
  "isSuccess": false,
  "message": "Subscription not found",
  "errorCode": 404
}
```

#### Query Parameters Details

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `cancelAtPeriodEnd` | boolean | No | `true` | Schedule cancellation vs immediate |
| `reason` | string | No | null | Cancellation reason for tracking |

#### Cancellation Behavior

**When `cancelAtPeriodEnd = true`:**
- `subscriptionStatus`: Remains `Active` (2) until period end
- `cancelAtPeriodEnd`: Set to `true`
- `cancelAt`: Set to `currentPeriodEnd` date
- `canceledAt`: Remains `null`
- User retains access until period ends
- Automatic renewal is disabled

**When `cancelAtPeriodEnd = false`:**
- `subscriptionStatus`: Changed to `Canceled` (4) immediately
- `cancelAtPeriodEnd`: Set to `false`
- `cancelAt`: Remains `null`
- `canceledAt`: Set to current timestamp
- User loses access immediately
- No refund for remaining days

#### Business Rules
- Cannot cancel an already canceled subscription (status = 4)
- Cannot cancel if subscription not found
- System sẽ tự động ghi log activity với thông tin:
  - Activity Type: "SubscriptionCanceled"
  - IP Address và User Agent của người thực hiện
  - Metadata chứa reason và chi tiết cancellation
- Recommended: Use `cancelAtPeriodEnd=true` for user-requested cancellations
- Recommended: Use `cancelAtPeriodEnd=false` for violations or fraud cases

---

## ❌ Error Handling

### Error Response Structure

Tất cả error responses đều follow cùng 1 structure:

```typescript
interface ErrorResponse {
  isSuccess: false;
  message: string;              // Human-readable error message
  errorCode: number;            // HTTP status code
  errors?: string[];            // Optional: Validation errors array
}
```

### Common Error Codes

| Status Code | Error Code | Description | Common Causes |
|-------------|------------|-------------|---------------|
| 400 | ValidationFailed | Request validation failed | Invalid input, enum out of range, date format |
| 401 | Unauthorized | Authentication failed | Missing/invalid token |
| 403 | Forbidden | Authorization failed | Insufficient role/permissions |
| 404 | NotFound | Resource not found | Invalid ID, deleted subscription |
| 409 | ResourceConflict | Resource conflict | Already canceled subscription |
| 500 | InternalError | Internal server error | Unexpected error, database issue |

### Error Handling Best Practices

```typescript
try {
  const response = await axios.get('/api/cms/subscriptions');
  
  if (response.data.isSuccess) {
    // Handle success
    const subscriptions = response.data.data.items;
  }
} catch (error) {
  if (axios.isAxiosError(error)) {
    const errorResponse = error.response?.data;
    
    switch (errorResponse?.errorCode) {
      case 400:
        // Show validation errors
        console.error('Validation errors:', errorResponse.errors);
        break;
      case 401:
        // Redirect to login
        router.push('/login');
        break;
      case 403:
        // Show permission denied message
        alert('You do not have permission');
        break;
      case 404:
        // Show not found message
        alert('Subscription not found');
        break;
      case 500:
        // Show generic error
        alert('Server error, please try again later');
        break;
      default:
        alert(errorResponse?.message || 'An error occurred');
    }
  }
}
```

---

## ✅ Testing Checklist

### 1. Get All Subscriptions
- [ ] Test without authentication (should return 401)
- [ ] Test with wrong role (should return 403)
- [ ] Test with valid token (should return 200)
- [ ] Test pagination: pageNumber=1, pageSize=10
- [ ] Test pagination: pageNumber=2, pageSize=5
- [ ] Test filter by subscriptionStatus=2 (Active)
- [ ] Test filter by userProfileId (specific user)
- [ ] Test filter by subscriptionPlanId (specific plan)
- [ ] Test filter by isActive=true
- [ ] Test filter by hasCancelScheduled=true
- [ ] Test sort by createdAt desc
- [ ] Test sort by createdAt asc
- [ ] Test with keyword search
- [ ] Test with multiple filters combined
- [ ] Test with date range (startDate, endDate)

### 2. Get Subscription By ID
- [ ] Test without authentication (should return 401)
- [ ] Test with wrong role (should return 403)
- [ ] Test with valid ID (should return 200)
- [ ] Test with non-existent ID (should return 404)
- [ ] Test with invalid GUID format (should return 400)

### 3. Update Subscription
- [ ] Test without authentication (should return 401)
- [ ] Test with wrong role (should return 403)
- [ ] Test update subscriptionStatus to Active (2)
- [ ] Test update subscriptionStatus to Canceled (4)
- [ ] Test update renewalBehavior to Manual (2)
- [ ] Test update cancelAtPeriodEnd to true
- [ ] Test update currentPeriodEnd to future date
- [ ] Test update currentPeriodEnd to past date (should return 400)
- [ ] Test update cancelAt to future date
- [ ] Test update cancelAt to past date (should return 400)
- [ ] Test update multiple fields together
- [ ] Test partial update (only 1 field)
- [ ] Test with non-existent ID (should return 404)
- [ ] Test with invalid enum value (should return 400)
- [ ] Verify activity log created with correct IP/UserAgent

### 4. Cancel Subscription
- [ ] Test without authentication (should return 401)
- [ ] Test with wrong role (should return 403)
- [ ] Test cancel at period end (cancelAtPeriodEnd=true)
- [ ] Test cancel immediately (cancelAtPeriodEnd=false)
- [ ] Test cancel with reason
- [ ] Test cancel without reason
- [ ] Test cancel already canceled subscription (should return 400)
- [ ] Test cancel with non-existent ID (should return 404)
- [ ] Verify status changed to Canceled for immediate cancel
- [ ] Verify cancelAt date set for period-end cancel
- [ ] Verify canceledAt timestamp set for immediate cancel
- [ ] Verify activity log created with correct IP/UserAgent and reason

### Integration Tests
- [ ] Create subscription → Update status → Cancel
- [ ] Create subscription → Schedule cancel → Verify can still access
- [ ] Create subscription → Cancel immediately → Verify no access
- [ ] Update multiple times → Verify all changes logged
- [ ] Cancel → Try to update (should still work for scheduled cancels)

---

## 📝 Notes for Frontend Developers

### Date Format
- Tất cả dates đều theo **ISO 8601 format**: `2025-10-03T11:00:00Z`
- Backend sẽ tự động convert về UTC
- Frontend nên sử dụng `new Date(dateString)` hoặc `dayjs(dateString)`

### Enum Values
- Backend trả về cả **number value** (`subscriptionStatus: 2`) và **string name** (`subscriptionStatusName: "Active"`)
- Frontend có thể hiển thị string name trực tiếp
- Khi gửi request, sử dụng number value

### Pagination
- Default page size: 10
- Max page size: 100
- `totalPages` = Math.ceil(totalRecords / pageSize)
- Frontend cần handle empty list (`items: []`)

### Error Handling
- Luôn check `isSuccess` field trước
- Hiển thị `message` cho user
- Log `errors` array nếu có (cho validation errors)

### Authentication
- Token expires sau 1 giờ (tùy config)
- Frontend cần implement token refresh
- Hoặc redirect to login khi gặp 401

### Activity Logging
- Mọi update/cancel actions đều được log tự động
- IP Address và User Agent được capture từ HTTP request
- Admin có thể xem lịch sử changes trong UserActivityLogs

---

## 🔗 Related APIs

- **Subscription Plans API**: `/api/cms/subscription-plans` - Quản lý các plan templates
- **User Profiles API**: `/api/users/profiles` - Quản lý user information
- **User Activity Logs API**: `/api/users/activity-logs` - Xem lịch sử thay đổi

---

## 📞 Support

Nếu có vấn đề hoặc câu hỏi:
1. Check lại API documentation này
2. Test với Postman/Insomnia trước
3. Check error response details
4. Contact Backend team với error logs

---

**Document Version**: 1.0  
**Last Updated**: October 3, 2025  
**Author**: Backend Team  
**Review Status**: ✅ Ready for Frontend Implementation
