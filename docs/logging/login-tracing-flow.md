# Login Request Flow với Distributed Tracing

## Mermaid Chart - Login Request Flow

```mermaid
sequenceDiagram
    participant Client as Frontend Client
    participant Gateway as API Gateway
    participant AuthService as Auth Service
    participant DB as PostgreSQL
    participant Redis as Redis Cache
    participant RabbitMQ as RabbitMQ

    Note over Client, RabbitMQ: Login Request với Distributed Tracing

    Client->>Gateway: POST /api/cms/auth/login
    Note right of Gateway: CorrelationId: abc123-456-789
    
    Gateway->>Gateway: UseCorrelationId() Middleware
    Note right of Gateway: Extract/Generate CorrelationId: abc123-456-789
    
    Gateway->>Gateway: DistributedAuthMiddleware
    Note right of Gateway: Log: "Processing request with CorrelationId: abc123-456-789"
    
    Gateway->>AuthService: Forward login request
    Note right of Gateway: Add X-Correlation-ID header
    
    AuthService->>AuthService: UseCorrelationId() Middleware
    Note right of AuthService: Extract CorrelationId: abc123-456-789
    
    AuthService->>AuthService: LoginCommandHandler.Handle()
    Note right of AuthService: Log: "Processing login for user: user@example.com"<br/>CorrelationId: abc123-456-789
    
    AuthService->>AuthService: IIdentityService.AuthenticateAsync()
    Note right of AuthService: Log: "Authenticating user credentials"<br/>CorrelationId: abc123-456-789
    
    AuthService->>DB: Query user by email & validate password
    Note right of AuthService: Log: "User authentication successful"<br/>CorrelationId: abc123-456-789
    
    DB-->>AuthService: User data
    Note right of AuthService: EF Core logs DISABLED (LogLevel.None)
    
    AuthService->>AuthService: Generate JWT & Refresh tokens
    Note right of AuthService: Log: "JWT tokens generated successfully"<br/>CorrelationId: abc123-456-789
    
    AuthService->>DB: Update user login info
    Note right of AuthService: Log: "User login info updated"<br/>CorrelationId: abc123-456-789
    
    AuthService->>Redis: Cache user state
    Note right of AuthService: Log: "User state cached in Redis"<br/>CorrelationId: abc123-456-789
    
    Redis-->>AuthService: Cache success
    
    AuthService->>RabbitMQ: Publish UserLoggedInEvent
    Note right of AuthService: Log: "Event published: UserLoggedInEvent"<br/>CorrelationId: abc123-456-789
    
    RabbitMQ->>AuthService: Event published
    Note right of AuthService: Log: "Outbox event processed successfully"<br/>CorrelationId: abc123-456-789
    
    AuthService-->>Gateway: Login response with JWT
    Note right of AuthService: Log: "User {UserId} logged in successfully"<br/>CorrelationId: abc123-456-789
    
    Gateway-->>Client: Login success response
    Note right of Gateway: Log: "Request completed successfully"<br/>CorrelationId: abc123-456-789
```

## Key Components

### 1. **CorrelationIdMiddleware**
- **Extract** Correlation ID từ request headers
- **Generate** new Correlation ID nếu không có
- **Add** Correlation ID vào response headers
- **Log** với Correlation ID context

### 2. **DistributedAuthMiddleware (Gateway)**
- **Validate** JWT token
- **Log** authentication events với Correlation ID
- **Forward** request với Correlation ID header

### 3. **LoginCommandHandler (AuthService)**
- **Authenticate** user credentials via IIdentityService
- **Generate** JWT và Refresh tokens
- **Update** user login information in database
- **Cache** user state in Redis
- **Publish** UserLoggedInEvent via Outbox pattern

### 4. **OutboxUnitOfWork**
- **Add** UserLoggedInEvent to outbox
- **Save** changes with transaction
- **Publish** event to RabbitMQ after successful save
- **Log** event publishing process

### 5. **RedisUserStateCache**
- **Cache** user state for distributed auth
- **Store** user roles, status, refresh token
- **Enable** fast authentication checks

### 6. **Service Logging**
- **All services** log với Correlation ID
- **EF Core logs** disabled (LogLevel.None)
- **File logging** per service
- **Console logging** với Correlation ID

### 7. **Event Propagation**
- **RabbitMQ events** include Correlation ID
- **Cross-service** communication tracked
- **End-to-end** request tracing

## Log Output Examples

### Gateway Logs
```
info: Gateway.API.Middlewares.CorrelationIdMiddleware[0]
      Processing request with CorrelationId: abc123-456-789

info: Gateway.API.Middlewares.DistributedAuthMiddleware[0]
      JWT token validated successfully for user: user@example.com
      CorrelationId: abc123-456-789
```

### AuthService Logs
```
info: SharedLibrary.Commons.Middlewares.CorrelationIdMiddleware[0]
      Processing request with CorrelationId: abc123-456-789

info: AuthService.Application.Features.Auth.Commands.Login.LoginCommandHandler[0]
      Processing login for user: user@example.com
      CorrelationId: abc123-456-789

info: AuthService.Application.Features.Auth.Commands.Login.LoginCommandHandler[0]
      User authentication successful for user: user@example.com
      CorrelationId: abc123-456-789

info: AuthService.Application.Features.Auth.Commands.Login.LoginCommandHandler[0]
      JWT tokens generated successfully for user: user@example.com
      CorrelationId: abc123-456-789

info: AuthService.Application.Features.Auth.Commands.Login.LoginCommandHandler[0]
      User state cached in Redis for user: user@example.com
      CorrelationId: abc123-456-789

info: SharedLibrary.Commons.Outbox.OutboxUnitOfWork[0]
      Publishing outbox event {EventId} of type UserLoggedInEvent
      CorrelationId: abc123-456-789

info: AuthService.Application.Features.Auth.Commands.Login.LoginCommandHandler[0]
      User {UserId} logged in successfully
      CorrelationId: abc123-456-789
```

## Benefits

1. **Complete Request Tracing** - Track requests across all services
2. **Debugging Support** - Easy to trace issues across services
3. **Performance Monitoring** - Track request duration across services
4. **Error Tracking** - Correlate errors with specific requests
5. **Audit Trail** - Complete log trail for compliance
