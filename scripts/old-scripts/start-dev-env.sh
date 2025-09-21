#!/bin/bash
# Script khởi động lại environment khi cần code
# Sử dụng: ./scripts/start-dev-env.sh

echo "🚀 STARTING DEVELOPMENT ENVIRONMENT..."
echo "⏱️ Estimated time: 10-15 minutes"

# 1. Start RDS database first
echo "🗄️ Starting RDS database..."
aws rds start-db-instance --db-instance-identifier healink-db-instance
echo "✅ RDS starting... (2-3 minutes)"

# 2. Recreate infrastructure via Terraform
echo "🏗️ Recreating infrastructure..."
cd terraform_healink

# Recreate Amazon MQ
echo "🐰 Creating Amazon MQ broker..."
terraform apply -target=aws_mq_broker.healink_rabbitmq -auto-approve

# Recreate Redis
echo "📦 Creating Redis cache..."
terraform apply -target=aws_elasticache_cluster.healink_redis -auto-approve

# Update ECS task definition with new endpoints
echo "🔄 Updating ECS task definition..."
terraform apply -auto-approve

echo "✅ Infrastructure ready"

# 3. Scale ECS service back to 1
echo "📈 Starting ECS service..."
aws ecs update-service --cluster healink-cluster --service auth-service --desired-count 1
echo "✅ ECS service starting... (2-3 minutes)"

cd ..

echo ""
echo "🎉 ENVIRONMENT STARTING!"
echo "⏱️ Check status in 10-15 minutes"
echo "🌐 ALB: $(terraform -chdir=terraform_healink output -raw alb_dns_name)"
echo "📊 Monitor: aws ecs describe-services --cluster healink-cluster --services auth-service"