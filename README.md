# Healink Microservices

A distributed microservices system for mental health and wellness platform built with .NET 8 using Clean Architecture and Event-Driven patterns.

## 📋 Overview

Healink Microservices is a comprehensive distributed system consisting of:
- **AuthService**: Authentication, authorization and identity management
- **UserService**: User profile and creator application management
- **ContentService**: Podcast, community stories, and content management
- **SubscriptionService**: Subscription plans and user subscription management with Saga orchestration
- **PaymentService**: Payment processing and invoice management
- **NotificationService**: Email and push notification delivery
- **Gateway**: API Gateway using Ocelot for request routing and load balancing
- **SharedLibrary**: Common utilities, patterns, and configurations shared across all services

## 🏗️ Architecture

```
Healink Microservices
├── Gateway (Port: 5000)
│   └── API Gateway with Ocelot, JWT Authentication
├── AuthService (Port: 5001)
│   ├── API Layer (Controllers, Middlewares)
│   ├── Application Layer (CQRS, Handlers, DTOs, Saga)
│   ├── Domain Layer (Entities, Business Logic)
│   └── Infrastructure Layer (Database, RabbitMQ, Redis)
├── UserService (Port: 5002)
│   ├── API Layer
│   ├── Application Layer (CQRS, Event Consumers)
│   ├── Domain Layer
│   └── Infrastructure Layer (Database, Events)
├── NotificationService (Port: 5003)
│   ├── API Layer
│   ├── Application Layer (Event Consumers)
│   ├── Domain Layer
│   └── Infrastructure Layer (Email Service, Firebase)
├── ContentService (Port: 5004)
│   ├── API Layer
│   ├── Application Layer (CQRS, Content Management)
│   ├── Domain Layer (Podcast, Community, Flashcard entities)
│   └── Infrastructure Layer (Database, AWS S3, Events)
├── SubscriptionService (Port: 5005) ✨ NEW
│   ├── API Layer
│   ├── Application Layer (CQRS, Saga Orchestration)
│   ├── Domain Layer (Subscription, Plans entities)
│   └── Infrastructure Layer (Database, Saga State, Events)
├── PaymentService (Port: 5006) ✨ NEW
│   ├── API Layer
│   ├── Application Layer (CQRS, Payment Processing)
│   ├── Domain Layer (Invoice, Transaction entities)
│   └── Infrastructure Layer (Database, Payment Gateway)
└── SharedLibrary
    ├── Commons (Base entities, Repositories, Services)
    ├── Configurations (JWT, Redis, RabbitMQ, Logging)
    ├── Contracts & Events (Domain events, DTOs)
    ├── Attributes (Authorization, Validation)
    └── Extensions (Migration, Seeding, DI)
```

## 🛠️ Tech Stack

### Core Framework & Language
- **.NET 8**: Latest LTS framework
- **C# 12**: Modern language features

### Database & Caching
- **PostgreSQL 15**: Primary relational database
- **Redis 7**: Distributed caching and session management
- **Entity Framework Core 8**: ORM with code-first migrations

### Messaging & Events
- **RabbitMQ 3.12**: Message broker for Event-driven architecture
- **MassTransit 8.3**: Distributed application framework with Saga support
- **Outbox Pattern**: Ensures reliable event publishing

### API & Gateway
- **Ocelot 23**: API Gateway for routing and load balancing
- **JWT Bearer**: Authentication & Authorization
- **Swagger/OpenAPI**: API Documentation and testing

### Architecture Patterns
- **Clean Architecture**: Domain-centric architecture
- **CQRS**: Command Query Responsibility Segregation with MediatR
- **Event Sourcing**: Event-driven communication
- **Saga Pattern**: Distributed transaction orchestration
- **Repository Pattern**: Data access abstraction
- **Unit of Work**: Transaction management

### Additional Services
- **AWS S3**: Object storage for content files
- **Firebase Cloud Messaging**: Push notifications
- **SMTP**: Email notification delivery

### DevOps & Tools
- **Docker & Docker Compose**: Containerization and orchestration
- **pgAdmin 4**: PostgreSQL administration
- **RabbitMQ Management**: Message broker monitoring
- **Serilog**: Structured logging with distributed tracing

## 🔧 System Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/Users/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) hoặc [VS Code](https://code.visualstudio.com/)

## 🚀 Installation and Setup

### 1. Clone repository

```bash
git clone https://github.com/oggycat-dev/dot-net-healink-back-end.git
cd dot-net-healink-back-end
```

### 2. Environment Setup

The project uses environment-based configuration. Copy the example file:

```bash
cp .env.example .env
```

Key environment variables in `.env`:

```env
# === DATABASE SETTINGS ===
DB_USER=admin
DB_PASSWORD=admin@123
DB_HOST=postgres
DB_PORT=5432

# Service-specific databases
AUTH_DB_NAME=authservicedb
USER_DB_NAME=userservicedb
CONTENT_DB_NAME=contentservicedb
SUBSCRIPTION_DB_NAME=subscriptiondb
PAYMENT_DB_NAME=paymentdb

# === JWT SETTINGS ===
JWT_SECRET_KEY=HealinkMicroserviceSecretKeyIsLongEnoughToBeUsedWithJWT
JWT_ISSUER=Healink
JWT_AUDIENCE=Healink.Users
JWT_EXPIRES_IN_MINUTES=60
JWT_REFRESH_TOKEN_EXPIRES_IN_DAYS=7

# === RABBITMQ SETTINGS ===
RABBITMQ_HOST=rabbitmq
RABBITMQ_PORT=5672
RABBITMQ_USER=admin
RABBITMQ_PASSWORD=admin@123
RABBITMQ_VHOST=/

# === REDIS SETTINGS ===
REDIS_HOST=redis
REDIS_PORT=6379
REDIS_PASSWORD=admin@123

# === EMAIL SETTINGS ===
EMAIL_SMTP_SERVER=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_SENDER_EMAIL=your-email@gmail.com
EMAIL_SENDER_PASSWORD=your-app-password

# === ADMIN ACCOUNT ===
ADMIN_EMAIL=admin@healink.com
ADMIN_PASSWORD=admin@123
```

**Note**: See `.env.example` for complete configuration options.

### 3. Run with Docker Compose (Recommended)

```bash
# Start all services
docker-compose up -d

# View logs for all services
docker-compose logs -f

# View logs for specific service
docker-compose logs -f authservice-api

# Check service health status
docker-compose ps

# Stop services
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

**Services will be available at:**
- Gateway: http://localhost:5000
- AuthService: http://localhost:5001
- UserService: http://localhost:5002
- NotificationService: http://localhost:5003
- ContentService: http://localhost:5004
- SubscriptionService: http://localhost:5005
- PaymentService: http://localhost:5006
- RabbitMQ Management UI: http://localhost:15672
- pgAdmin: http://localhost:5050

### 4. Run in Development Environment

```bash
# Restore packages for all projects
dotnet restore

# Run specific service
cd src/AuthService/AuthService.API
dotnet run

# Or use the helper script
./scripts/local-dev.sh start authservice
```

## 📡 API Endpoints

All services are accessible through the Gateway at `http://localhost:5000`

### Authentication Service (Port: 5001)
**CMS (Admin)**
- `POST /api/cms/auth/login` - Admin login
- `POST /api/cms/auth/logout` - Admin logout
- `POST /api/cms/auth/refresh` - Refresh access token
- `GET /api/cms/auth/profile` - Get admin profile

**User**
- `POST /api/user/auth/register` - User registration
- `POST /api/user/auth/login` - User login
- `POST /api/user/auth/verify-otp` - Verify OTP for registration/password reset
- `POST /api/user/auth/reset-password` - Request password reset
- `POST /api/user/auth/refresh` - Refresh user token

**Health Check**
- `GET /api/auth/health` - Service health status

### User Service (Port: 5002)
**Profile Management**
- `GET /api/user/profile` - Get user profile
- `PUT /api/user/profile` - Update user profile
- `DELETE /api/user/profile` - Delete user account

**Creator Applications**
- `POST /api/creatorapplications` - Submit creator application
- `GET /api/creatorapplications/pending` - Get pending applications (Admin)
- `POST /api/creatorapplications/{id}/approve` - Approve application (Admin)
- `POST /api/creatorapplications/{id}/reject` - Reject application (Admin)

**Health Check**
- `GET /api/users/health` - Service health status

### Content Service (Port: 5004)
**Podcasts**
- `GET /api/content/podcasts` - List podcasts with filters
- `GET /api/content/podcasts/{id}` - Get podcast details
- `POST /api/content/podcasts` - Create podcast (Creator)
- `PUT /api/content/podcasts/{id}` - Update podcast (Creator)
- `DELETE /api/content/podcasts/{id}` - Delete podcast (Creator)

**Community Stories**
- `GET /api/content/community` - List community stories
- `GET /api/content/community/{id}` - Get story details
- `POST /api/content/community` - Create story
- `PUT /api/content/community/{id}` - Update story

**Health Check**
- `GET /api/content/health` - Service health status

### Subscription Service (Port: 5005) ✨ NEW
**Subscription Plans**
- `GET /api/subscriptions/plans` - List available plans
- `GET /api/subscriptions/plans/{id}` - Get plan details

**User Subscriptions**
- `GET /api/subscriptions/my-subscription` - Get current subscription
- `POST /api/subscriptions/subscribe` - Subscribe to a plan
- `POST /api/subscriptions/cancel` - Cancel subscription
- `POST /api/subscriptions/upgrade` - Upgrade subscription plan

**Health Check**
- `GET /api/subscription/health` - Service health status

### Payment Service (Port: 5006) ✨ NEW
**Payments**
- `POST /api/payments/process` - Process payment
- `GET /api/payments/{id}` - Get payment status
- `POST /api/payments/{id}/refund` - Request refund

**Invoices**
- `GET /api/payments/invoices` - List user invoices
- `GET /api/payments/invoices/{id}` - Get invoice details

**Health Check**
- `GET /api/payment/health` - Service health status

### Notification Service (Port: 5003)
**Health Check**
- `GET /api/notification/health` - Service health status

**Note**: NotificationService processes events internally (welcome emails, notifications, etc.)

### Gateway (Port: 5000)
- Routes all requests to appropriate microservices
- Handles authentication via JWT
- Provides unified API documentation
- Swagger UI: http://localhost:5000/swagger

### API Documentation
Each service provides Swagger UI documentation:
- AuthService: http://localhost:5001/swagger
- UserService: http://localhost:5002/swagger
- ContentService: http://localhost:5004/swagger
- SubscriptionService: http://localhost:5005/swagger
- PaymentService: http://localhost:5006/swagger

## 📚 Database Schema

### AuthService Database (`authservicedb`)
- **AppUsers**: User authentication and identity
- **AppRoles**: Role definitions
- **AppUserRoles**: User-role mappings
- **RefreshTokens**: JWT refresh token management
- **OutboxEvents**: Event sourcing for reliable messaging

### UserService Database (`userservicedb`)
- **UserProfiles**: User profile information
- **CreatorApplications**: Creator application workflow
- **OutboxEvents**: Event sourcing

### ContentService Database (`contentservicedb`)
- **Podcasts**: Podcast content and metadata
- **CommunityStories**: User-generated stories
- **Flashcards**: Mental health flashcard content
- **Postcards**: Motivational postcard content
- **EmotionCategories**: Emotion categorization
- **TopicCategories**: Content topic categorization
- **OutboxEvents**: Event sourcing

### SubscriptionService Database (`subscriptiondb`) ✨ NEW
- **SubscriptionPlans**: Available subscription tiers
- **Subscriptions**: User subscription records
- **SubscriptionSagaState**: Saga orchestration state for subscription workflows
- **OutboxEvents**: Event sourcing

### PaymentService Database (`paymentdb`) ✨ NEW
- **Invoices**: Payment invoices
- **PaymentTransactions**: Payment transaction records
- **OutboxEvents**: Event sourcing

**Note**: Each service has its own database following the Database-per-Service pattern.

## 🔒 Security

### Authentication & Authorization
- **JWT Bearer Tokens**: Stateless authentication with access tokens (60 min) and refresh tokens (7 days)
- **Role-Based Access Control (RBAC)**: Admin, User, Creator roles
- **Distributed Authorization**: JWT validation across all microservices via SharedLibrary
- **Token Refresh**: Secure token renewal without re-authentication

### Security Features
- **Password Encryption**: Secure password hashing and encryption
- **OTP Verification**: One-Time Password for registration and password reset
- **CORS Configuration**: Controlled cross-origin resource sharing
- **Rate Limiting**: Request throttling to prevent abuse
- **Input Validation**: FluentValidation for request validation
- **SQL Injection Prevention**: EF Core parameterized queries

### Service Communication
- **Internal Service Auth**: Services communicate securely via RabbitMQ
- **Redis Security**: Password-protected Redis connections
- **Database Security**: Encrypted connection strings stored in environment variables

## 📨 API Testing

### Health Check Testing
Test all services are running correctly:
```powershell
# Run the health check script
.\test-health-checks.ps1
```

### Default Admin Account
- **Email**: admin@healink.com
- **Password**: admin@123

### Using Postman/Thunder Client
1. **Login**: `POST http://localhost:5000/api/user/auth/login`
   ```json
   {
     "email": "admin@healink.com",
     "password": "admin@123"
   }
   ```

2. **Get Access Token** from response

3. **Add Authorization Header** to subsequent requests:
   ```
   Authorization: Bearer <your-access-token>
   ```

4. **Test Authenticated Endpoints**

### Testing Registration Flow
See complete registration flow documentation: [REGISTRATION_SAGA_IMPLEMENTATION.md](docs/REGISTRATION_SAGA_IMPLEMENTATION.md)

### API Documentation
- **Gateway Swagger**: http://localhost:5000/swagger
- **Individual Service Swagger**: http://localhost:500X/swagger (where X = service port)

### Example Requests
See `test-registration.json` and service-specific `.http` files in each API project.

## 🐳 Docker Services

| Service | Container Name | Port(s) | Description | Health Check |
|---------|---------------|---------|-------------|--------------|
| **Gateway** | gateway-api | 5000 | API Gateway (Ocelot) | N/A |
| **AuthService** | authservice-api | 5001 | Authentication & Identity | ✅ /health |
| **UserService** | userservice-api | 5002 | User Profile Management | ✅ /health |
| **NotificationService** | notificationservice-api | 5003 | Email & Push Notifications | ✅ /api/health |
| **ContentService** | contentservice-api | 5004 | Content Management | ✅ /api/health |
| **SubscriptionService** | subscriptionservice-api | 5005 | Subscription Management | ✅ /api/health |
| **PaymentService** | paymentservice-api | 5006 | Payment Processing | ✅ /api/health |
| **PostgreSQL** | healink-postgres | 5432 | Relational Database | ✅ pg_isready |
| **Redis** | healink-redis | 6379 | Cache & Session Store | ✅ redis-cli ping |
| **RabbitMQ** | healink-rabbitmq | 5672, 15672 | Message Broker & UI | ✅ rabbitmq-diagnostics |
| **pgAdmin** | healink-pgadmin | 5050 | Database Admin UI | N/A |

### Service Dependencies
- **AuthService**: PostgreSQL, RabbitMQ, Redis
- **UserService**: PostgreSQL, RabbitMQ, Redis
- **ContentService**: PostgreSQL, RabbitMQ, Redis, AWS S3
- **SubscriptionService**: PostgreSQL, RabbitMQ, Redis
- **PaymentService**: PostgreSQL, RabbitMQ, Redis
- **NotificationService**: RabbitMQ, SMTP Server
- **Gateway**: AuthService, UserService, Redis

## 📊 Monitoring & Health Checks

### Health Check Endpoints
All services provide health check endpoints for monitoring:

**Via Gateway:**
- http://localhost:5000/api/auth/health
- http://localhost:5000/api/users/health
- http://localhost:5000/api/content/health
- http://localhost:5000/api/subscription/health
- http://localhost:5000/api/payment/health
- http://localhost:5000/api/notification/health

**Direct Service Access:**
- AuthService: http://localhost:5001/health
- UserService: http://localhost:5002/health
- ContentService: http://localhost:5004/api/health
- SubscriptionService: http://localhost:5005/api/health
- PaymentService: http://localhost:5006/api/health
- NotificationService: http://localhost:5003/api/health

### Management UIs
- **RabbitMQ Management**: http://localhost:15672
  - Username: admin
  - Password: admin@123
  - Monitor queues, exchanges, and message flow
  
- **pgAdmin**: http://localhost:5050
  - Email: admin@healink.com
  - Password: admin@123
  - Manage all PostgreSQL databases

### Docker Container Monitoring
```bash
# Check all container health status
docker-compose ps

# View container logs
docker-compose logs -f <service-name>

# Check specific service health
docker inspect <container-name> --format='{{.State.Health.Status}}'
```

### Logging
- **Structured Logging**: Serilog with JSON formatting
- **Distributed Tracing**: Correlation IDs across services
- **Log Levels**: Information, Warning, Error, Debug
- **Log Outputs**: Console and File (with rotation)

For detailed health check documentation, see: [HEALTH_CHECK_ENDPOINTS.md](HEALTH_CHECK_ENDPOINTS.md)

## 🔄 Event-Driven Architecture

The system uses **RabbitMQ with MassTransit** for asynchronous, reliable communication between services:

### Event Flow Examples

#### 1. User Registration Saga
```
User Registration Request
  ↓
AuthService (Saga Orchestrator)
  ↓
Events Published:
  - UserCreated → UserService (Create Profile)
  - UserProfileCreated → SubscriptionService (Create Free Subscription)
  - UserProfileCreated → NotificationService (Send Welcome Email)
```

#### 2. Subscription Purchase
```
Subscription Purchase Request
  ↓
SubscriptionService (Saga Orchestrator)
  ↓
Events Published:
  - PaymentRequired → PaymentService (Process Payment)
  - PaymentCompleted → SubscriptionService (Activate Subscription)
  - SubscriptionActivated → NotificationService (Send Confirmation)
```

### Key Events
- **Auth Events**: UserCreated, UserAuthenticated, PasswordReset
- **User Events**: UserProfileCreated, UserProfileUpdated, CreatorApplicationSubmitted
- **Subscription Events**: SubscriptionCreated, SubscriptionActivated, SubscriptionCancelled
- **Payment Events**: PaymentProcessed, PaymentCompleted, PaymentFailed, RefundIssued
- **Notification Events**: WelcomeEmailSent, NotificationSent

### Saga Pattern Implementation
- **Registration Saga**: Orchestrates user registration across Auth, User, Subscription, and Notification services
- **Subscription Saga**: Manages subscription lifecycle with payment coordination
- **Compensation Logic**: Automatic rollback on failure (e.g., payment failed → cancel subscription)

### Outbox Pattern
Each service uses the **Outbox Pattern** to ensure reliable event publishing:
1. Events saved to OutboxEvents table in same transaction
2. Background processor publishes events to RabbitMQ
3. Guarantees at-least-once delivery
4. Prevents data loss during failures

For detailed saga implementation: [REGISTRATION_SAGA_IMPLEMENTATION.md](docs/REGISTRATION_SAGA_IMPLEMENTATION.md)

## 🧪 Testing

### Build and Test
```bash
# Build entire solution
dotnet build HealinkMicroservices.sln

# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific service tests
dotnet test src/AuthService/AuthService.Tests
```

### Integration Testing
```bash
# Start infrastructure services only
docker-compose up -d postgres redis rabbitmq

# Run integration tests
dotnet test --filter Category=Integration
```

### Health Check Testing
```powershell
# Test all service health endpoints
.\test-health-checks.ps1
```

### Manual Testing
- Use Swagger UI at http://localhost:5000/swagger
- Import `.http` files in VS Code with REST Client extension
- Test registration flow with `test-registration.json`

## 📝 Logging & Observability

### Structured Logging with Serilog
- **JSON Format**: Structured log output for easy parsing
- **Correlation IDs**: Distributed tracing across services
- **Log Enrichment**: User context, service name, timestamps

### Log Levels
- **Information**: Normal application flow
- **Warning**: Unexpected but handled situations
- **Error**: Failures and exceptions
- **Debug**: Detailed diagnostic information (dev only)

### Log Configuration
Configure logging via environment variables in `.env`:
```env
LOG_ENABLE_FILE_LOGGING=true
LOG_ENABLE_CONSOLE_LOGGING=true
LOG_ENABLE_DISTRIBUTED_TRACING=true
LOG_MINIMUM_LEVEL=Information
```

### Viewing Logs
```bash
# View all service logs
docker-compose logs -f

# View specific service
docker-compose logs -f authservice-api

# Follow new logs only
docker-compose logs -f --tail=100 authservice-api
```

### Distributed Tracing
- Each request gets a unique **Correlation ID**
- Trace requests across multiple services
- Example log entry:
  ```json
  {
    "Timestamp": "2025-10-01T12:00:00Z",
    "Level": "Information",
    "Service": "AuthService",
    "CorrelationId": "abc-123-xyz",
    "Message": "User authenticated successfully",
    "UserId": "user-guid"
  }
  ```

For detailed logging configuration: [logging-system-documentation.md](docs/logging/logging-system-documentation.md)

## 🤝 Contributing

We welcome contributions! Please follow these guidelines:

### Getting Started
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Code Standards
- Follow Clean Architecture principles
- Use CQRS pattern for new features
- Write unit tests for business logic
- Update documentation for API changes
- Follow C# coding conventions
- Use meaningful commit messages

### Pull Request Process
1. Update README.md with details of changes
2. Update API documentation if needed
3. Ensure all tests pass
4. Request review from maintainers

### Development Workflow
See: [PROFESSIONAL_WORKFLOW.md](PROFESSIONAL_WORKFLOW.md)

## 📋 Roadmap

### ✅ Completed
- [x] Core microservices architecture (6 services)
- [x] Event-driven communication with RabbitMQ & MassTransit
- [x] Saga pattern for distributed transactions
- [x] JWT authentication & authorization
- [x] Redis distributed caching
- [x] Docker containerization
- [x] Health check endpoints
- [x] Structured logging with distributed tracing
- [x] User registration with OTP verification
- [x] Creator application workflow
- [x] Subscription management with saga orchestration
- [x] Payment processing integration

### 🚧 In Progress
- [ ] Unit test coverage for all services
- [ ] Integration test suite
- [ ] API rate limiting and throttling
- [ ] Content recommendation engine
- [ ] Advanced search and filtering

### 📅 Planned
- [ ] Monitoring with Prometheus & Grafana
- [ ] CI/CD pipeline with GitHub Actions
- [ ] Kubernetes deployment manifests
- [ ] API versioning strategy
- [ ] GraphQL gateway
- [ ] Elasticsearch for content search
- [ ] WebSocket support for real-time features
- [ ] Mobile app push notifications (FCM)
- [ ] Content CDN integration
- [ ] Advanced analytics and reporting

### 🔮 Future Considerations
- [ ] Multi-language support (i18n)
- [ ] Service mesh (Istio)
- [ ] Event store for complete event history
- [ ] Machine learning for content recommendations
- [ ] A/B testing framework
- [ ] Multi-region deployment

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📚 Documentation

### Architecture & Design
- [Registration Saga Implementation](docs/REGISTRATION_SAGA_IMPLEMENTATION.md)
- [Registration Flow Diagrams](docs/register-user/REGISTRATION_FLOW_DIAGRAMS.md)
- [Content Service Sequence Diagram](docs/content-services/ContentService_Sequence_Diagram.md)
- [Microservice Structure](docs/graph-diagram-microservice-structure-v1.png)

### Configuration & Setup
- [Health Check Endpoints](HEALTH_CHECK_ENDPOINTS.md)
- [Health Check Complete Guide](HEALTH_CHECK_COMPLETE.md)
- [Subscription & Payment Services Summary](SUBSCRIPTION_PAYMENT_SERVICES_SUMMARY.md)
- [Local Development Guide](LOCAL_DEVELOPMENT.md)
- [Professional Workflow](PROFESSIONAL_WORKFLOW.md)

### Service-Specific Guides
- [Creator Application Flow](CREATOR_APPLICATION_FLOW_GUIDE.md)
- [Logging System Documentation](docs/logging/logging-system-documentation.md)
- [Service Configuration Summary](docs/logging/service-configuration-summary.md)

### Integration & Success Reports
- [Integration Success Report](INTEGRATION_SUCCESS_REPORT.md)
- [Final Success Report](FINAL_SUCCESS_REPORT.md)

## 🙏 Acknowledgments

### Architecture & Patterns
- **Clean Architecture** by Robert C. Martin
- **Domain-Driven Design** principles by Eric Evans
- **Microservices Patterns** by Chris Richardson
- **Event-Driven Architecture** best practices

### Technologies & Frameworks
- **.NET Team** for the amazing .NET 8 framework
- **MassTransit** for distributed application framework
- **Ocelot** for API Gateway
- **Serilog** for structured logging
- **Entity Framework Core** team

### Community
- Open source contributors
- .NET community for guidance and support
- Stack Overflow community

---

## 📞 Contact & Support

- **Project Repository**: [oggycat-dev/dot-net-healink-back-end](https://github.com/oggycat-dev/dot-net-healink-back-end)
- **Issues**: Please use GitHub Issues for bug reports and feature requests
- **Support Email**: healinksupport@gmail.com

---

**Built with ❤️ using .NET 8 and Clean Architecture**

**Last Updated**: October 1, 2025