#!/bin/bash

# Professional Development Workflow - Destroy Production Environment
# CRITICAL: Must run after presentations to stop production costs

set -e
cd "$(dirname "$0")/../terraform_healink"

echo "💥 Professional Production Environment Cleanup"
echo "=============================================="
echo ""
echo "🎯 Post-Presentation Cleanup"
echo "This script DESTROYS production to STOP high costs."
echo ""
echo "⚠️  CRITICAL: Production costs ~$5-10/hour"
echo "⚠️  Must be destroyed immediately after presentations!"
echo ""

# Check current workspace
CURRENT_WS=$(terraform workspace show)
if [ "$CURRENT_WS" != "prod" ]; then
    echo "🔄 Switching to production workspace..."
    terraform workspace select prod
fi

echo "🗂️  Current workspace: $(terraform workspace show)"
echo ""

echo "📋 Production resources to be destroyed:"
echo "   - High-availability ECS services (2+ tasks)"
echo "   - Production RDS (db.t4g.small)"
echo "   - Multi-AZ Amazon MQ broker"
echo "   - Production Redis cache"
echo "   - Application Load Balancer"
echo "   - All production networking"
echo ""

echo "💰 Cost Impact:"
echo "   ✅ Production charges STOP immediately (~$5-10/hour saved)"
echo "   ✅ Multi-AZ charges eliminated"
echo "   📊 Only storage costs remain (~$0.01/month)"
echo ""

echo "⏰ Session Information:"
echo "   📅 Current time: $(date)"
echo "   💡 Tip: Calculate your session cost based on deployment time"
echo ""

read -p "🔥 DESTROY PRODUCTION environment? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Cancelled - Production still running!"
    echo "⚠️  WARNING: High production costs are still accumulating!"
    echo "💰 Remember: ~$5-10 per hour until destroyed"
    exit 1
fi

echo ""
echo "💥 Destroying production environment..."
echo "This will take 10-15 minutes for complete cleanup..."

# Show what will be destroyed
terraform plan -destroy -var-file="prod.tfvars"
echo ""

# Destroy everything
terraform destroy -var-file="prod.tfvars" -auto-approve

echo ""
echo "🎉 SUCCESS! Production environment destroyed!"
echo "============================================"
echo ""
echo "💰 Cost Status:"
echo "   ✅ All production AWS resources destroyed"
echo "   ✅ High-cost charges stopped"
echo "   ✅ Multi-AZ charges eliminated"
echo "   ✅ Back to minimal costs"
echo ""
echo "📊 Professional Workflow Complete:"
echo "   ✅ Presentation/demo completed"
echo "   ✅ Production environment cleaned up"
echo "   ✅ Cost-effective development maintained"
echo ""
echo "🔄 Next Steps:"
echo "   📝 Continue development with local Docker Compose"
echo "   🚀 Use './scripts/create-dev-env.sh' for integration testing"
echo "   🎯 Use './scripts/deploy-prod-env.sh' for next presentation"
echo ""
echo "💡 Professional Development Cycle:"
echo "   1. Develop locally (free)"
echo "   2. Create dev environment for team testing (destroy after)"
echo "   3. Deploy production for presentations (destroy immediately after)"
echo "   4. Repeat as needed"
echo ""