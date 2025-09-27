#!/bin/bash

# ==============================================
# HEALINK LOCAL DEVELOPMENT MANAGER
# ==============================================
# Quick local development with Docker Compose
# Author: GitHub Copilot

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

print_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

show_usage() {
    echo "🐳 HEALINK LOCAL DEVELOPMENT MANAGER"
    echo "===================================="
    echo ""
    echo "Usage: $0 <command> [service]"
    echo ""
    echo "Commands:"
    echo "  start [service]     Start all services or specific service"
    echo "  stop [service]      Stop all services or specific service"
    echo "  restart [service]   Restart all services or specific service"
    echo "  rebuild [service]   Rebuild and restart service"
    echo "  logs [service]      Show logs for all or specific service"
    echo "  status              Show status of all services"
    echo "  clean               Clean up all containers and volumes"
    echo "  reset               Complete reset (clean + rebuild)"
    echo "  urls                Show all service URLs"
    echo "  create-service      Create new microservice template"
    echo ""
    echo "Services:"
    echo "  postgres           PostgreSQL database"
    echo "  rabbitmq           RabbitMQ message broker"
    echo "  redis              Redis cache"
    echo "  authservice-api    Authentication service"
    echo "  userservice-api    User management service"
    echo "  gateway-api        API Gateway"
    echo "  pgadmin            PostgreSQL admin interface"
    echo ""
    echo "Examples:"
    echo "  $0 start                    # Start all services"
    echo "  $0 restart authservice-api  # Restart auth service"
    echo "  $0 logs userservice-api     # Show user service logs"
    echo "  $0 rebuild gateway-api      # Rebuild gateway"
    echo "  $0 urls                     # Show all URLs"
    echo ""
}

check_docker() {
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed"
        exit 1
    fi
    
    if ! docker compose version &> /dev/null; then
        print_error "Docker Compose is not available"
        exit 1
    fi
}

check_env_file() {
    if [ ! -f ".env" ]; then
        print_warning ".env file not found, creating from docker-compose.example.yml"
        
        if [ -f "docker-compose.example.yml" ]; then
            print_info "Please check .env file and update values as needed"
            # Create basic .env file
            cat > .env << EOF
# Database Configuration
DB_USER=healink_user
DB_PASSWORD=healink_password_2024
DB_PORT=5432
AUTH_DB_NAME=healink_auth_db
USER_DB_NAME=healink_user_db

# RabbitMQ Configuration
RABBITMQ_USER=healink_mq
RABBITMQ_PASSWORD=healink_mq_password_2024
RABBITMQ_EXCHANGE=healink.exchange

# Redis Configuration
REDIS_PASSWORD=healink_redis_password_2024
REDIS_PORT=6379

# JWT Configuration
JWT_SECRET_KEY=HealinkJWTSecretKeyForDevelopmentEnvironmentOnly2024
JWT_ISSUER=Healink.Development
JWT_AUDIENCE=Healink.Users

# Admin Account
ADMIN_EMAIL=admin@healink.dev
ADMIN_PASSWORD=Admin@123
EOF
            print_success ".env file created with default values"
        else
            print_error "No template file found to create .env"
            exit 1
        fi
    fi
}

start_services() {
    local service=$1
    
    check_env_file
    
    if [ -n "$service" ]; then
        print_info "Starting service: $service"
        docker compose up -d $service
    else
        print_info "Starting all services..."
        docker compose up -d
    fi
    
    print_success "Services started"
    show_urls
}

stop_services() {
    local service=$1
    
    if [ -n "$service" ]; then
        print_info "Stopping service: $service"
        docker compose stop $service
    else
        print_info "Stopping all services..."
        docker compose down
    fi
    
    print_success "Services stopped"
}

restart_services() {
    local service=$1
    
    if [ -n "$service" ]; then
        print_info "Restarting service: $service"
        docker compose restart $service
    else
        print_info "Restarting all services..."
        docker compose restart
    fi
    
    print_success "Services restarted"
}

rebuild_service() {
    local service=$1
    
    if [ -z "$service" ]; then
        print_error "Service name is required for rebuild"
        exit 1
    fi
    
    print_info "Rebuilding service: $service"
    docker compose build --no-cache $service
    docker compose up -d $service
    
    print_success "Service $service rebuilt and restarted"
}

show_logs() {
    local service=$1
    
    if [ -n "$service" ]; then
        print_info "Showing logs for: $service"
        docker compose logs -f $service
    else
        print_info "Showing logs for all services"
        docker compose logs -f
    fi
}

show_status() {
    print_info "Service status:"
    docker compose ps
}

clean_all() {
    print_warning "This will remove all containers and volumes"
    read -p "Are you sure? (y/N): " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Cleaning up..."
        docker compose down -v --remove-orphans
        docker system prune -f
        print_success "Cleanup completed"
    else
        print_info "Cleanup cancelled"
    fi
}

reset_all() {
    print_warning "This will completely reset the development environment"
    read -p "Are you sure? (y/N): " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Resetting environment..."
        docker compose down -v --remove-orphans
        docker compose build --no-cache
        docker compose up -d
        print_success "Environment reset completed"
        show_urls
    else
        print_info "Reset cancelled"
    fi
}

show_urls() {
    echo ""
    print_success "🌐 Service URLs:"
    echo "================================"
    echo "🚪 Gateway API:      http://localhost:5000"
    echo "🔐 Auth Service:     http://localhost:5001"
    echo "📦 User Service:  http://localhost:5002"
    echo "🐰 RabbitMQ Admin:   http://localhost:15672"
    echo "🗄️  PostgreSQL Admin: http://localhost:5050"
    echo "📊 Redis Commander:  http://localhost:8081"
    echo ""
    echo "📋 Default Credentials:"
    echo "RabbitMQ: healink_mq / healink_mq_password_2024"
    echo "pgAdmin:  admin@healink.dev / Admin@123"
    echo ""
}

create_new_service() {
    echo ""
    print_info "🚀 Creating new microservice template"
    echo "======================================"
    
    read -p "Enter service name (e.g., order-service): " service_name
    
    if [ -z "$service_name" ]; then
        print_error "Service name is required"
        exit 1
    fi
    
    # Convert to different formats
    service_kebab=$(echo "$service_name" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]/-/g')
    service_pascal=$(echo "$service_name" | sed 's/-/ /g' | sed 's/\b\w/\U&/g' | sed 's/ //g')
    service_port_base=50
    
    # Find next available port
    while docker compose ps | grep -q ":${service_port_base}0[0-9]:80"; do
        service_port_base=$((service_port_base + 1))
    done
    
    service_port="${service_port_base}03"
    
    print_info "Creating ${service_pascal}Service..."
    print_info "Port: ${service_port}"
    
    # Create service directory structure
    mkdir -p "src/${service_pascal}Service"
    mkdir -p "src/${service_pascal}Service/${service_pascal}Service.API"
    mkdir -p "src/${service_pascal}Service/${service_pascal}Service.Application"
    mkdir -p "src/${service_pascal}Service/${service_pascal}Service.Domain"
    mkdir -p "src/${service_pascal}Service/${service_pascal}Service.Infrastructure"
    
    # Create basic Dockerfile
    cat > "src/${service_pascal}Service/${service_pascal}Service.API/Dockerfile" << EOF
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_ENVIRONMENT=Development

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/${service_pascal}Service/${service_pascal}Service.API/${service_pascal}Service.API.csproj", "${service_pascal}Service/${service_pascal}Service.API/"]
COPY ["src/${service_pascal}Service/${service_pascal}Service.Application/${service_pascal}Service.Application.csproj", "${service_pascal}Service/${service_pascal}Service.Application/"]
COPY ["src/${service_pascal}Service/${service_pascal}Service.Infrastructure/${service_pascal}Service.Infrastructure.csproj", "${service_pascal}Service/${service_pascal}Service.Infrastructure/"]
COPY ["src/${service_pascal}Service/${service_pascal}Service.Domain/${service_pascal}Service.Domain.csproj", "${service_pascal}Service/${service_pascal}Service.Domain/"]
COPY ["src/Shared/Shared.csproj", "Shared/"]

# Restore dependencies
RUN dotnet restore "${service_pascal}Service/${service_pascal}Service.API/${service_pascal}Service.API.csproj"

# Copy source code
COPY src/ .

# Build
WORKDIR "/src/${service_pascal}Service/${service_pascal}Service.API"
RUN dotnet build "${service_pascal}Service.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "${service_pascal}Service.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN echo '#!/bin/bash\\nset -e\\necho "Starting ${service_pascal}Service..."\\nexec dotnet ${service_pascal}Service.API.dll' > /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

ENTRYPOINT ["/app/entrypoint.sh"]
EOF

    # Add to docker-compose.yml
    print_info "Adding to docker-compose.yml..."
    
    cat >> docker-compose.yml << EOF

  # ${service_pascal}Service API  
  ${service_kebab}-api:
    build:
      context: .
      dockerfile: ./src/${service_pascal}Service/${service_pascal}Service.API/Dockerfile
    container_name: ${service_kebab}-api
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:80
      ConnectionConfig__DefaultConnection: "Host=postgres;Database=\${${service_pascal^^}_DB_NAME};Username=\${DB_USER};Password=\${DB_PASSWORD}"
      RabbitMQ__HostName: rabbitmq
      RabbitMQ__UserName: \${RABBITMQ_USER}
      RabbitMQ__Password: \${RABBITMQ_PASSWORD}
      RabbitMQ__ExchangeName: \${RABBITMQ_EXCHANGE}
      JwtConfig__Key: \${JWT_SECRET_KEY}
      JwtConfig__Issuer: \${JWT_ISSUER}
      JwtConfig__Audience: \${JWT_AUDIENCE}
      Redis__ConnectionString: "redis:\${REDIS_PORT},password=\${REDIS_PASSWORD}"
    ports:
      - "${service_port}:80"
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
      redis:
        condition: service_healthy
    networks:
      - healink-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
EOF

    # Add environment variable to .env
    echo "${service_pascal^^}_DB_NAME=healink_${service_kebab//-/_}_db" >> .env
    
    print_success "✅ ${service_pascal}Service template created!"
    echo ""
    echo "📁 Created directories:"
    echo "   src/${service_pascal}Service/"
    echo ""
    echo "🐳 Added to docker-compose.yml:"
    echo "   Service: ${service_kebab}-api"
    echo "   Port: ${service_port}"
    echo ""
    echo "🔧 Next steps:"
    echo "   1. Implement your service logic in src/${service_pascal}Service/"
    echo "   2. Create .csproj files for each layer"
    echo "   3. Run: $0 rebuild ${service_kebab}-api"
    echo "   4. Test: http://localhost:${service_port}"
    echo ""
}

# Main script
main() {
    if [ $# -lt 1 ]; then
        show_usage
        exit 1
    fi
    
    check_docker
    
    local command=$1
    local service=$2
    
    case $command in
        start)
            start_services $service
            ;;
        stop)
            stop_services $service
            ;;
        restart)
            restart_services $service
            ;;
        rebuild)
            rebuild_service $service
            ;;
        logs)
            show_logs $service
            ;;
        status)
            show_status
            ;;
        clean)
            clean_all
            ;;
        reset)
            reset_all
            ;;
        urls)
            show_urls
            ;;
        create-service)
            create_new_service
            ;;
        *)
            print_error "Unknown command: $command"
            show_usage
            exit 1
            ;;
    esac
}

main "$@"