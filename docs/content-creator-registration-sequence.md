# Content Creator Registration Sequence Diagram

## Luồng đăng ký làm Content Creator

```mermaid
sequenceDiagram
    participant Frontend as Frontend
    participant Gateway as API Gateway<br/>(Ocelot)
    participant UserService as UserService.API
    participant Controller as CreatorApplicationsController
    participant Handler as SubmitCreatorApplicationHandler
    participant UoW as UnitOfWork
    participant DB as PostgreSQL
    participant EventBus as RabbitMQ EventBus
    participant NotificationService as NotificationService

    Note over Frontend, NotificationService: User đăng ký làm Content Creator

    Frontend->>Gateway: POST /api/user/CreatorApplications<br/>Authorization: Bearer JWT_TOKEN<br/>Body: {experience, portfolio, motivation, socialMedia, additionalInfo}
    
    Gateway->>UserService: Forward request to UserService.API
    
    UserService->>Controller: SubmitApplication(SubmitCreatorApplicationCommand)
    
    Note over Controller: Extract UserId from JWT token
    Controller->>Controller: User.FindFirst(ClaimTypes.NameIdentifier)
    Controller->>Controller: request.UserId = userId
    
    Controller->>Handler: _mediator.Send(request)
    
    Note over Handler: Validate and process application
    Handler->>UoW: Check existing pending application
    UoW->>DB: SELECT * FROM CreatorApplications<br/>WHERE UserId = userProfile.Id AND Status = 'Pending'
    DB-->>UoW: Return existing application (if any)
    UoW-->>Handler: Return result
    
    alt Existing pending application found
        Handler-->>Controller: Return error: "Bạn đã có một đơn đăng ký đang chờ duyệt"
        Controller-->>UserService: 400 Bad Request
        UserService-->>Gateway: 400 Bad Request
        Gateway-->>Frontend: 400 Bad Request
    else No existing application
        Handler->>UoW: Get user profile
        UoW->>DB: SELECT * FROM UserProfiles<br/>WHERE UserId = request.UserId
        DB-->>UoW: Return user profile
        UoW-->>Handler: Return userProfile
        
        Handler->>UoW: Get ContentCreator role
        UoW->>DB: SELECT * FROM BusinessRoles<br/>WHERE RoleType = 'ContentCreator'
        DB-->>UoW: Return ContentCreator role
        UoW-->>Handler: Return contentCreatorRole
        
        Note over Handler: Create application data
        Handler->>Handler: Create applicationData dictionary<br/>{experience, portfolio, motivation, socialMedia, additionalInfo}
        
        Note over Handler: Create CreatorApplication entity
        Handler->>Handler: new CreatorApplication {<br/>  UserId = userProfile.Id,<br/>  ApplicationData = JsonSerializer.Serialize(applicationData),<br/>  ApplicationStatus = ApplicationStatusEnum.Pending,<br/>  SubmittedAt = DateTime.UtcNow,<br/>  RequestedBusinessRoleId = contentCreatorRole.Id<br/>}
        
        Handler->>UoW: Add creator application
        UoW->>DB: INSERT INTO CreatorApplications<br/>(Id, UserId, ApplicationData, ApplicationStatus, SubmittedAt, RequestedBusinessRoleId)
        DB-->>UoW: Application saved
        
        Handler->>UoW: Save changes
        UoW->>DB: COMMIT TRANSACTION
        DB-->>UoW: Transaction committed
        UoW-->>Handler: Changes saved
        
        Note over Handler: Publish event after successful commit
        Handler->>Handler: Create CreatorApplicationSubmittedEvent
        Handler->>EventBus: PublishAsync(applicationEvent)
        EventBus-->>Handler: Event published
        
        Handler-->>Controller: Return SubmitCreatorApplicationResponse<br/>{Success: true, ApplicationId, Status: "Pending", SubmittedAt}
        Controller-->>UserService: 200 OK
        UserService-->>Gateway: 200 OK
        Gateway-->>Frontend: 200 OK
        
        Note over EventBus, NotificationService: Event processing (async)
        EventBus->>NotificationService: CreatorApplicationSubmittedEvent
        NotificationService->>NotificationService: Process application notification<br/>(Send email to admin, etc.)
    end

    Note over Frontend, NotificationService: User kiểm tra trạng thái đơn đăng ký

    Frontend->>Gateway: GET /api/user/CreatorApplications/my-status<br/>Authorization: Bearer JWT_TOKEN
    
    Gateway->>UserService: Forward request to UserService.API
    
    UserService->>Controller: GetMyApplicationStatus()
    
    Note over Controller: Extract UserId from JWT token
    Controller->>Controller: User.FindFirst(ClaimTypes.NameIdentifier)
    Controller->>Controller: userId = Guid.Parse(userIdClaim)
    
    Controller->>Handler: _mediator.Send(GetMyApplicationStatusQuery { UserId = userId })
    
    Handler->>UoW: Get UserProfile
    UoW->>DB: SELECT * FROM UserProfiles<br/>WHERE UserId = request.UserId
    DB-->>UoW: Return user profile
    UoW-->>Handler: Return userProfile
    
    Handler->>UoW: Get applications for user
    UoW->>DB: SELECT * FROM CreatorApplications<br/>WHERE UserId = userProfile.Id<br/>ORDER BY SubmittedAt DESC
    DB-->>UoW: Return user applications
    UoW-->>Handler: Return userApplications
    
    alt No applications found
        Handler-->>Controller: Return null
        Controller-->>UserService: 404 Not Found<br/>"Bạn chưa nộp đơn đăng ký Content Creator nào"
        UserService-->>Gateway: 404 Not Found
        Gateway-->>Frontend: 404 Not Found
    else Applications found
        Handler->>Handler: Parse application data from JSON
        Handler->>Handler: Create MyApplicationStatusDto<br/>{ApplicationId, Status, StatusDescription, SubmittedAt, etc.}
        Handler-->>Controller: Return MyApplicationStatusDto
        Controller-->>UserService: 200 OK
        UserService-->>Gateway: 200 OK
        Gateway-->>Frontend: 200 OK
    end
```

## Các thành phần chính trong luồng:

### 1. **Frontend**
- Gửi POST request với thông tin đăng ký
- Gửi GET request để kiểm tra trạng thái

### 2. **API Gateway (Ocelot)**
- Route `/api/user/CreatorApplications` → UserService.API
- Route `/api/user/CreatorApplications/my-status` → UserService.API

### 3. **UserService.API**
- `CreatorApplicationsController.SubmitApplication()`
- `CreatorApplicationsController.GetMyApplicationStatus()`

### 4. **Application Layer**
- `SubmitCreatorApplicationHandler`
- `GetMyApplicationStatusHandler`

### 5. **Infrastructure Layer**
- `UnitOfWork` (Repository pattern)
- PostgreSQL Database
- RabbitMQ EventBus

### 6. **External Services**
- NotificationService (xử lý event async)

## Các bước chính:

1. **Validation**: Kiểm tra đơn đăng ký đã tồn tại chưa
2. **Data Processing**: Tạo application data và entity
3. **Persistence**: Lưu vào database với transaction
4. **Event Publishing**: Publish event sau khi commit thành công
5. **Status Retrieval**: Tìm và trả về trạng thái đơn đăng ký

## Lưu ý quan trọng:

- **JWT Token**: User ID được trích xuất từ JWT token
- **Foreign Key**: Sử dụng `userProfile.Id` thay vì JWT User ID để tránh lỗi foreign key constraint
- **Transaction**: Commit database trước khi publish event
- **Error Handling**: Xử lý các trường hợp lỗi và validation
