#!/bin/bash

# Healink Terraform Deploy Script
# Simple workflow for two-layer infrastructure

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if terraform is installed
check_terraform() {
    if ! command -v terraform &> /dev/null; then
        log_error "Terraform is not installed. Please install it first."
        exit 1
    fi
}

# Show help
show_help() {
    echo "Healink Terraform Deploy Script"
    echo ""
    echo "Usage: $0 <command> [environment]"
    echo ""
    echo "Commands:"
    echo "  stateful-init        Initialize stateful infrastructure"
    echo "  stateful-plan        Plan stateful infrastructure"
    echo "  stateful-apply       Deploy stateful infrastructure"
    echo "  stateful-destroy     Destroy stateful infrastructure (DANGEROUS!)"
    echo ""
    echo "  app-init            Initialize app infrastructure"
    echo "  app-plan            Plan app infrastructure"
    echo "  app-apply           Deploy app infrastructure"
    echo "  app-destroy         Destroy app infrastructure (Safe for testing)"
    echo ""
    echo "  quick-deploy        Deploy both layers (stateful + app)"
    echo "  quick-test          Destroy & recreate app layer"
    echo "  status              Show current status"
    echo "  clean               Clean all caches and restart"
    echo ""
    echo "Environment (optional): dev, prod (default: dev)"
    echo ""
    echo "Examples:"
    echo "  $0 quick-deploy dev      # Deploy everything to dev"
    echo "  $0 app-destroy          # Destroy app layer (safe)"
    echo "  $0 quick-test           # Test cycle: destroy + redeploy app"
    echo "  $0 status               # Check what's running"
}

# Set environment
ENV=${2:-dev}
log_info "Using environment: $ENV"

# Stateful infrastructure commands
stateful_init() {
    log_info "Initializing stateful infrastructure..."
    cd stateful-infra
    terraform workspace select $ENV 2>/dev/null || terraform workspace new $ENV
    terraform init
    log_success "Stateful infrastructure initialized"
}

stateful_plan() {
    log_info "Planning stateful infrastructure..."
    cd stateful-infra
    terraform workspace select $ENV
    terraform plan
}

stateful_apply() {
    log_info "Applying stateful infrastructure..."
    cd stateful-infra
    terraform workspace select $ENV
    terraform apply
    log_success "Stateful infrastructure deployed"
}

stateful_destroy() {
    log_warning "⚠️  DANGEROUS: This will destroy DATABASE and all data!"
    echo "Type 'yes' to confirm:"
    read confirmation
    if [ "$confirmation" = "yes" ]; then
        log_info "Destroying stateful infrastructure..."
        cd stateful-infra
        terraform workspace select $ENV
        terraform destroy
        log_success "Stateful infrastructure destroyed"
    else
        log_info "Cancelled"
    fi
}

# App infrastructure commands
app_init() {
    log_info "Initializing app infrastructure..."
    cd app-infra
    terraform workspace select $ENV 2>/dev/null || terraform workspace new $ENV
    terraform init
    log_success "App infrastructure initialized"
}

app_plan() {
    log_info "Planning app infrastructure..."
    cd app-infra
    terraform workspace select $ENV
    terraform plan
}

app_apply() {
    log_info "Applying app infrastructure..."
    cd app-infra
    terraform workspace select $ENV
    terraform apply
    log_success "App infrastructure deployed"
    echo ""
    log_info "Getting service URLs..."
    terraform output
}

app_destroy() {
    log_info "Destroying app infrastructure (safe operation)..."
    cd app-infra
    terraform workspace select $ENV
    terraform destroy
    log_success "App infrastructure destroyed"
}

# Quick commands
quick_deploy() {
    log_info "🚀 Quick deploy: stateful + app infrastructure"
    stateful_init
    stateful_apply
    app_init
    app_apply
    log_success "🎉 Full deployment completed!"
}

quick_test() {
    log_info "🧪 Quick test: destroy + recreate app layer"
    app_destroy
    app_apply
    log_success "🎉 Test cycle completed!"
}

status() {
    log_info "Checking terraform status..."
    echo ""
    echo "=== STATEFUL INFRASTRUCTURE ==="
    if [ -d "stateful-infra/.terraform" ]; then
        cd stateful-infra
        terraform workspace list
        echo "Current workspace: $(terraform workspace show)"
        terraform output 2>/dev/null || echo "No outputs (not deployed)"
        cd ..
    else
        echo "Not initialized"
    fi
    
    echo ""
    echo "=== APP INFRASTRUCTURE ==="
    if [ -d "app-infra/.terraform" ]; then
        cd app-infra
        terraform workspace list
        echo "Current workspace: $(terraform workspace show)"
        terraform output 2>/dev/null || echo "No outputs (not deployed)"
        cd ..
    else
        echo "Not initialized"
    fi
}

clean() {
    log_warning "Cleaning all terraform caches..."
    rm -rf stateful-infra/.terraform stateful-infra/.terraform.lock.hcl
    rm -rf app-infra/.terraform app-infra/.terraform.lock.hcl
    log_success "Cleaned. Run 'init' commands to restart."
}

# Main script
check_terraform

case $1 in
    stateful-init)
        stateful_init
        ;;
    stateful-plan)
        stateful_plan
        ;;
    stateful-apply)
        stateful_apply
        ;;
    stateful-destroy)
        stateful_destroy
        ;;
    app-init)
        app_init
        ;;
    app-plan)
        app_plan
        ;;
    app-apply)
        app_apply
        ;;
    app-destroy)
        app_destroy
        ;;
    quick-deploy)
        quick_deploy
        ;;
    quick-test)
        quick_test
        ;;
    status)
        status
        ;;
    clean)
        clean
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        log_error "Unknown command: $1"
        echo ""
        show_help
        exit 1
        ;;
esac