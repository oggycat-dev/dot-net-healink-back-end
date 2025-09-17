#!/bin/bash

# ===================================
# Database Migration Script for Healink Microservices
# Supports environment variable-based configuration
# ===================================

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Function to check if environment variables are set
check_env_vars() {
    local service_name=$1
    local required_vars=()
    
    if [ -z "${!service_name}_DB_CONNECTION_STRING" ]; then
        required_vars+=("DB_HOST" "DB_USER" "DB_PASSWORD" "${service_name}_DB_NAME")
    fi
    
    for var in "${required_vars[@]}"; do
        if [ -z "${!var}" ]; then
            print_error "Required environment variable '$var' is not set"
            return 1
        fi
    done
    
    return 0
}

# Function to run migration for a service
run_migration() {
    local service_name=$1
    local action=$2
    local migration_name=$3
    
    print_info "Running $action for $service_name Service..."
    
    # Check environment variables
    if ! check_env_vars "$service_name"; then
        print_error "Environment variables check failed for $service_name"
        return 1
    fi
    
    # Set working directory
    local service_dir="src/${service_name}Service/${service_name}Service.Infrastructure"
    local startup_project="../${service_name}Service.API"
    
    if [ ! -d "$service_dir" ]; then
        print_error "Service directory not found: $service_dir"
        return 1
    fi
    
    cd "$service_dir"
    
    case $action in
        "add")
            if [ -z "$migration_name" ]; then
                print_error "Migration name is required for 'add' action"
                return 1
            fi
            print_info "Adding migration: $migration_name"
            dotnet ef migrations add "$migration_name" --project . --startup-project "$startup_project"
            ;;
        "update")
            print_info "Applying migrations to database"
            dotnet ef database update --project . --startup-project "$startup_project"
            ;;
        "script")
            print_info "Generating SQL script"
            dotnet ef migrations script --project . --startup-project "$startup_project"
            ;;
        "list")
            print_info "Listing migrations"
            dotnet ef migrations list --project . --startup-project "$startup_project"
            ;;
        "remove")
            print_info "Removing last migration"
            dotnet ef migrations remove --project . --startup-project "$startup_project"
            ;;
        *)
            print_error "Unknown action: $action"
            print_info "Available actions: add, update, script, list, remove"
            return 1
            ;;
    esac
    
    cd - > /dev/null
    print_success "$action completed for $service_name Service"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [service] [action] [migration_name]"
    echo ""
    echo "Services:"
    echo "  auth      - Auth Service"
    echo "  user      - User Service"
    echo "  all       - All Services (only for update action)"
    echo ""
    echo "Actions:"
    echo "  add       - Add new migration (requires migration_name)"
    echo "  update    - Apply migrations to database"
    echo "  script    - Generate SQL script"
    echo "  list      - List all migrations"
    echo "  remove    - Remove last migration"
    echo ""
    echo "Examples:"
    echo "  $0 auth add InitialCreate"
    echo "  $0 user update"
    echo "  $0 all update"
    echo "  $0 auth script > migrations.sql"
    echo ""
    echo "Environment Variables:"
    echo "  Required: DB_HOST, DB_USER, DB_PASSWORD, AUTH_DB_NAME, USER_DB_NAME"
    echo "  Or: AUTH_DB_CONNECTION_STRING, USER_DB_CONNECTION_STRING"
}

# Main script logic
main() {
    local service=$1
    local action=$2
    local migration_name=$3
    
    if [ $# -lt 2 ]; then
        show_usage
        exit 1
    fi
    
    # Load .env file if it exists
    if [ -f ".env" ]; then
        print_info "Loading environment variables from .env file"
        source .env
    fi
    
    # Validate environment variables
    print_info "Validating environment variables..."
    if ! ./scripts/validate-env.sh > /dev/null 2>&1; then
        print_warning "Environment validation failed, but continuing..."
        print_info "Make sure required database environment variables are set"
    fi
    
    case $service in
        "auth")
            run_migration "Auth" "$action" "$migration_name"
            ;;
        "user")
            run_migration "User" "$action" "$migration_name"
            ;;
        "all")
            if [ "$action" = "update" ]; then
                print_info "Running migrations for all services..."
                run_migration "Auth" "$action" && run_migration "User" "$action"
            else
                print_error "The 'all' service option is only supported for 'update' action"
                exit 1
            fi
            ;;
        *)
            print_error "Unknown service: $service"
            show_usage
            exit 1
            ;;
    esac
    
    print_success "Migration process completed successfully!"
}

# Check if dotnet ef is installed
if ! command -v dotnet > /dev/null 2>&1; then
    print_error "dotnet CLI is not installed or not in PATH"
    exit 1
fi

if ! dotnet ef > /dev/null 2>&1; then
    print_error "dotnet ef tools are not installed"
    print_info "Install with: dotnet tool install --global dotnet-ef"
    exit 1
fi

# Run main function with all arguments
main "$@"
