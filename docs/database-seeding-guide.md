# 🌱 Database Seeding Guide

This guide explains how to seed initial data for the Healink Microservices system, including admin accounts, permissions, and business roles.

## 🎯 Overview

The system automatically seeds essential data on startup to ensure proper functionality:

### **AuthService Seeds:**
- ✅ **Core Roles** (Admin, Staff, User)
- ✅ **Permissions** (25+ permissions across 5 modules)
- ✅ **Admin User Account** with all permissions
- ✅ **Role-Permission Assignments**

### **UserService Seeds:**
- ✅ **Business Roles** (12 business-specific roles)
- ✅ **Admin User Profile** (linked to AuthService admin)
- ✅ **Admin Business Role Assignments**

## 🔧 Configuration

### **Environment Variables**
```bash
# Admin Account Configuration
ADMIN_EMAIL=admin@healink.com
ADMIN_PASSWORD=HealinkAdmin2024!
DEFAULT_ADMIN_USER_ID=00000000-0000-0000-0000-000000000001

# Seeding Control
DATA_ENABLE_SEED_DATA=true
```

### **appsettings.json**
```json
{
  "DataConfig": {
    "EnableSeedData": "true"
  },
  "DefaultAdminAccount": {
    "Email": "${ADMIN_EMAIL:admin@healink.com}",
    "Password": "${ADMIN_PASSWORD:HealinkAdmin2024!}",
    "UserId": "${DEFAULT_ADMIN_USER_ID:00000000-0000-0000-0000-000000000001}"
  }
}
```

## 🏗️ AuthService Seeding

### **1. Core Roles**
```csharp
// Seeded from RoleEnum
- Admin      // Full system access
- Staff      // Internal team access  
- User       // End user access
```

### **2. Core Permissions (25 permissions)**

#### **Authentication Module (6 permissions)**
```csharp
- auth.manage                    // Full auth system control
- auth.users.view               // View user accounts
- auth.users.create             // Create user accounts
- auth.users.edit               // Edit user accounts
- auth.users.delete             // Delete user accounts
- auth.roles.manage             // Manage roles & permissions
```

#### **User Module (5 permissions)**
```csharp
- user.profile.view             // View user profiles
- user.profile.edit             // Edit user profiles
- user.business_roles.manage    // Assign business roles
- user.applications.view        // View creator applications
- user.applications.approve     // Approve creator applications
```

#### **Content Module (8 permissions)**
```csharp
- content.create                        // Create content
- content.edit                          // Edit content
- content.delete                        // Delete content
- content.publish                       // Publish content
- content.moderate                      // Moderate content
- content.podcast.manage                // Manage podcasts
- content.flashcard.manage              // Manage flashcards
- content.community_story.moderate      // Moderate stories
```

#### **System Module (4 permissions)**
```csharp
- system.admin                  // Full system admin
- system.settings.manage        // System settings
- system.logs.view             // View logs
- system.monitoring.view       // System monitoring
```

#### **Notification Module (2 permissions)**
```csharp
- notification.send            // Send notifications
- notification.manage          // Manage notifications
```

### **3. Admin User**
```csharp
User ID: 00000000-0000-0000-0000-000000000001
Email: admin@healink.com
Password: HealinkAdmin2024!
Roles: [Admin]
Permissions: ALL (25 permissions)
```

## 🏗️ UserService Seeding

### **1. Business Roles (12 roles)**

#### **User Roles**
```csharp
- FreeUser (Priority: 100)
  - Description: Standard user with basic access
  - Required Core Role: User
  - Requires Approval: false
  - Permissions: []

- PremiumUser (Priority: 90)
  - Description: Premium subscriber with enhanced features
  - Required Core Role: User
  - Requires Approval: false
  - Permissions: ["content.premium.access"]
```

#### **Content Creator Roles**
```csharp
- ContentCreator (Priority: 50)
  - Description: Create and manage own content
  - Required Core Role: User
  - Requires Approval: true
  - Permissions: ["content.create", "content.edit", "content.publish"]

- ContentEditor (Priority: 40)
  - Description: Edit and review content from creators
  - Required Core Role: Staff
  - Requires Approval: false
  - Permissions: ["content.create", "content.edit", "content.publish", "content.review"]

- ExpertCollaborator (Priority: 45)
  - Description: Medical/wellness expert contributing content
  - Required Core Role: User
  - Requires Approval: true
  - Permissions: ["content.create", "content.edit", "content.expert_review"]
```

#### **Community Roles**
```csharp
- CommunityMember (Priority: 80)
  - Description: Active community participant
  - Required Core Role: User
  - Requires Approval: false
  - Permissions: ["content.community_story.create", "content.comment"]

- CommunityModerator (Priority: 30)
  - Description: Moderate community content and discussions
  - Required Core Role: Staff
  - Requires Approval: false
  - Permissions: ["content.moderate", "content.community_story.moderate"]
```

#### **Admin Roles**
```csharp
- SystemAdministrator (Priority: 10)
  - Description: Full system administration access
  - Required Core Role: Admin
  - Permissions: ["system.admin", "system.settings.manage", "system.logs.view"]

- UserManager (Priority: 20)
  - Description: Manage user accounts and business roles
  - Required Core Role: Admin
  - Permissions: ["user.profile.view", "user.profile.edit", "user.business_roles.manage", "user.applications.approve"]

- EcommerceManager (Priority: 25)
  - Description: Manage e-commerce operations
  - Required Core Role: Admin
  - Permissions: ["order.manage", "subscription.manage"]

- MarketingManager (Priority: 25)
  - Description: Manage marketing campaigns
  - Required Core Role: Admin
  - Permissions: ["content.promote", "notification.send", "notification.manage"]

- DataAnalyst (Priority: 35)
  - Description: Access to analytics and reporting
  - Required Core Role: Staff
  - Permissions: ["system.monitoring.view", "content.analytics.view", "user.analytics.view"]

- BusinessOwner (Priority: 5)
  - Description: Complete business oversight and decision making
  - Required Core Role: Admin
  - Permissions: ["system.admin", "system.monitoring.view", "user.profile.view", "content.analytics.view"]
```

### **2. Admin User Profile**
```csharp
User ID: 00000000-0000-0000-0000-000000000001  // Links to AuthService admin
Full Name: System Administrator
Email: admin@healink.com
Phone: +1-000-000-0000
Business Roles: [SystemAdministrator, UserManager, BusinessOwner]
```

## 🚀 Seeding Process

### **Startup Sequence**
```csharp
1. AuthService starts
2. Apply AuthService migrations
3. Seed AuthService data:
   - Core roles (Admin, Staff, User)
   - Core permissions (25 permissions)
   - Admin user account
   - Role-permission assignments

4. UserService starts  
5. Apply UserService migrations
6. Seed UserService data:
   - Business roles (12 roles)
   - Admin user profile
   - Admin business role assignments
```

### **Idempotent Seeding**
- ✅ **Safe to run multiple times** - checks for existing data
- ✅ **Only creates missing data** - doesn't duplicate existing records
- ✅ **Updates if needed** - can update business role assignments

## 🔍 Verification

### **Check AuthService Seeding**
```sql
-- Check roles
SELECT * FROM "AspNetRoles" ORDER BY "Name";

-- Check permissions
SELECT * FROM "Permissions" ORDER BY "Module", "Name";

-- Check admin user
SELECT u."Email", r."Name" as "Role"
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'admin@healink.com';

-- Check role permissions
SELECT r."Name" as "Role", p."Name" as "Permission", p."Module"
FROM "AspNetRoles" r
JOIN "RolePermissions" rp ON r."Id" = rp."RoleId"
JOIN "Permissions" p ON rp."PermissionId" = p."Id"
WHERE r."Name" = 'Admin'
ORDER BY p."Module", p."Name";
```

### **Check UserService Seeding**
```sql
-- Check business roles
SELECT "RoleType", "DisplayName", "RequiredCoreRole", "RequiresApproval", "Priority"
FROM "BusinessRoles" 
ORDER BY "Priority";

-- Check admin profile
SELECT * FROM "UserProfiles" 
WHERE "Email" = 'admin@healink.com';

-- Check admin business role assignments
SELECT up."FullName", br."DisplayName" as "BusinessRole"
FROM "UserProfiles" up
JOIN "UserBusinessRoles" ubr ON up."UserId" = ubr."UserId"
JOIN "BusinessRoles" br ON ubr."BusinessRoleId" = br."Id"
WHERE up."Email" = 'admin@healink.com';
```

## 🛠️ Customization

### **Adding New Permissions**
```csharp
// In AuthSeedingExtension.cs
var corePermissions = new List<(string Name, string DisplayName, string Description, PermissionModuleEnum Module)>
{
    // Add new permission
    ("content.video.manage", "Manage Videos", "Full control over video content", PermissionModuleEnum.Content),
};
```

### **Adding New Business Roles**
```csharp
// In UserSeedingExtension.cs
var businessRoles = new List<(BusinessRoleEnum RoleType, string Name, string DisplayName, string Description, RoleEnum RequiredCoreRole, bool RequiresApproval, string[] Permissions, int Priority)>
{
    // Add new business role
    (BusinessRoleEnum.VideoCreator, "VideoCreator", "Video Creator", "Create and manage video content", RoleEnum.User, true, new string[] { "content.video.manage" }, 55),
};
```

### **Updating Admin Configuration**
```bash
# Development
ADMIN_EMAIL=dev-admin@healink.com
ADMIN_PASSWORD=DevAdmin123!

# Production
ADMIN_EMAIL=admin@production.healink.com
ADMIN_PASSWORD=SecureProductionPassword123!
```

## 🚨 Security Notes

### **Production Checklist**
- ✅ **Change default admin password** immediately after first login
- ✅ **Use strong passwords** (minimum 12 characters, mixed case, numbers, symbols)
- ✅ **Rotate admin credentials** regularly
- ✅ **Monitor admin access** logs
- ✅ **Use environment-specific** admin accounts
- ✅ **Disable default admin** if using external auth (Azure AD, etc.)

### **Environment Separation**
```bash
# Development
ADMIN_EMAIL=dev-admin@healink.local
ADMIN_PASSWORD=DevPassword123!

# Staging  
ADMIN_EMAIL=staging-admin@healink-staging.com
ADMIN_PASSWORD=StagingPassword123!

# Production
ADMIN_EMAIL=admin@healink.com
ADMIN_PASSWORD=ProductionSecurePassword123!
```

## 🔗 Related Documentation

- [Environment Configuration Guide](./environment-configuration.md)
- [Database Migrations Guide](./database-migrations-guide.md)
- [Microservices Architecture](./microservices-architecture.md)
- [Authentication & Authorization](./auth-architecture.md)
