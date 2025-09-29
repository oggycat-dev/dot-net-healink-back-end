#!/bin/bash

# ============================================
# HEALINK ENVIRONMENT MANAGER - ALL-IN-ONE
# ============================================
# Single script to manage all environments
# Author: GitHub Copilot
# Version: 3.1

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Show usage
show_usage() {
    echo "🚀 HEALINK ENVIRONMENT MANAGER"
    echo "=============================="
    echo ""
    echo "Usage: $0 <command> [environment]"
    echo ""
    echo "Commands:"
    echo "  create <env>    Create fresh environment (dev/prod)"
    echo "  deploy <env>    Deploy/update environment (dev/prod)" 
    echo "  start <env>     Start existing environment"
    echo "  stop <env>      Stop environment (keep infrastructure)"
    echo "  destroy <env>   Destroy environment completely"
    echo "  status <env>    Show environment status"
    echo "  logs <env>      Show recent logs"
    echo "  config          Generate config files from .env"
    echo ""
    echo "Environments:"
    echo "  dev            Development environment"
    echo "  prod           Production environment"
    echo ""
    echo "Examples:"
    echo "  $0 create dev      # Create dev environment"
    echo "  $0 deploy prod     # Deploy to production"
    echo "  $0 destroy dev     # Clean destroy (save money)"
    echo "  $0 status dev      # Check current status"
    echo "  $0 config          # Generate config files from .env"
    echo ""
}

# Check if we're in the right directory
check_directory() {
    if [ ! -f "docker-compose.yml" ]; then
        print_error "Please run this script from the project root directory"
        exit 1
    fi
}

# Generate configuration files from .env
generate_config() {
    print_status "Generating configuration files from .env..."
    
    # Check if generate-appsettings.sh exists and is executable
    if [ -f "scripts/generate-appsettings.sh" ] && [ -x "scripts/generate-appsettings.sh" ]; then
        ./scripts/generate-appsettings.sh
    else
        print_error "Scripts/generate-appsettings.sh not found or not executable!"
        exit 1
    fi
    
    print_success "✅ Configuration files generated successfully!"
}

# Set Terraform workspace
set_workspace() {
    local env=$1
    cd terraform_healink
    
    print_status "Setting Terraform workspace to: $env"
    terraform workspace select "$env" 2>/dev/null || terraform workspace new "$env"
    
    CURRENT_WORKSPACE=$(terraform workspace show)
    if [ "$CURRENT_WORKSPACE" != "$env" ]; then
        print_error "Failed to set workspace to $env"
        exit 1
    fi
    
    print_success "Using workspace: $CURRENT_WORKSPACE"
}

# Create environment
create_environment() {
    local env=$1
    
    print_status "Creating fresh $env environment..."
    
    # Switch workspace
    set_workspace $env
    
    # Initialize if needed
    terraform init -no-color
    
    # Plan
    print_status "Planning infrastructure..."
    terraform plan -var-file="${env}.tfvars" -out=tfplan -no-color
    
    # Confirm
    echo ""
    read -p "Create $env environment? (yes/no): " confirm
    if [ "$confirm" != "yes" ]; then
        print_warning "Cancelled by user"
        rm -f tfplan
        exit 0
    fi
    
    # Apply
    print_status "Creating infrastructure..."
    terraform apply tfplan -no-color
    
    print_success "✅ $env environment created successfully!"
    
    # Show outputs
    echo ""
    print_status "Environment details:"
    terraform output -no-color
    
    rm -f tfplan
}

# Deploy environment (create or update)
deploy_environment() {
    local env=$1
    
    print_status "Deploying $env environment..."
    
    # Switch workspace
    set_workspace $env
    
    # Initialize
    terraform init -no-color
    
    # Plan
    print_status "Planning deployment..."
    terraform plan -var-file="${env}.tfvars" -out=tfplan -no-color
    
    # Apply
    print_status "Applying changes..."
    terraform apply tfplan -no-color
    
    print_success "✅ $env environment deployed successfully!"
    
    # Show outputs
    terraform output -no-color
    rm -f tfplan
}

# Destroy environment
destroy_environment() {
    local env=$1
    
    print_warning "⚠️  DESTRUCTIVE OPERATION"
    print_warning "This will DESTROY all infrastructure in $env environment"
    
    if [ "$env" = "prod" ]; then
        print_error "🚨 PRODUCTION ENVIRONMENT DESTRUCTION"
        echo ""
        read -p "Type 'DESTROY PRODUCTION' to confirm: " confirm
        if [ "$confirm" != "DESTROY PRODUCTION" ]; then
            print_warning "Cancelled - confirmation text incorrect"
            exit 0
        fi
    else
        echo ""
        read -p "Type 'yes' to destroy $env environment: " confirm
        if [ "$confirm" != "yes" ]; then
            print_warning "Cancelled by user"
            exit 0
        fi
    fi
    
    # Switch workspace  
    set_workspace $env
    
    # Destroy
    print_status "Destroying $env environment..."
    terraform destroy -var-file="${env}.tfvars" -auto-approve -no-color
    
    print_success "✅ $env environment destroyed"
    print_success "💰 Cost savings activated!"
}

# Show environment status
show_status() {
    local env=$1
    
    set_workspace $env
    
    print_status "Environment: $env"
    print_status "Workspace: $(terraform workspace show)"
    
    echo ""
    print_status "Current infrastructure:"
    terraform output -no-color 2>/dev/null || echo "No infrastructure deployed"
}

# Start local development environment
start_local() {
    print_status "Starting local development environment..."
    
    # Generate config files from .env if needed
    generate_config
    
    # Start Docker environment
    print_status "Starting Docker containers..."
    docker-compose up -d
    
    print_success "✅ Local development environment started"
    print_status "Access the API Gateway at: http://localhost:5010"
}

# Stop local development environment
stop_local() {
    print_status "Stopping local development environment..."
    docker-compose down
    print_success "✅ Local development environment stopped"
}

# Main script logic
main() {
    if [ $# -lt 1 ]; then
        show_usage
        exit 1
    fi
    
    check_directory
    
    local command=$1
    local env=${2:-dev}  # Default to dev environment
    
    case $command in
        create)
            # Validate environment
            if [ "$env" != "dev" ] && [ "$env" != "prod" ]; then
                print_error "Invalid environment: $env (use 'dev' or 'prod')"
                exit 1
            fi
            create_environment $env
            ;;
        deploy)
            # Validate environment
            if [ "$env" != "dev" ] && [ "$env" != "prod" ]; then
                print_error "Invalid environment: $env (use 'dev' or 'prod')"
                exit 1
            fi
            deploy_environment $env
            ;;
        destroy)
            # Validate environment
            if [ "$env" != "dev" ] && [ "$env" != "prod" ]; then
                print_error "Invalid environment: $env (use 'dev' or 'prod')"
                exit 1
            fi
            destroy_environment $env
            ;;
        status)
            # Validate environment
            if [ "$env" != "dev" ] && [ "$env" != "prod" ]; then
                print_error "Invalid environment: $env (use 'dev' or 'prod')"
                exit 1
            fi
            show_status $env
            ;;
        start)
            if [ "$env" = "local" ]; then
                start_local
            else
                # Validate environment
                if [ "$env" != "dev" ] && [ "$env" != "prod" ]; then
                    print_error "Invalid environment: $env (use 'dev', 'prod', or 'local')"
                    exit 1
                fi
                print_status "Starting $env environment..."
                deploy_environment $env
            fi
            ;;
        stop)
            if [ "$env" = "local" ]; then
                stop_local
            else
                # Validate environment
                if [ "$env" != "dev" ] && [ "$env" != "prod" ]; then
                    print_error "Invalid environment: $env (use 'dev', 'prod', or 'local')"
                    exit 1
                fi
                print_status "Stopping $env environment..."
                print_warning "Stop functionality not implemented yet for cloud environments"
                print_status "Use 'destroy' to save costs completely"
            fi
            ;;
        logs)
            # Validate environment
            if [ "$env" != "dev" ] && [ "$env" != "prod" ]; then
                print_error "Invalid environment: $env (use 'dev' or 'prod')"
                exit 1
            fi
            print_status "Showing logs for $env environment..."
            print_warning "Logs functionality not implemented yet"
            print_status "Check CloudWatch logs in AWS console"
            ;;
        config)
            generate_config
            ;;
        *)
            print_error "Unknown command: $command"
            show_usage
            exit 1
            ;;
    esac
}

# Run main function
main "$@"