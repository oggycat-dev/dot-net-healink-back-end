# ProductAuth Microservices

A distributed microservices system for product management and user authentication built with .NET 8 using Clean Architecture.

## 📋 Overview

ProductAuth Microservices is a distributed system consisting of:
- **AuthService**: Authentication and user management service
- **ProductService**: Product and category management service
- **Gateway**: API Gateway using Ocelot for request routing
- **Shared**: Common library for all services

## 🏗️ Architecture

```
ProductAuth Microservices
├── AuthService (Port: 5001)
│   ├── API Layer (Controllers, Middlewares)
│   ├── Application Layer (CQRS, Handlers, DTOs)
│   ├── Domain Layer (Entities, Business Logic)
│   └── Infrastructure Layer (Database, External Services)
├── ProductService (Port: 5002)
│   ├── API Layer
│   ├── Application Layer
│   ├── Domain Layer
│   └── Infrastructure Layer
├── Gateway (Port: 5000)
│   └── API Gateway with Ocelot
└── Shared
    ├── Common Utilities
    ├── Contracts & Events
    └── Configurations
```

## 🛠️ Tech Stack

- **.NET 8**: Main framework
- **PostgreSQL**: Primary database
- **Redis**: Distributed caching
- **RabbitMQ**: Message broker for Event-driven architecture
- **Entity Framework Core**: ORM
- **Ocelot**: API Gateway
- **JWT**: Authentication & Authorization
- **MediatR**: CQRS pattern
- **Docker & Docker Compose**: Containerization
- **Swagger/OpenAPI**: API Documentation

## 🔧 System Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) hoặc [VS Code](https://code.visualstudio.com/)

## 🚀 Installation and Setup

### 1. Clone repository

```bash
git clone <repository-url>
cd ProductAuthMicroservice
```

### 2. Environment Setup

Create `.env` file from `.env.example`:

```bash
cp .env.example .env
```

Configure environment variables in `.env` file:

```env
# Database Configuration
DB_USER=postgres
DB_PASSWORD=your_password
DB_PORT=5432
AUTH_DB_NAME=AuthServiceDB
PRODUCT_DB_NAME=ProductServiceDB

# RabbitMQ Configuration
RABBITMQ_USER=admin
RABBITMQ_PASSWORD=your_password
RABBITMQ_PORT=5672
RABBITMQ_VHOST=/
RABBITMQ_EXCHANGE=ProductAuthExchange

# Redis Configuration
REDIS_PASSWORD=your_password
REDIS_PORT=6379

# JWT Configuration
JWT_SECRET_KEY=your_super_secret_key_here
JWT_ISSUER=ProductAuthMicroservice
JWT_AUDIENCE=ProductAuthMicroservice

# Admin Account
ADMIN_EMAIL=admin@productauth.com
ADMIN_PASSWORD=Admin@123
```

### 3. Run with Docker Compose

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

### 4. Run in Development Environment

```bash
# Restore packages
dotnet restore

# Run each service separately
cd src/AuthService/AuthService.API
dotnet run

cd src/ProductService/ProductService.API
dotnet run

cd src/Gateway/Gateway.API
dotnet run
```

## 📡 API Endpoints

### Authentication Service (Port: 5001)
- `POST /api/cms/auth/login` - User login
- `POST /api/cms/auth/logout` - User logout
- `POST /api/cms/auth/refresh` - Refresh token
- Swagger UI: http://localhost:5001/swagger

### Product Service (Port: 5002)
- `GET /api/cms/products` - Get products list
- `GET /api/cms/products/{id}` - Get product by ID
- `POST /api/cms/products` - Create new product
- `PUT /api/cms/products/{id}` - Update product
- `DELETE /api/cms/products/{id}` - Delete product
- `GET /api/cms/categories` - Category management
- Swagger UI: http://localhost:5002/swagger

### Gateway (Port: 5000)
- All endpoints are proxied through Gateway
- Health checks: `/health`, `/health/auth`, `/health/products`
- Swagger UI: http://localhost:5000/swagger

## 📚 Database Schema

### AuthService Database
- **Users**: User management
- **Roles**: Role management
- **UserRoles**: User-role mapping
- **RefreshTokens**: Refresh token management

### ProductService Database
- **Products**: Product information
- **Categories**: Product categories
- **OutboxEvents**: Event sourcing pattern

## 🔒 Security

- **JWT Authentication**: Access token (60 minutes) and Refresh token (7 days)
- **Role-based Authorization**: Permission based on roles
- **CORS**: Cross-Origin Resource Sharing configuration
- **Rate Limiting**: Request rate limiting
- **Input Validation**: Input data validation

## 📨 API Testing

### Using Postman
1. Import collection from `postman/` folder
2. Setup environment variables
3. Execute workflow: Login → Create Category → Create Product

See details at: [Postman Documentation](postman/README.md)

### Default Account
- **Email**: admin@productauth.com
- **Password**: Admin@123

## 🐳 Docker Services

| Service | Port | Description |
|---------|------|-------------|
| Gateway | 5000 | API Gateway |
| AuthService | 5001 | Authentication Service |
| ProductService | 5002 | Product Management Service |
| PostgreSQL | 5432 | Database |
| Redis | 6379 | Cache |
| RabbitMQ | 5672 | Message Broker |
| RabbitMQ Management | 15672 | RabbitMQ UI |
| pgAdmin | 5050 | Database Admin UI |

## 📊 Monitoring & Health Checks

- **Health Checks**: `/health` endpoints for each service
- **RabbitMQ Management**: http://localhost:15672
- **pgAdmin**: http://localhost:5050

## 🔄 Event-Driven Architecture

The system uses RabbitMQ for asynchronous communication between services:
- **Product Events**: Create/update/delete products
- **User Events**: Register/update user information
- **Outbox Pattern**: Ensures consistency in distributed transactions

## 🧪 Testing

```bash
# Run unit tests
dotnet test

# Run integration tests
dotnet test --filter Category=Integration

# Coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## 📝 Logging

- **Structured Logging**: Using Serilog
- **Log Levels**: Information, Warning, Error
- **Log Storage**: Console and File

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## 📋 Roadmap

- [ ] Implement unit tests
- [ ] Add integration tests
- [ ] Implement caching strategies
- [ ] Add monitoring with Prometheus & Grafana
- [ ] Implement CI/CD pipeline
- [ ] Add notification service
- [ ] Implement file upload service

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.


## 🙏 Acknowledgments

- Clean Architecture pattern
- Domain-Driven Design principles
- CQRS & Event Sourcing patterns
- Microservices best practices
# microservices-dot-net-template
## Test