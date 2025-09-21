#!/bin/bash

# Professional Development Workflow - Start Dev Environment
# Cost-effective AWS development with workspace isolation

set -e
cd "$(dirname "$0")/../terraform_healink"

echo "🚀 Professional Development Environment Startup"
echo "=============================================="
echo ""
echo "This script implements the cost-effective development workflow:"
echo "1. Creates a temporary dev environment on AWS"
echo "2. Uses optimized instances for cost savings"
echo "3. Should be DESTROYED after use to stop costs"
echo ""
echo "⚠️  WARNING: This will incur AWS costs (~$2-5/hour)"
echo ""

read -p "Continue with creating dev environment? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Cancelled"
    exit 1
fi

echo "📋 Setting up development environment..."
echo ""

# Switch to dev workspace
echo "🔄 Switching to development workspace..."
terraform workspace select dev || terraform workspace new dev
echo "✅ Now using workspace: $(terraform workspace show)"
echo ""

# Initialize if needed
echo "🔧 Initializing Terraform for dev workspace..."
terraform init
echo ""

# Plan and apply development configuration
echo "📋 Planning development infrastructure..."
terraform plan -var-file="dev.tfvars"
echo ""

echo "🏗️  Creating development infrastructure..."
echo "This will take 5-10 minutes..."
terraform apply -var-file="dev.tfvars" -auto-approve

echo ""
echo "🎉 SUCCESS! Development environment is ready!"
echo "=============================================="
echo ""

# Display endpoints
ALB_DNS=$(terraform output -raw alb_dns_name 2>/dev/null || echo "Not available")
RABBITMQ_URL=$(terraform output -raw rabbitmq_console_url 2>/dev/null || echo "Not available")

echo "🌐 Environment Endpoints:"
echo "   - Auth API: http://$ALB_DNS"
echo "   - RabbitMQ Console: $RABBITMQ_URL"
echo ""

echo "📊 COST MONITORING:"
echo "   ⚠️  Dev environment is ACTIVE and incurring costs"
echo "   💰 Estimated: ~$2-5 per hour"
echo "   ⏰ Current time: $(date)"
echo ""

echo "🔥 IMPORTANT - Cost Management:"
echo "   1. Use this environment for frontend integration testing"
echo "   2. Share the API URL with your frontend team"
echo "   3. DESTROY immediately after testing: ./scripts/destroy-dev-env.sh"
echo ""

echo "📝 Next Steps:"
echo "   1. Test your APIs at: http://$ALB_DNS"
echo "   2. Share this URL with frontend team"
echo "   3. When done, run: ./scripts/destroy-dev-env.sh"
echo ""