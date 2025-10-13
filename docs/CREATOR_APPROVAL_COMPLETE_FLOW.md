# Creator Application Approval - Complete Flow Diagram

## Full System Flow (Mermaid)

```mermaid
sequenceDiagram
    autonumber
    
    actor User as 👤 User
    participant Gateway as 🌐 Gateway
    participant UserAPI as 🔵 UserService API
    participant UserHandler as 📝 ApproveHandler
    participant UserDB as 💾 UserService DB
    participant Outbox as 📤 Outbox
    participant RabbitMQ as 🐰 RabbitMQ
    participant AuthHandler as 🔐 AuthUserRolesChangedHandler
    participant AuthDB as 💾 AuthService DB
    participant Redis as 🔴 Redis Cache
    participant NotifHandler as 📧 CreatorApprovedHandler
    participant EmailService as 📬 Email Service
    participant UserEmail as ✉️ User Email

    %% Application Submission
    rect rgb(240, 248, 255)
        Note over User,UserDB: Step 1: User Applies for Content Creator
        User->>Gateway: POST /api/user/creator-applications
        Gateway->>UserAPI: Forward request
        UserAPI->>UserHandler: SubmitCreatorApplicationHandler
        UserHandler->>UserDB: Save CreatorApplication (Status: Pending)
        UserDB-->>UserHandler: Application saved
        UserHandler->>Outbox: Add CreatorApplicationSubmittedEvent
        UserHandler-->>User: 201 Created (ApplicationId)
    end

    %% Background Outbox Processing
    rect rgb(255, 250, 240)
        Note over Outbox,RabbitMQ: Outbox Background Service
        Outbox->>RabbitMQ: Publish CreatorApplicationSubmittedEvent
        Note over RabbitMQ: Event queued for consumers
    end

    %% Staff Approval
    rect rgb(240, 255, 240)
        Note over User,UserDB: Step 2: Staff/Admin Approves Application
        User->>Gateway: PUT /api/user/creator-applications/{id}/approve
        Gateway->>UserAPI: Forward approval request
        UserAPI->>UserHandler: ApproveCreatorApplicationHandler
        
        UserHandler->>UserDB: Find CreatorApplication
        UserDB-->>UserHandler: Application found
        
        UserHandler->>UserDB: Find ContentCreator BusinessRole
        UserDB-->>UserHandler: Role found
        
        UserHandler->>UserDB: Assign role to UserProfile
        UserHandler->>UserDB: Update Application (Status: Approved)
        
        %% Add 3 events to outbox
        UserHandler->>Outbox: 1. CreatorApplicationApprovedEvent
        UserHandler->>Outbox: 2. RoleAddedToUserEvent (for AuthService)
        UserHandler->>Outbox: 3. UserRolesChangedEvent (for Redis cache)
        
        UserHandler->>UserDB: SaveChanges (Transaction)
        UserDB-->>UserHandler: Committed
        UserHandler-->>User: 200 OK (Approval success)
    end

    %% Event Publishing
    rect rgb(255, 245, 245)
        Note over Outbox,RabbitMQ: Step 3: Outbox Publishes Events
        
        Outbox->>RabbitMQ: Publish CreatorApplicationApprovedEvent
        Note over RabbitMQ: Queue: CreatorApplicationApproved
        
        Outbox->>RabbitMQ: Publish RoleAddedToUserEvent
        Note over RabbitMQ: Queue: AuthEvent
        
        Outbox->>RabbitMQ: Publish UserRolesChangedEvent
        Note over RabbitMQ: Queue: UserEvent
        
        Outbox->>UserDB: Mark events as Processed
    end

    %% Role Persistence in AuthService
    rect rgb(255, 250, 245)
        Note over RabbitMQ,AuthDB: Step 4: Role Persistence in AuthService Database
        
        RabbitMQ->>AuthHandler: Consume UserRolesChangedEvent
        Note over AuthHandler: AuthUserRolesChangedEventHandler
        
        AuthHandler->>AuthDB: GetUserRolesAsync(userId)
        AuthDB-->>AuthHandler: Current roles: ["User"]
        
        Note over AuthHandler: ADD-ONLY Logic:<br/>Add new roles, keep existing
        
        AuthHandler->>AuthDB: AddRoleToUserAsync("ContentCreator")
        Note over AuthDB: INSERT INTO AspNetUserRoles<br/>(UserId, RoleId)
        AuthDB-->>AuthHandler: Role added to database
        
        AuthHandler->>Redis: UpdateUserRolesAsync(["User", "ContentCreator"])
        Redis-->>AuthHandler: Cache updated
        
        AuthHandler-->>RabbitMQ: Event processed successfully
    end

    %% Notification
    rect rgb(245, 245, 255)
        Note over RabbitMQ,UserEmail: Step 5: Send Approval Notification Email
        
        RabbitMQ->>NotifHandler: Consume CreatorApplicationApprovedEvent
        Note over NotifHandler: CreatorApplicationApprovedEventHandler
        
        NotifHandler->>NotifHandler: Build email from template
        Note over NotifHandler: Template: CreatorApproved<br/>Subject: Congratulations! 🎉
        
        NotifHandler->>EmailService: SendNotificationAsync(email, template)
        EmailService->>UserEmail: Send HTML email
        Note over UserEmail: "You Are Now a<br/>Content Creator!"
        
        UserEmail-->>EmailService: Email delivered
        EmailService-->>NotifHandler: Success
        NotifHandler-->>RabbitMQ: Event processed (non-critical)
    end

    %% User Re-Login
    rect rgb(250, 250, 255)
        Note over User,Redis: Step 6: User Re-Login (JWT Token Generation)
        
        User->>Gateway: POST /api/auth/login
        Gateway->>UserAPI: Forward to AuthService
        
        UserAPI->>AuthDB: GetUserRolesAsync(userId)
        Note over AuthDB: SELECT * FROM AspNetUserRoles<br/>WHERE UserId = xxx
        AuthDB-->>UserAPI: Roles: ["User", "ContentCreator"]
        
        UserAPI->>UserAPI: Generate JWT Token
        Note over UserAPI: Claims: roles = ["User", "ContentCreator"]
        
        UserAPI->>Redis: Cache user state
        UserAPI-->>User: 200 OK + JWT Token
        
        Note over User: JWT now contains:<br/>✅ User role<br/>✅ ContentCreator role
    end

    %% Success State
    rect rgb(240, 255, 240)
        Note over User,UserEmail: ✅ Final State
        Note over User: Can create podcasts<br/>Has both roles in JWT
        Note over UserEmail: Received congratulations email
        Note over AuthDB: Roles persisted in database
        Note over Redis: Roles cached for quick access
    end
```

## Key Components Flow

```mermaid
flowchart TB
    Start([Staff Approves Creator Application])
    
    Start --> Handler[ApproveCreatorApplicationHandler]
    
    Handler --> DB1[Update Application Status = Approved]
    Handler --> DB2[Assign BusinessRole to UserProfile]
    Handler --> Outbox[Add 3 Events to Outbox]
    
    Outbox --> E1[CreatorApplicationApprovedEvent]
    Outbox --> E2[RoleAddedToUserEvent]
    Outbox --> E3[UserRolesChangedEvent]
    
    E1 --> RMQ1[RabbitMQ Queue: CreatorApplicationApproved]
    E2 --> RMQ2[RabbitMQ Queue: AuthEvent]
    E3 --> RMQ3[RabbitMQ Queue: UserEvent]
    
    RMQ1 --> NH[NotificationService Handler]
    RMQ2 --> AH1[AuthService Handler]
    RMQ3 --> AH2[AuthService Handler - Same]
    
    AH1 --> ADB[Update AspNetUserRoles Table]
    ADB --> ARoles[Add ContentCreator Role to Database]
    ARoles --> ARedis[Update Redis Cache: User + ContentCreator]
    
    NH --> Email[Build Email Template]
    Email --> Send[Send Congratulations Email]
    
    ARedis --> Final1[✅ Roles Persisted in DB]
    Send --> Final2[✅ Email Sent to User]
    
    Final1 --> Login[User Re-Login]
    Login --> JWT[Generate JWT with Both Roles]
    JWT --> Success([✅ User Can Create Podcasts])
    
    style Start fill:#90EE90
    style Success fill:#90EE90
    style Handler fill:#87CEEB
    style NH fill:#FFB6C1
    style AH1 fill:#DDA0DD
    style Email fill:#FFB6C1
    style JWT fill:#FFD700
```

## Role Persistence Architecture

```mermaid
graph TD
    subgraph "UserService"
        A[ApproveCreatorApplicationHandler]
        B[UserService Database]
        C[Outbox Table]
    end
    
    subgraph "Event Bus"
        D[RabbitMQ]
        E[UserRolesChangedEvent]
    end
    
    subgraph "AuthService"
        F[AuthUserRolesChangedEventHandler]
        G[IRoleService]
        H[UserManager & RoleManager]
        I[AuthService Database]
        J[AspNetUserRoles Table]
    end
    
    subgraph "Cache Layer"
        K[Redis]
        L[IUserStateCache]
    end
    
    A -->|1. Add event| C
    C -->|2. Publish| D
    D -->|3. Route| E
    E -->|4. Consume| F
    F -->|5. Get current roles| G
    G -->|6. Query| I
    I -->|7. Return: User| G
    G -->|8. Add new role| H
    H -->|9. INSERT| J
    J -->|10. ContentCreator added| H
    H -->|11. Success| F
    F -->|12. Merge roles| L
    L -->|13. Update| K
    K -->|14. Cached: User + ContentCreator| L
    
    style A fill:#87CEEB
    style F fill:#DDA0DD
    style J fill:#90EE90
    style K fill:#FFB6C1
```

## ADD-ONLY Logic Flow

```mermaid
flowchart TD
    Start([UserRolesChangedEvent Received])
    Start --> Get[Get Current Roles from Database]
    Get --> Current{Current Roles}
    
    Current -->|Example: User| Check[Check New Roles from Event]
    Check -->|Example: ContentCreator| Compare{Role Already Exists?}
    
    Compare -->|No| Add[AddRoleToUserAsync ContentCreator]
    Compare -->|Yes| Skip[Skip - Already Exists]
    
    Add --> Update[✅ Database: User + ContentCreator]
    Skip --> Update
    
    Update --> Merge[Merge All Roles for Cache]
    Merge --> Cache[UpdateUserRolesAsync to Redis]
    Cache --> Done([✅ Both Roles Persisted])
    
    style Start fill:#90EE90
    style Add fill:#87CEEB
    style Update fill:#90EE90
    style Cache fill:#FFB6C1
    style Done fill:#FFD700
```

## Notification System Flow

```mermaid
sequenceDiagram
    participant RMQ as RabbitMQ
    participant Handler as CreatorApprovedHandler
    participant Factory as INotificationFactory
    participant Helper as NotificationTemplateHelper
    participant Email as EmailService
    participant SMTP as SMTP Server
    participant User as User Inbox
    
    RMQ->>Handler: CreatorApplicationApprovedEvent
    
    Note over Handler: Extract event data:<br/>- ApplicationId<br/>- UserEmail<br/>- ApprovedAt<br/>- RoleName
    
    Handler->>Factory: GetSender(Email)
    Factory-->>Handler: EmailService instance
    
    Handler->>Helper: BuildCreatorApprovedNotification()
    Note over Helper: Template: CreatorApproved<br/>Variables: ApplicationId, ApprovedAt, RoleName
    Helper-->>Handler: NotificationRequest (HTML)
    
    Handler->>Email: SendNotificationAsync(request, recipient)
    Note over Email: Fire and forget<br/>(non-blocking)
    
    Email->>SMTP: Send HTML email
    SMTP->>User: Deliver email
    
    Note over User: Subject: Healink - Content Creator Application Approved! 🎉<br/><br/>Body:<br/>- Congratulations message<br/>- Application details<br/>- What's next<br/>- Tips for success
    
    User-->>SMTP: Email received
    SMTP-->>Email: Delivery confirmed
    Email-->>Handler: Success logged
    Handler-->>RMQ: Event processed
```

## Database State Changes

```mermaid
stateDiagram-v2
    [*] --> UserOnly: Initial Registration
    
    state UserOnly {
        [*] --> AspNetUserRoles
        AspNetUserRoles: UserId: xxx
        AspNetUserRoles: RoleId: User
    }
    
    UserOnly --> Processing: Apply for Creator
    
    state Processing {
        [*] --> CreatorApplication
        CreatorApplication: Status: Pending
        CreatorApplication: UserId: xxx
    }
    
    Processing --> RoleAdded: Staff Approves
    
    state RoleAdded {
        [*] --> AspNetUserRoles_Updated
        AspNetUserRoles_Updated: UserId: xxx
        AspNetUserRoles_Updated: RoleId: User ✅
        AspNetUserRoles_Updated: ---
        AspNetUserRoles_Updated: UserId: xxx
        AspNetUserRoles_Updated: RoleId: ContentCreator ✅
        
        [*] --> CreatorApplication_Approved
        CreatorApplication_Approved: Status: Approved ✅
        CreatorApplication_Approved: ReviewedAt: timestamp
    }
    
    RoleAdded --> Cached: Update Redis
    
    state Cached {
        [*] --> RedisCache
        RedisCache: Key: user:xxx:roles
        RedisCache: Value: ["User", "ContentCreator"]
    }
    
    Cached --> [*]: ✅ Both Roles Persisted
```

## Complete System Architecture

```mermaid
graph TB
    subgraph "Client Layer"
        User[👤 User/Staff]
        Browser[🌐 Browser]
    end
    
    subgraph "API Gateway"
        Gateway[Gateway Service]
    end
    
    subgraph "UserService Microservice"
        UserAPI[User API]
        UserHandler[ApproveHandler]
        UserDB[(UserService DB)]
        UserOutbox[Outbox Table]
    end
    
    subgraph "Message Bus"
        RabbitMQ[🐰 RabbitMQ]
        Queue1[CreatorApproved Queue]
        Queue2[AuthEvent Queue]
        Queue3[UserEvent Queue]
    end
    
    subgraph "AuthService Microservice"
        AuthAPI[Auth API]
        AuthHandler[RolesChangedHandler]
        RoleService[IRoleService]
        AuthDB[(AuthService DB)]
        AspNetRoles[AspNetUserRoles]
    end
    
    subgraph "NotificationService Microservice"
        NotifAPI[Notification API]
        NotifHandler[CreatorApprovedHandler]
        EmailSvc[Email Service]
        Templates[Email Templates]
    end
    
    subgraph "Cache Layer"
        Redis[(🔴 Redis)]
    end
    
    subgraph "External Services"
        SMTP[📧 SMTP Server]
        UserInbox[✉️ User Email]
    end
    
    User -->|1. Approve Request| Browser
    Browser -->|2. HTTP| Gateway
    Gateway -->|3. Route| UserAPI
    UserAPI -->|4. Handle| UserHandler
    UserHandler -->|5. Update| UserDB
    UserHandler -->|6. Add Events| UserOutbox
    
    UserOutbox -->|7. Publish| RabbitMQ
    RabbitMQ -->|8. Route| Queue1
    RabbitMQ -->|9. Route| Queue2
    RabbitMQ -->|10. Route| Queue3
    
    Queue2 -->|11. Consume| AuthHandler
    AuthHandler -->|12. Use| RoleService
    RoleService -->|13. Update| AspNetRoles
    AuthHandler -->|14. Cache| Redis
    
    Queue1 -->|15. Consume| NotifHandler
    NotifHandler -->|16. Build| Templates
    NotifHandler -->|17. Send| EmailSvc
    EmailSvc -->|18. SMTP| SMTP
    SMTP -->|19. Deliver| UserInbox
    
    AuthAPI -->|Login Query| AspNetRoles
    AspNetRoles -->|Both Roles| AuthAPI
    AuthAPI -->|JWT Token| Browser
    
    style User fill:#90EE90
    style UserHandler fill:#87CEEB
    style AuthHandler fill:#DDA0DD
    style NotifHandler fill:#FFB6C1
    style Redis fill:#FFB6C1
    style AspNetRoles fill:#90EE90
    style UserInbox fill:#FFD700
```

---

## Summary

**Components:**
- 🔵 **UserService**: Manages creator applications
- 🔐 **AuthService**: Persists roles in database (AspNetUserRoles)
- 📧 **NotificationService**: Sends approval email
- 🐰 **RabbitMQ**: Event bus for async communication
- 🔴 **Redis**: Caches user roles for immediate access

**Key Events:**
1. `CreatorApplicationApprovedEvent` → NotificationService
2. `UserRolesChangedEvent` → AuthService (Database + Redis)

**Critical Fix:**
- **ADD-ONLY logic** in AuthService prevents removing base "User" role
- Both roles persist in database for JWT generation
- Email sent asynchronously (fire-and-forget, non-critical)

**Result:**
✅ User has both "User" and "ContentCreator" roles after re-login  
✅ Roles persist in database permanently  
✅ User receives congratulations email  
✅ User can create podcasts immediately
