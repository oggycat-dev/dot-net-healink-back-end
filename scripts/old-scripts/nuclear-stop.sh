#!/bin/bash
# Script NUCLEAR - Tắt GẦN NHƯ TẤT CẢ (chi phí ~$0-5/tháng)
# CẢNH BÁO: Sẽ mất data tạm thời, cần recreate hoàn toàn
# Sử dụng: ./scripts/nuclear-stop.sh

echo "☢️ NUCLEAR STOP - MAXIMUM COST SAVING"
echo "⚠️ WARNING: This will delete almost everything!"
echo "💾 Make sure you have your code pushed to GitHub!"
read -p "Continue? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "Cancelled"
    exit 1
fi

# 1. Stop all ECS services
echo "🛑 Stopping all ECS services..."
aws ecs update-service --cluster healink-cluster --service auth-service --desired-count 0

# 2. Delete entire Terraform state (except VPC/basic networking)
echo "💥 Destroying most infrastructure..."
cd terraform_healink
terraform destroy -target=aws_ecs_service.auth_service -auto-approve
terraform destroy -target=aws_ecs_task_definition.auth_service -auto-approve
terraform destroy -target=aws_mq_broker.healink_rabbitmq -auto-approve
terraform destroy -target=aws_elasticache_cluster.healink_redis -auto-approve
terraform destroy -target=aws_db_instance.healink_db -auto-approve

# Keep: VPC, ALB, ECR, Secrets (low/no cost)

echo ""
echo "☢️ NUCLEAR STOP COMPLETE!"
echo "💰 Monthly cost now: ~$0-5 (only ALB + storage)"
echo "🔄 To restart: ./scripts/nuclear-start.sh (takes 20-30 minutes)"