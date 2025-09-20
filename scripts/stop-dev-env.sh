#!/bin/bash
# Script tắt toàn bộ environment để tiết kiệm chi phí
# Sử dụng: ./scripts/stop-dev-env.sh

echo "🛑 STOPPING DEVELOPMENT ENVIRONMENT FOR COST SAVING..."
echo "💰 Target: Near $0 cost when not coding"

# 1. Scale ECS service to 0 (QUAN TRỌNG NHẤT)
echo "📉 Scaling ECS service to 0..."
aws ecs update-service --cluster healink-cluster --service auth-service --desired-count 0
echo "✅ ECS service stopped (saves $20-30/month)"

# 2. Stop RDS database
echo "🗄️ Stopping RDS database..."
aws rds stop-db-instance --db-instance-identifier healink-db-instance
echo "✅ RDS stopped (saves $15-25/month, auto-restart in 7 days)"

# 3. Delete Amazon MQ broker (will recreate later)
echo "🐰 Deleting Amazon MQ broker..."
BROKER_ID=$(aws mq list-brokers --query "BrokerSummaries[?BrokerName=='healink-rabbitmq'].BrokerId" --output text)
if [ ! -z "$BROKER_ID" ]; then
    aws mq delete-broker --broker-id $BROKER_ID
    echo "✅ Amazon MQ deleted (saves $15-20/month)"
else
    echo "⚠️ No Amazon MQ broker found"
fi

# 4. Delete ElastiCache Redis (will recreate later)
echo "📦 Deleting Redis cache..."
aws elasticache delete-cache-cluster --cache-cluster-id healink-redis
echo "✅ Redis deleted (saves $10-15/month)"

echo ""
echo "🎉 ENVIRONMENT STOPPED!"
echo "💰 Monthly savings: ~$60-90 (from $80+ to ~$20)"
echo "📝 Remaining costs: ALB (~$18), VPC endpoints (~$5), storage (~$2)"
echo ""
echo "🚀 To restart: ./scripts/start-dev-env.sh"
echo "⏱️ Restart time: ~10-15 minutes"