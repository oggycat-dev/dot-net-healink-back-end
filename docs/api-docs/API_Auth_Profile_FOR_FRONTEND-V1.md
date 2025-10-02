# 📡 API Documentation for Frontend

> **Comprehensive API Reference for Frontend Integration**  
> Base URL: `http://localhost:5010` (Gateway)

---

## 📋 Table of Contents
- [Authentication APIs](#authentication-apis)
  - [Register](#1-register-user)
  - [Verify OTP](#2-verify-otp)
  - [Login](#3-login)
  - [Logout](#4-logout)
  - [Refresh Token](#5-refresh-token)
  - [Reset Password](#6-reset-password)
- [Profile APIs](#profile-apis)
  - [Get Profile](#1-get-user-profile)
- [Common Response Format](#common-response-format)
- [Error Codes](#error-codes)
- [Authentication Flow](#authentication-flow)

---

## 🔐 Authentication APIs

### 1. Register User

**Description:** Đăng ký user mới và gửi OTP qua email/SMS

**Endpoint:** `POST /api/user/auth/register`

**Authentication:** ❌ No authentication required

**Request Headers:**
```
Content-Type: application/json
```

**Request Body:**
```typescript
{
  email: string;           // Required, valid email format
  password: string;        // Required, min 8 chars, must contain uppercase, lowercase, number, special char
  confirmPassword: string; // Required, must match password
  fullName: string;        // Required, max 100 chars
  phoneNumber: string;     // Required, format: +84xxxxxxxxx
  otpSentChannel: number;  // Optional, default: 1 (1=Email, 2=Phone)
}
```

**Example Request:**
```javascript
const response = await fetch('http://localhost:8000/api/user/auth/register', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'User@123',
    confirmPassword: 'User@123',
    fullName: 'John Doe',
    phoneNumber: '+84987654321',
    otpSentChannel: 1
  })
});

const data = await response.json();
```

**Success Response (200 OK):**
```json
{
  "isSuccess": true,
  "message": "Registration started. Please check your email for OTP.",
  "data": null,
  "errors": null
}
```

**Error Response (400 Bad Request):**
```json
{
  "isSuccess": false,
  "message": "Validation failed",
  "data": null,
  "errors": [
    "Email is required",
    "Password must be at least 8 characters",
    "Passwords do not match"
  ]
}
```

**Error Response (409 Conflict):**
```json
{
  "isSuccess": false,
  "message": "Email already exists",
  "data": null,
  "errors": ["This email is already registered"]
}
```

---

### 2. Verify OTP

**Description:** Xác thực OTP code để hoàn tất đăng ký hoặc reset password

**Endpoint:** `POST /api/user/auth/verify-otp`

**Authentication:** ❌ No authentication required

**Request Headers:**
```
Content-Type: application/json
```

**Request Body:**
```typescript
{
  contact: string;         // Required, email or phone number
  otpCode: string;         // Required, 6-digit OTP code
  otpSentChannel: number;  // Required, 1=Email, 2=Phone
  otpType: number;         // Required, 1=Registration, 2=PasswordReset
}
```

**Example Request:**
```javascript
const response = await fetch('http://localhost:8000/api/user/auth/verify-otp', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    contact: 'user@example.com',
    otpCode: '123456',
    otpSentChannel: 1,
    otpType: 1
  })
});

const data = await response.json();
```

**Success Response (200 OK):**
```json
{
  "isSuccess": true,
  "message": "OTP verified successfully. Creating your account...",
  "data": null,
  "errors": null
}
```

**Error Response (400 Bad Request - Invalid OTP):**
```json
{
  "isSuccess": false,
  "message": "Invalid OTP code",
  "data": null,
  "errors": ["The OTP code you entered is incorrect"]
}
```

**Error Response (410 Gone - Expired OTP):**
```json
{
  "isSuccess": false,
  "message": "OTP expired",
  "data": null,
  "errors": ["OTP has expired. Please request a new one."]
}
```

---

### 3. Login

**Description:** Đăng nhập và nhận JWT access token

**Endpoint:** 
- CMS Admin: `POST /api/cms/auth/login`
- User App: `POST /api/user/auth/login`

**Authentication:** ❌ No authentication required

**Request Headers:**
```
Content-Type: application/json
```

**Request Body:**
```typescript
{
  email: string;      // Required, valid email
  password: string;   // Required
  grantType: number;  // Optional, default: 0 (0=Password, 1=RefreshToken)
}
```

**Example Request:**
```javascript
const response = await fetch('http://localhost:8000/api/user/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'User@123',
    grantType: 0
  })
});

const data = await response.json();

// Save token to localStorage
if (data.isSuccess) {
  localStorage.setItem('accessToken', data.data.accessToken);
  localStorage.setItem('expiresAt', data.data.expiresAt);
  localStorage.setItem('roles', JSON.stringify(data.data.roles));
}
```

**Success Response (200 OK):**
```json
{
  "isSuccess": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
    "expiresAt": "2025-10-02T14:00:00Z",
    "roles": ["User"]
  },
  "errors": null
}
```

**Error Response (401 Unauthorized):**
```json
{
  "isSuccess": false,
  "message": "Invalid credentials",
  "data": null,
  "errors": ["Email or password is incorrect"]
}
```

**Error Response (403 Forbidden - Wrong Portal):**
```json
{
  "isSuccess": false,
  "message": "Access denied",
  "data": null,
  "errors": ["You don't have permission to access this portal"]
}
```

---

### 4. Logout

**Description:** Đăng xuất và xóa refresh token

**Endpoint:**
- CMS Admin: `POST /api/cms/auth/logout`
- User App: `POST /api/user/auth/logout`

**Authentication:** ✅ Bearer Token required

**Request Headers:**
```
Authorization: Bearer {accessToken}
```

**Request Body:** None

**Example Request:**
```javascript
const token = localStorage.getItem('accessToken');

const response = await fetch('http://localhost:8000/api/user/auth/logout', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

const data = await response.json();

// Clear local storage
if (data.isSuccess) {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('expiresAt');
  localStorage.removeItem('roles');
}
```

**Success Response (200 OK):**
```json
{
  "isSuccess": true,
  "message": "Logout successful",
  "data": null,
  "errors": null
}
```

**Error Response (401 Unauthorized):**
```json
{
  "isSuccess": false,
  "message": "Unauthorized",
  "data": null,
  "errors": ["Invalid or expired token"]
}
```

---

### 5. Refresh Token

**Description:** Làm mới access token khi hết hạn

**Endpoint:**
- CMS Admin: `POST /api/cms/auth/refresh-token`
- User App: `POST /api/user/auth/refresh-token`

**Authentication:** ✅ Bearer Token required (current access token)

**Request Headers:**
```
Authorization: Bearer {accessToken}
```

**Request Body:** None

**Example Request:**
```javascript
const token = localStorage.getItem('accessToken');

const response = await fetch('http://localhost:8000/api/user/auth/refresh-token', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

const data = await response.json();

// Update token in localStorage
if (data.isSuccess) {
  localStorage.setItem('accessToken', data.data.accessToken);
  localStorage.setItem('expiresAt', data.data.expiresAt);
}
```

**Success Response (200 OK):**
```json
{
  "isSuccess": true,
  "message": "Token refreshed successfully",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-10-02T15:00:00Z",
    "roles": ["User"]
  },
  "errors": null
}
```

**Error Response (401 Unauthorized):**
```json
{
  "isSuccess": false,
  "message": "Refresh token expired",
  "data": null,
  "errors": ["Please login again"]
}
```

---

### 6. Reset Password

**Description:** Gửi OTP để reset password

**Endpoint:** `POST /api/user/auth/reset-password`

**Authentication:** ❌ No authentication required

**Request Headers:**
```
Content-Type: application/json
```

**Request Body:**
```typescript
{
  contact: string;         // Required, email or phone
  newPassword: string;     // Required, min 8 chars
  otpSentChannel: number;  // Required, 1=Email, 2=Phone
}
```

**Example Request:**
```javascript
const response = await fetch('http://localhost:8000/api/user/auth/reset-password', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    contact: 'user@example.com',
    newPassword: 'NewPassword@123',
    otpSentChannel: 1
  })
});

const data = await response.json();
```

**Success Response (200 OK):**
```json
{
  "isSuccess": true,
  "message": "OTP sent successfully. Please check your email.",
  "data": null,
  "errors": null
}
```

**Error Response (404 Not Found):**
```json
{
  "isSuccess": false,
  "message": "User not found",
  "data": null,
  "errors": ["No account found with this email"]
}
```

---

## 👤 Profile APIs

### 1. Get User Profile

**Description:** Lấy thông tin profile của user đang đăng nhập

**Endpoint:** `GET /api/user/profile`

**Authentication:** ✅ Bearer Token required

**Request Headers:**
```
Authorization: Bearer {accessToken}
```

**Request Body:** None

**Example Request:**
```javascript
const token = localStorage.getItem('accessToken');

const response = await fetch('http://localhost:8000/api/user/profile', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

const data = await response.json();
```

**Success Response (200 OK):**
```json
{
  "isSuccess": true,
  "message": "Profile retrieved successfully",
  "data": {
    "fullName": "John Doe",
    "email": "user@example.com",
    "phoneNumber": "+84987654321",
    "address": "123 Main St, Hanoi",
    "avatarPath": "/uploads/avatars/user123.jpg",
    "createdAt": "2025-10-01T10:00:00Z"
  },
  "errors": null
}
```

**Error Response (401 Unauthorized):**
```json
{
  "isSuccess": false,
  "message": "Unauthorized",
  "data": null,
  "errors": ["Invalid or expired token"]
}
```

**Error Response (404 Not Found):**
```json
{
  "isSuccess": false,
  "message": "Profile not found",
  "data": null,
  "errors": ["User profile does not exist"]
}
```

---

## 📦 Common Response Format

All APIs follow this standard response format:

```typescript
interface ApiResponse<T> {
  isSuccess: boolean;      // true if request succeeded, false otherwise
  message: string;         // Human-readable message
  data: T | null;         // Response data (null if failed)
  errors: string[] | null; // Error messages (null if succeeded)
}
```

**Success Response Example:**
```json
{
  "isSuccess": true,
  "message": "Operation successful",
  "data": { /* actual data */ },
  "errors": null
}
```

**Error Response Example:**
```json
{
  "isSuccess": false,
  "message": "Operation failed",
  "data": null,
  "errors": ["Error message 1", "Error message 2"]
}
```

---

## 🚨 Error Codes

### HTTP Status Codes

| Status Code | Meaning | When It Occurs |
|------------|---------|----------------|
| 200 | OK | Request successful |
| 400 | Bad Request | Validation error, invalid input |
| 401 | Unauthorized | Missing or invalid token |
| 403 | Forbidden | No permission to access resource |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Resource already exists (e.g., email) |
| 500 | Internal Server Error | Server error |

### Common Error Messages

```typescript
// Validation Errors
"Email is required"
"Invalid email format"
"Password must be at least 8 characters"
"Passwords do not match"

// Authentication Errors
"Invalid credentials"
"Email or password is incorrect"
"Invalid or expired token"
"Refresh token expired"

// Authorization Errors
"Access denied"
"You don't have permission to access this portal"

// Registration Errors
"Email already exists"
"This email is already registered"

// OTP Errors
"Invalid OTP code"
"OTP has expired"
"OTP not found"

// Profile Errors
"User not found"
"Profile not found"
```

---

## 🔄 Authentication Flow

### Complete Registration & Login Flow

```
┌─────────────────────────────────────────────────────────┐
│                    REGISTRATION FLOW                     │
└─────────────────────────────────────────────────────────┘

1. User fills registration form
   ↓
2. POST /api/user/auth/register
   Request: { email, password, confirmPassword, fullName, phoneNumber }
   Response: { isSuccess: true, message: "Check your email for OTP" }
   ↓
3. User receives OTP via email/SMS
   ↓
4. User enters OTP code
   ↓
5. POST /api/user/auth/verify-otp
   Request: { contact, otpCode, otpSentChannel: 1, otpType: 1 }
   Response: { isSuccess: true, message: "Account created successfully" }
   ↓
6. User can now login

┌─────────────────────────────────────────────────────────┐
│                       LOGIN FLOW                         │
└─────────────────────────────────────────────────────────┘

1. User enters email & password
   ↓
2. POST /api/user/auth/login
   Request: { email, password, grantType: 0 }
   Response: {
     isSuccess: true,
     data: {
       accessToken: "...",
       expiresAt: "...",
       roles: ["User"]
     }
   }
   ↓
3. Save accessToken to localStorage
   localStorage.setItem('accessToken', data.data.accessToken)
   ↓
4. Use token for authenticated requests
   headers: { 'Authorization': 'Bearer ' + token }

┌─────────────────────────────────────────────────────────┐
│                  TOKEN REFRESH FLOW                      │
└─────────────────────────────────────────────────────────┘

1. Check if token is expired
   const expiresAt = new Date(localStorage.getItem('expiresAt'))
   const isExpired = expiresAt < new Date()
   ↓
2. If expired, refresh token
   POST /api/user/auth/refresh-token
   headers: { 'Authorization': 'Bearer ' + oldToken }
   ↓
3. Update token in localStorage
   localStorage.setItem('accessToken', newToken)

┌─────────────────────────────────────────────────────────┐
│                  PASSWORD RESET FLOW                     │
└─────────────────────────────────────────────────────────┘

1. User clicks "Forgot Password"
   ↓
2. POST /api/user/auth/reset-password
   Request: { contact, newPassword, otpSentChannel: 1 }
   Response: { isSuccess: true, message: "OTP sent to your email" }
   ↓
3. User receives OTP
   ↓
4. POST /api/user/auth/verify-otp
   Request: { contact, otpCode, otpSentChannel: 1, otpType: 2 }
   Response: { isSuccess: true, message: "Password reset successful" }
   ↓
5. User can login with new password
```

---

## 💻 TypeScript Interfaces

### Request Types

```typescript
// Registration Request
interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  fullName: string;
  phoneNumber: string;
  otpSentChannel?: number; // 1=Email, 2=Phone
}

// Verify OTP Request
interface VerifyOtpRequest {
  contact: string;
  otpCode: string;
  otpSentChannel: number; // 1=Email, 2=Phone
  otpType: number;        // 1=Registration, 2=PasswordReset
}

// Login Request
interface LoginRequest {
  email: string;
  password: string;
  grantType?: number; // 0=Password, 1=RefreshToken
}

// Reset Password Request
interface ResetPasswordRequest {
  contact: string;
  newPassword: string;
  otpSentChannel: number; // 1=Email, 2=Phone
}
```

### Response Types

```typescript
// Generic API Response
interface ApiResponse<T = any> {
  isSuccess: boolean;
  message: string;
  data: T | null;
  errors: string[] | null;
}

// Auth Response
interface AuthResponse {
  accessToken: string;
  expiresAt: string; // ISO 8601 date string
  roles: string[];
}

// Profile Response
interface ProfileResponse {
  fullName: string;
  email: string;
  phoneNumber: string;
  address: string | null;
  avatarPath: string | null;
  createdAt: string | null; // ISO 8601 date string
}
```

---

## 🛠️ Helper Functions for Frontend

### 1. API Client Setup

```typescript
// api-client.ts
const API_BASE_URL = 'http://localhost:8000';

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<ApiResponse<T>> {
    const url = `${this.baseUrl}${endpoint}`;
    
    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    return response.json();
  }

  async get<T>(endpoint: string, token?: string): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      method: 'GET',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });
  }

  async post<T>(
    endpoint: string,
    body: any,
    token?: string
  ): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: JSON.stringify(body),
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });
  }
}

export const apiClient = new ApiClient(API_BASE_URL);
```

### 2. Auth Service

```typescript
// auth.service.ts
import { apiClient } from './api-client';

export class AuthService {
  // Register
  async register(data: RegisterRequest): Promise<ApiResponse> {
    return apiClient.post('/api/user/auth/register', data);
  }

  // Verify OTP
  async verifyOtp(data: VerifyOtpRequest): Promise<ApiResponse> {
    return apiClient.post('/api/user/auth/verify-otp', data);
  }

  // Login
  async login(data: LoginRequest): Promise<ApiResponse<AuthResponse>> {
    const response = await apiClient.post<AuthResponse>(
      '/api/user/auth/login',
      data
    );

    if (response.isSuccess && response.data) {
      this.saveToken(response.data);
    }

    return response;
  }

  // Logout
  async logout(): Promise<ApiResponse> {
    const token = this.getToken();
    const response = await apiClient.post('/api/user/auth/logout', {}, token);
    
    if (response.isSuccess) {
      this.clearToken();
    }

    return response;
  }

  // Refresh Token
  async refreshToken(): Promise<ApiResponse<AuthResponse>> {
    const token = this.getToken();
    const response = await apiClient.post<AuthResponse>(
      '/api/user/auth/refresh-token',
      {},
      token
    );

    if (response.isSuccess && response.data) {
      this.saveToken(response.data);
    }

    return response;
  }

  // Reset Password
  async resetPassword(data: ResetPasswordRequest): Promise<ApiResponse> {
    return apiClient.post('/api/user/auth/reset-password', data);
  }

  // Token Management
  private saveToken(authData: AuthResponse): void {
    localStorage.setItem('accessToken', authData.accessToken);
    localStorage.setItem('expiresAt', authData.expiresAt);
    localStorage.setItem('roles', JSON.stringify(authData.roles));
  }

  getToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  private clearToken(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('expiresAt');
    localStorage.removeItem('roles');
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    const expiresAt = localStorage.getItem('expiresAt');

    if (!token || !expiresAt) return false;

    return new Date(expiresAt) > new Date();
  }

  getRoles(): string[] {
    const roles = localStorage.getItem('roles');
    return roles ? JSON.parse(roles) : [];
  }
}

export const authService = new AuthService();
```

### 3. Profile Service

```typescript
// profile.service.ts
import { apiClient } from './api-client';
import { authService } from './auth.service';

export class ProfileService {
  async getProfile(): Promise<ApiResponse<ProfileResponse>> {
    const token = authService.getToken();
    if (!token) {
      return {
        isSuccess: false,
        message: 'Not authenticated',
        data: null,
        errors: ['Please login first'],
      };
    }

    return apiClient.get<ProfileResponse>('/api/user/profile', token);
  }
}

export const profileService = new ProfileService();
```

### 4. Auto Token Refresh

```typescript
// token-refresh.interceptor.ts
import { authService } from './auth.service';

export async function autoRefreshToken(): Promise<void> {
  const expiresAt = localStorage.getItem('expiresAt');
  if (!expiresAt) return;

  const expirationTime = new Date(expiresAt).getTime();
  const currentTime = new Date().getTime();
  const timeUntilExpiry = expirationTime - currentTime;

  // Refresh 5 minutes before expiry
  const refreshThreshold = 5 * 60 * 1000; // 5 minutes in milliseconds

  if (timeUntilExpiry < refreshThreshold && timeUntilExpiry > 0) {
    try {
      await authService.refreshToken();
      console.log('Token refreshed automatically');
    } catch (error) {
      console.error('Failed to refresh token:', error);
      authService.logout();
    }
  }
}

// Call this function periodically (e.g., every minute)
setInterval(autoRefreshToken, 60000); // Check every minute
```

---

## 📱 Usage Examples

### React Component Example

```typescript
// RegisterForm.tsx
import React, { useState } from 'react';
import { authService } from './services/auth.service';

export const RegisterForm: React.FC = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    fullName: '',
    phoneNumber: '',
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const response = await authService.register({
        ...formData,
        otpSentChannel: 1, // Email
      });

      if (response.isSuccess) {
        // Redirect to OTP verification page
        window.location.href = '/verify-otp';
      } else {
        setError(response.errors?.join(', ') || response.message);
      }
    } catch (err) {
      setError('Network error. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      {error && <div className="error">{error}</div>}
      
      <input
        type="email"
        placeholder="Email"
        value={formData.email}
        onChange={(e) => setFormData({ ...formData, email: e.target.value })}
        required
      />
      
      <input
        type="password"
        placeholder="Password"
        value={formData.password}
        onChange={(e) => setFormData({ ...formData, password: e.target.value })}
        required
      />
      
      <input
        type="password"
        placeholder="Confirm Password"
        value={formData.confirmPassword}
        onChange={(e) => setFormData({ ...formData, confirmPassword: e.target.value })}
        required
      />
      
      <input
        type="text"
        placeholder="Full Name"
        value={formData.fullName}
        onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
        required
      />
      
      <input
        type="tel"
        placeholder="Phone Number"
        value={formData.phoneNumber}
        onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
        required
      />
      
      <button type="submit" disabled={loading}>
        {loading ? 'Registering...' : 'Register'}
      </button>
    </form>
  );
};
```

### Login Component Example

```typescript
// LoginForm.tsx
import React, { useState } from 'react';
import { authService } from './services/auth.service';

export const LoginForm: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const response = await authService.login({
        email,
        password,
        grantType: 0,
      });

      if (response.isSuccess) {
        // Redirect to dashboard
        window.location.href = '/dashboard';
      } else {
        setError(response.errors?.join(', ') || response.message);
      }
    } catch (err) {
      setError('Network error. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      {error && <div className="error">{error}</div>}
      
      <input
        type="email"
        placeholder="Email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
      />
      
      <input
        type="password"
        placeholder="Password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
      />
      
      <button type="submit" disabled={loading}>
        {loading ? 'Logging in...' : 'Login'}
      </button>
    </form>
  );
};
```

### Profile Component Example

```typescript
// ProfilePage.tsx
import React, { useEffect, useState } from 'react';
import { profileService } from './services/profile.service';
import { ProfileResponse } from './types';

export const ProfilePage: React.FC = () => {
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadProfile();
  }, []);

  const loadProfile = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await profileService.getProfile();

      if (response.isSuccess && response.data) {
        setProfile(response.data);
      } else {
        setError(response.errors?.join(', ') || response.message);
      }
    } catch (err) {
      setError('Failed to load profile');
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div>Loading...</div>;
  if (error) return <div className="error">{error}</div>;
  if (!profile) return <div>No profile data</div>;

  return (
    <div className="profile">
      <h1>Profile</h1>
      <div className="profile-info">
        <p><strong>Name:</strong> {profile.fullName}</p>
        <p><strong>Email:</strong> {profile.email}</p>
        <p><strong>Phone:</strong> {profile.phoneNumber}</p>
        <p><strong>Address:</strong> {profile.address || 'N/A'}</p>
        {profile.avatarPath && (
          <img src={profile.avatarPath} alt="Avatar" />
        )}
      </div>
    </div>
  );
};
```

---

## 🔍 Testing with cURL

### Register
```bash
curl -X POST http://localhost:8000/api/user/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123",
    "confirmPassword": "Test@123",
    "fullName": "Test User",
    "phoneNumber": "+84987654321",
    "otpSentChannel": 1
  }'
```

### Verify OTP
```bash
curl -X POST http://localhost:8000/api/user/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "contact": "test@example.com",
    "otpCode": "123456",
    "otpSentChannel": 1,
    "otpType": 1
  }'
```

### Login
```bash
curl -X POST http://localhost:8000/api/user/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123",
    "grantType": 0
  }'
```

### Get Profile
```bash
curl -X GET http://localhost:8000/api/user/profile \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE"
```

---

## 📝 Notes for Frontend Developers

### Important Points

1. **Always check `isSuccess` field** before accessing `data`
2. **Store token in localStorage** for persistent auth
3. **Include Authorization header** for protected endpoints
4. **Handle token expiration** with auto-refresh
5. **Show user-friendly error messages** from `errors` array
6. **Validate input on frontend** before sending to API
7. **Use TypeScript interfaces** for type safety

### Best Practices

- ✅ Always check token expiration before making requests
- ✅ Implement auto token refresh 5 minutes before expiry
- ✅ Clear tokens on logout
- ✅ Handle network errors gracefully
- ✅ Show loading states during API calls
- ✅ Validate user input before submission
- ✅ Display meaningful error messages to users

### Security Considerations

- 🔒 Never store passwords in localStorage
- 🔒 Always use HTTPS in production
- 🔒 Clear tokens on logout
- 🔒 Implement CORS properly
- 🔒 Validate JWT tokens on backend
- 🔒 Set token expiration properly

---

**Last Updated:** October 2, 2025  
**Version:** 1.0  
**Author:** Healink Backend Team  
**Status:** ✅ Ready for Frontend Integration

