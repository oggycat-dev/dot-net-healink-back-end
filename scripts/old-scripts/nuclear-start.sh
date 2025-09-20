#!/bin/bash
# Script khởi động lại từ nuclear stop
# Sử dụng: ./scripts/nuclear-start.sh

echo "🚀 NUCLEAR START - Rebuilding everything..."
echo "⏱️ Estimated time: 20-30 minutes"

cd terraform_healink

# Recreate everything
echo "🏗️ Recreating all infrastructure..."
terraform apply -auto-approve

echo ""
echo "🎉 NUCLEAR START COMPLETE!"
echo "⏱️ Environment should be ready in 10-15 minutes"
echo "🌐 Test: curl http://$(terraform output -raw alb_dns_name)/health"