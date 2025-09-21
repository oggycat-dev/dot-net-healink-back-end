#!/bin/bash

# Professional Development Workflow - Production Deployment
# For presentations, demos, and final submissions

set -e
cd "$(dirname "$0")/../terraform_healink"

echo "🚀 Professional Production Deployment"
echo "====================================="
echo ""
echo "This script deploys to PRODUCTION environment for:"
echo "- Final presentations"
echo "- Client demos"
echo "- Project submissions"
echo ""
echo "⚠️  WARNING: Production costs more than dev (~$5-10/hour)"
echo "⚠️  CRITICAL: Must be destroyed after presentation!"
echo ""

read -p "Deploy to PRODUCTION environment? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Cancelled"
    exit 1
fi

echo "📋 Setting up production environment..."
echo ""

# Switch to prod workspace
echo "🔄 Switching to production workspace..."
terraform workspace select prod || terraform workspace new prod
echo "✅ Now using workspace: $(terraform workspace show)"
echo ""

# Initialize if needed
echo "🔧 Initializing Terraform for production workspace..."
terraform init
echo ""

# Plan and apply production configuration
echo "📋 Planning production infrastructure..."
terraform plan -var-file="prod.tfvars"
echo ""

echo "🏗️  Creating production infrastructure..."
echo "This will take 10-15 minutes for full production setup..."
terraform apply -var-file="prod.tfvars" -auto-approve

echo ""
echo "🎉 SUCCESS! Production environment is ready!"
echo "============================================"
echo ""

# Display endpoints
ALB_DNS=$(terraform output -raw alb_dns_name 2>/dev/null || echo "Not available")
RABBITMQ_URL=$(terraform output -raw rabbitmq_console_url 2>/dev/null || echo "Not available")

echo "🌐 Production Endpoints:"
echo "   - Auth API: http://$ALB_DNS"
echo "   - RabbitMQ Console: $RABBITMQ_URL"
echo ""

echo "📊 PRODUCTION COST MONITORING:"
echo "   ⚠️  Production environment is ACTIVE"
echo "   💰 Estimated: ~$5-10 per hour"
echo "   🏭 High-availability, multi-AZ setup"
echo "   ⏰ Deployment time: $(date)"
echo ""

echo "🎯 For Presentations:"
echo "   📋 API Base URL: http://$ALB_DNS"
echo "   🔍 Test endpoints: http://$ALB_DNS/swagger"
echo "   📊 RabbitMQ Console: $RABBITMQ_URL"
echo ""

echo "🔥 CRITICAL - After Presentation:"
echo "   1. Immediately run: ./scripts/destroy-prod-env.sh"
echo "   2. This prevents ongoing production costs"
echo "   3. Don't forget - costs accumulate quickly!"
echo ""

echo "✅ Production deployment complete!"
echo "Ready for presentations and demos."