#!/bin/bash

# Professional Development Workflow - Destroy Dev Environment
# CRITICAL: This script MUST be run after dev testing to stop costs

set -e
cd "$(dirname "$0")/../terraform_healink"

echo "💥 Professional Development Environment Cleanup"
echo "==============================================="
echo ""
echo "This script will DESTROY the development environment to STOP costs."
echo "⚠️  This is CRITICAL for cost management!"
echo ""

# Check current workspace
CURRENT_WS=$(terraform workspace show)
if [ "$CURRENT_WS" != "dev" ]; then
    echo "🔄 Switching to dev workspace..."
    terraform workspace select dev
fi

echo "🗂️  Current workspace: $(terraform workspace show)"
echo ""

echo "📋 What will be destroyed:"
echo "   - All ECS services and tasks"
echo "   - RDS database instance"
echo "   - Amazon MQ broker"
echo "   - Redis cache"
echo "   - Load balancer"
echo "   - All networking resources"
echo ""

echo "💰 Cost Impact:"
echo "   ✅ All hourly charges will STOP after destruction"
echo "   📊 Only storage costs remain (ECR images ~$0.01/month)"
echo ""

read -p "🔥 DESTROY development environment? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Cancelled - Environment still running and costing money!"
    echo "⚠️  Remember: You're still being charged for running resources"
    exit 1
fi

echo ""
echo "💥 Destroying development environment..."
echo "This will take 5-10 minutes..."

# Show what will be destroyed
terraform plan -destroy -var-file="dev.tfvars"
echo ""

# Destroy everything
terraform destroy -var-file="dev.tfvars" -auto-approve

echo ""
echo "🎉 SUCCESS! Development environment destroyed!"
echo "============================================="
echo ""
echo "💰 Cost Status:"
echo "   ✅ All AWS resources destroyed"
echo "   ✅ Hourly charges stopped"
echo "   ✅ Only minimal storage costs remain"
echo ""
echo "📊 Cost Summary:"
echo "   - Session time: $(date)"
echo "   - Estimated session cost: $2-5 (depending on duration)"
echo "   - Future costs: Near zero until next deployment"
echo ""
echo "🔄 Next Time:"
echo "   - Run './scripts/create-dev-env.sh' when you need dev environment"
echo "   - Always destroy after use to maintain cost-effectiveness"
echo ""