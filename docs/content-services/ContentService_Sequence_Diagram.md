# ContentService Sequence Diagrams

## 1. Create Podcast Flow (Complete CQRS with S3 Upload)

```mermaid
sequenceDiagram
    participant Client as Client App
    participant API as PodcastsController
    participant Auth as Authorization
    participant Validator as FluentValidator
    participant Mediator as MediatR
    participant Handler as CreatePodcastHandler
    participant S3 as S3FileStorageService
    participant Repo as ContentRepository
    participant DB as PostgreSQL
    participant Outbox as OutboxUnitOfWork
    participant BG as OutboxEventProcessor

    Client->>+API: POST /podcasts (multipart/form-data)
    Note over Client,API: Audio file + metadata
    
    API->>+Auth: Authorize [ContentCreator]
    Auth-->>-API: Authorization Success
    
    API->>+Validator: Validate CreatePodcastDto
    Validator-->>-API: Validation Success
    
    API->>+Mediator: Send CreatePodcastCommand
    Mediator->>+Handler: Handle CreatePodcastCommand
    
    Handler->>+S3: UploadFileAsync(audioFile)
    S3-->>-Handler: S3 URL + Metadata
    
    Handler->>Handler: Create Podcast Entity
    Note over Handler: Set ContentStatus = Draft
    
    Handler->>+Repo: CreateAsync(podcast)
    Repo->>+DB: INSERT INTO Contents
    DB-->>-Repo: Podcast Created
    Repo-->>-Handler: Entity with ID
    
    Handler->>+Outbox: PublishEventAsync(ContentCreatedEvent)
    Outbox->>+DB: INSERT INTO OutboxEvents
    DB-->>-Outbox: Event Stored
    Outbox-->>-Handler: Event Queued
    
    Handler-->>-Mediator: PodcastResponseDto
    Mediator-->>-API: Response
    API-->>-Client: 201 Created + PodcastDto
    
    Note over BG: Background Processing
    BG->>+Outbox: GetPendingEventsAsync()
    Outbox->>+DB: SELECT * FROM OutboxEvents
    DB-->>-Outbox: Pending Events
    Outbox-->>-BG: Events List
    
    BG->>BG: Process ContentCreatedEvent
    Note over BG: Notify other services
```

## 2. Approve/Reject Podcast Flow (Moderator Action)

```mermaid
sequenceDiagram
    participant Moderator as Community Moderator
    participant API as PodcastsController
    participant Auth as Authorization
    participant Mediator as MediatR
    participant Handler as ApprovePodcastHandler
    participant Repo as ContentRepository
    participant DB as PostgreSQL
    participant Outbox as OutboxUnitOfWork
    participant BG as OutboxEventProcessor

    Moderator->>+API: PUT /podcasts/{id}/approve
    
    API->>+Auth: Authorize [CommunityModerator]
    Auth-->>-API: Authorization Success
    
    API->>+Mediator: Send ApprovePodcastCommand
    Mediator->>+Handler: Handle ApprovePodcastCommand
    
    Handler->>+Repo: GetByIdAsync(podcastId)
    Repo->>+DB: SELECT * FROM Contents WHERE Id = ?
    DB-->>-Repo: Podcast Entity
    Repo-->>-Handler: Podcast
    
    Handler->>Handler: Update ContentStatus = Approved
    Handler->>Handler: Set ApprovedBy & ApprovedAt
    
    Handler->>+Repo: UpdateAsync(podcast)
    Repo->>+DB: UPDATE Contents SET ContentStatus = 'Approved'
    DB-->>-Repo: Updated
    Repo-->>-Handler: Success
    
    Handler->>+Outbox: PublishEventAsync(PodcastApprovedEvent)
    Outbox->>+DB: INSERT INTO OutboxEvents
    DB-->>-Outbox: Event Stored
    Outbox-->>-Handler: Event Queued
    
    Handler-->>-Mediator: Success
    Mediator-->>-API: Success
    API-->>-Moderator: 200 OK
    
    BG->>+Outbox: Process PodcastApprovedEvent
    Outbox-->>-BG: Event Published to Message Bus
```

## 3. Get Podcasts with Filtering (Query Side)

```mermaid
sequenceDiagram
    participant Client as Client App
    participant API as PodcastsController
    participant Auth as Authorization (Optional)
    participant Mediator as MediatR
    participant Handler as GetPodcastsQueryHandler
    participant Repo as ContentRepository
    participant DB as PostgreSQL
    participant Cache as Redis (Future)

    Client->>+API: GET /podcasts?status=approved&page=1&size=10
    
    opt Authentication Required
        API->>+Auth: Validate JWT Token
        Auth-->>-API: User Context
    end
    
    API->>+Mediator: Send GetPodcastsQuery
    Mediator->>+Handler: Handle GetPodcastsQuery
    
    opt Cache Check (Future Enhancement)
        Handler->>+Cache: Check Cache Key
        Cache-->>-Handler: Cache Miss
    end
    
    Handler->>+Repo: GetPodcastsAsync(filters, pagination)
    Repo->>+DB: SELECT * FROM Contents WHERE ContentType = 'Podcast'
    Note over Repo,DB: Apply soft delete filter (IsDeleted = false)
    Note over Repo,DB: Apply status filter, pagination, sorting
    DB-->>-Repo: Podcast Results
    Repo-->>-Handler: Paginated Podcasts
    
    Handler->>Handler: Map to PodcastResponseDto[]
    
    opt Cache Store (Future Enhancement)
        Handler->>+Cache: Store Results
        Cache-->>-Handler: Cached
    end
    
    Handler-->>-Mediator: PagedResult<PodcastResponseDto>
    Mediator-->>-API: Paginated Response
    API-->>-Client: 200 OK + Podcast List
```

## 4. Delete Podcast Flow (Soft Delete with S3 Cleanup)

```mermaid
sequenceDiagram
    participant User as Content Creator
    participant API as PodcastsController
    participant Auth as Authorization
    participant Mediator as MediatR
    participant Handler as DeletePodcastHandler
    participant Repo as ContentRepository
    participant DB as PostgreSQL
    participant S3 as S3FileStorageService
    participant Outbox as OutboxUnitOfWork

    User->>+API: DELETE /podcasts/{id}
    
    API->>+Auth: Authorize [ContentCreator]
    Auth->>Auth: Verify User Owns Podcast
    Auth-->>-API: Authorization Success
    
    API->>+Mediator: Send DeletePodcastCommand
    Mediator->>+Handler: Handle DeletePodcastCommand
    
    Handler->>+Repo: GetByIdAsync(podcastId)
    Repo->>+DB: SELECT * FROM Contents WHERE Id = ?
    DB-->>-Repo: Podcast Entity
    Repo-->>-Handler: Podcast with AudioUrl
    
    Handler->>Handler: Verify Ownership (CreatedBy)
    
    Handler->>+S3: DeleteFileAsync(audioUrl)
    S3-->>-Handler: File Deleted
    
    Handler->>Handler: Set IsDeleted = true, DeletedAt = Now
    
    Handler->>+Repo: UpdateAsync(podcast)
    Repo->>+DB: UPDATE Contents SET IsDeleted = true
    DB-->>-Repo: Updated
    Repo-->>-Handler: Success
    
    Handler->>+Outbox: PublishEventAsync(ContentDeletedEvent)
    Outbox->>+DB: INSERT INTO OutboxEvents
    DB-->>-Outbox: Event Stored
    Outbox-->>-Handler: Event Queued
    
    Handler-->>-Mediator: Success
    Mediator-->>-API: Success
    API-->>-User: 204 No Content
```

## 5. Comment System Flow

```mermaid
sequenceDiagram
    participant User as Authenticated User
    participant API as CommentsController
    participant Auth as Authorization
    participant Mediator as MediatR
    participant Handler as CreateCommentHandler
    participant ContentRepo as ContentRepository
    participant CommentRepo as CommentRepository
    participant DB as PostgreSQL
    participant Outbox as OutboxUnitOfWork

    User->>+API: POST /contents/{contentId}/comments
    
    API->>+Auth: Authorize (Authenticated User)
    Auth-->>-API: User Context
    
    API->>+Mediator: Send CreateCommentCommand
    Mediator->>+Handler: Handle CreateCommentCommand
    
    Handler->>+ContentRepo: ExistsAsync(contentId)
    ContentRepo->>+DB: SELECT COUNT(*) FROM Contents WHERE Id = ?
    DB-->>-ContentRepo: Exists = true
    ContentRepo-->>-Handler: Content Exists
    
    Handler->>Handler: Create Comment Entity
    Note over Handler: Set IsApproved = false (requires moderation)
    
    Handler->>+CommentRepo: CreateAsync(comment)
    CommentRepo->>+DB: INSERT INTO Comments
    DB-->>-CommentRepo: Comment Created
    CommentRepo-->>-Handler: Comment with ID
    
    Handler->>+ContentRepo: IncrementCommentCount(contentId)
    ContentRepo->>+DB: UPDATE Contents SET CommentCount = CommentCount + 1
    DB-->>-ContentRepo: Updated
    ContentRepo-->>-Handler: Success
    
    Handler->>+Outbox: PublishEventAsync(CommentCreatedEvent)
    Outbox->>+DB: INSERT INTO OutboxEvents
    DB-->>-Outbox: Event Stored
    Outbox-->>-Handler: Event Queued
    
    Handler-->>-Mediator: CommentResponseDto
    Mediator-->>-API: Response
    API-->>-User: 201 Created + CommentDto
```

## 6. Error Handling Flow

```mermaid
sequenceDiagram
    participant Client as Client App
    participant API as PodcastsController
    participant Validator as FluentValidator
    participant Mediator as MediatR
    participant Handler as CreatePodcastHandler
    participant S3 as S3FileStorageService
    participant Exception as Exception Handler

    Client->>+API: POST /podcasts (invalid data)
    
    API->>+Validator: Validate CreatePodcastDto
    Validator->>Validator: Check Required Fields
    Validator->>Validator: Validate File Size/Type
    Validator-->>-API: ValidationException
    
    API->>+Exception: Handle ValidationException
    Exception-->>-API: 400 Bad Request + Validation Errors
    API-->>-Client: 400 + Error Details
    
    Note over Client,API: Alternative Flow - S3 Upload Failure
    
    Client->>+API: POST /podcasts (valid data)
    API->>+Mediator: Send CreatePodcastCommand
    Mediator->>+Handler: Handle Command
    
    Handler->>+S3: UploadFileAsync(audioFile)
    S3-->>-Handler: S3Exception (network/auth error)
    
    Handler->>+Exception: Handle S3Exception
    Exception-->>-Handler: Wrapped ApplicationException
    Handler-->>-Mediator: ApplicationException
    Mediator-->>-API: 500 Internal Server Error
    
    API->>+Exception: Handle ApplicationException
    Exception-->>-API: Structured Error Response
    API-->>-Client: 500 + Error Message
```

## 7. Background Outbox Event Processing

```mermaid
sequenceDiagram
    participant Timer as Background Timer
    participant Processor as OutboxEventProcessor
    participant Outbox as OutboxUnitOfWork
    participant DB as PostgreSQL
    participant Bus as EventBus/MessageBus
    participant External as External Services

    Timer->>+Processor: Trigger (every 30 seconds)
    
    Processor->>+Outbox: GetPendingOutboxEventsAsync(batchSize: 10)
    Outbox->>+DB: SELECT TOP 10 FROM OutboxEvents WHERE ProcessedAt IS NULL
    DB-->>-Outbox: Pending Events
    Outbox-->>-Processor: Event Batch
    
    loop For Each Event in Batch
        Processor->>+Bus: PublishAsync(event)
        
        alt Successful Publish
            Bus-->>-Processor: Success
            Processor->>+Outbox: MarkAsProcessedAsync(eventId)
            Outbox->>+DB: UPDATE OutboxEvents SET ProcessedAt = NOW()
            DB-->>-Outbox: Updated
            Outbox-->>-Processor: Marked as Processed
        else Publish Failed
            Bus-->>-Processor: Exception
            Processor->>+Outbox: IncrementRetryCountAsync(eventId)
            Outbox->>+DB: UPDATE OutboxEvents SET RetryCount++, NextRetryAt = ?
            DB-->>-Outbox: Updated
            Outbox-->>-Processor: Retry Scheduled
        end
    end
    
    Processor-->>-Timer: Batch Completed
    
    Note over External: External services receive events:
    Note over External: - UserService (for notifications)
    Note over External: - NotificationService (for alerts)
    Note over External: - AnalyticsService (for metrics)
```

## Architecture Notes

### Key Components:
1. **API Layer**: Controllers with attribute-based routing and authorization
2. **Application Layer**: CQRS with MediatR, FluentValidation, AutoMapper
3. **Domain Layer**: Entities, enums, interfaces, domain events
4. **Infrastructure Layer**: EF Core, S3 integration, repositories, outbox pattern

### Design Patterns Used:
- **CQRS**: Separate command/query responsibilities
- **Mediator Pattern**: Decoupled request/response handling
- **Repository Pattern**: Data access abstraction
- **Outbox Pattern**: Reliable event publishing
- **Clean Architecture**: Dependency inversion and separation of concerns

### Key Features:
- **Role-based Authorization**: ContentCreator, CommunityModerator policies
- **Soft Delete**: IsDeleted filter applied automatically via EF query filters
- **File Upload**: Direct S3 integration with cleanup on delete
- **Event-driven**: Async event publishing for microservice communication
- **Validation**: Comprehensive input validation with FluentValidation
- **Error Handling**: Structured exception handling with appropriate HTTP status codes