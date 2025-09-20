#!/bin/bash

# ============================================
# HEALINK MODULAR DEPLOYMENT SCRIPT
# ============================================
# This script demonstrates the new module-based approach
# Author: GitHub Copilot
# Version: 2.0

set -e

echo "🚀 HEALINK MODULAR INFRASTRUCTURE DEPLOYMENT"
echo "=============================================="

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

# Check if we're in the right directory
if [ ! -f "terraform_healink/healink-with-modules.tf" ]; then
    print_error "Please run this script from the project root directory"
    exit 1
fi

# Navigate to terraform directory
cd terraform_healink

# Check current workspace
CURRENT_WORKSPACE=$(terraform workspace show)
print_status "Current Terraform workspace: $CURRENT_WORKSPACE"

# Validate Terraform configuration
print_status "Validating Terraform configuration..."
terraform validate -no-color

if [ $? -eq 0 ]; then
    print_success "Terraform configuration is valid"
else
    print_error "Terraform configuration validation failed"
    exit 1
fi

# Check if this is a dry run
if [ "$1" == "--plan" ]; then
    print_status "Running Terraform plan for modular infrastructure..."
    terraform plan -var-file="${CURRENT_WORKSPACE}.tfvars" -no-color
    print_success "Plan completed successfully"
    exit 0
fi

# Initialize Terraform (if needed)
print_status "Initializing Terraform..."
terraform init -no-color

# Plan the deployment
print_status "Planning modular infrastructure deployment..."
terraform plan -var-file="${CURRENT_WORKSPACE}.tfvars" -out=tfplan -no-color

# Ask for confirmation
echo ""
read -p "Do you want to apply these changes? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    print_warning "Deployment cancelled by user"
    rm -f tfplan
    exit 0
fi

# Apply the configuration
print_status "Applying modular infrastructure..."
terraform apply tfplan -no-color

if [ $? -eq 0 ]; then
    print_success "✅ MODULAR INFRASTRUCTURE DEPLOYED SUCCESSFULLY!"
    echo ""
    print_status "Getting deployment information..."
    
    # Get outputs
    echo ""
    echo "📋 DEPLOYMENT SUMMARY"
    echo "===================="
    terraform output -no-color
    
    echo ""
    print_success "🎉 Your modular microservices infrastructure is ready!"
    echo ""
    echo "Next steps:"
    echo "1. Build and push Docker images to ECR"
    echo "2. Update ECS services to use new images"
    echo "3. Test each microservice endpoint"
    echo ""
    echo "🔗 Quick access URLs will be shown above"
    
else
    print_error "❌ Deployment failed"
    exit 1
fi

# Clean up
rm -f tfplan

print_status "Done! 🚀"