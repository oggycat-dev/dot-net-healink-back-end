# Outputs for Stateful Infrastructure
# These values will be consumed by the app-infra layer

# === DATABASE OUTPUTS ===
output "database_endpoint" {
  description = "RDS PostgreSQL endpoint"
  value       = aws_db_instance.healink_db.endpoint
}

output "database_name" {
  description = "Database name"
  value       = aws_db_instance.healink_db.db_name
}

output "database_username" {
  description = "Database username"
  value       = var.db_username
}

output "database_password" {
  description = "Database password"
  value       = var.db_password
  sensitive   = true
}

output "database_port" {
  description = "Database port"
  value       = aws_db_instance.healink_db.port
}

# === REDIS OUTPUTS ===
output "redis_endpoint" {
  description = "Redis endpoint"
  value       = aws_elasticache_replication_group.healink_redis.primary_endpoint_address
}

output "redis_port" {
  description = "Redis port"
  value       = aws_elasticache_replication_group.healink_redis.port
}

# === RABBITMQ OUTPUTS ===
output "rabbitmq_endpoint" {
  description = "RabbitMQ SSL endpoint"
  value       = try(aws_mq_broker.healink_rabbitmq.instances[0].endpoints[1], "")
}

output "rabbitmq_console_url" {
  description = "RabbitMQ Management Console URL"
  value       = try(aws_mq_broker.healink_rabbitmq.instances[0].console_url, "")
}

output "rabbitmq_username" {
  description = "RabbitMQ username"
  value       = var.rabbitmq_username
}

output "rabbitmq_password" {
  description = "RabbitMQ password"
  value       = var.rabbitmq_password
  sensitive   = true
}

# === ECR OUTPUTS ===
output "auth_service_ecr_url" {
  description = "Auth Service ECR repository URL"
  value       = aws_ecr_repository.auth_service.repository_url
}

output "user_service_ecr_url" {
  description = "User Service ECR repository URL"
  value       = aws_ecr_repository.user_service.repository_url
}

output "content_service_ecr_url" {
  description = "Content Service ECR repository URL"
  value       = aws_ecr_repository.content_service.repository_url
}

output "notification_service_ecr_url" {
  description = "Notification Service ECR repository URL"
  value       = aws_ecr_repository.notification_service.repository_url
}

output "subscription_service_ecr_url" {
  description = "Subscription Service ECR repository URL"
  value       = aws_ecr_repository.subscription_service.repository_url
}

output "payment_service_ecr_url" {
  description = "Payment Service ECR repository URL"
  value       = aws_ecr_repository.payment_service.repository_url
}

output "gateway_ecr_url" {
  description = "Gateway ECR repository URL"
  value       = aws_ecr_repository.gateway.repository_url
}

output "podcast_recommendation_service_ecr_url" {
  description = "Podcast Recommendation Service ECR repository URL"
  value       = aws_ecr_repository.podcast_recommendation_service.repository_url
}

output "podcast_ai_service_ecr_url" {
  description = "Podcast AI Service (FastAPI) ECR repository URL"
  value       = aws_ecr_repository.podcast_ai_service.repository_url
}

# === NETWORK OUTPUTS ===
output "vpc_id" {
  description = "VPC ID"
  value       = var.vpc_id
}

output "public_subnets" {
  description = "Public subnet IDs"
  value       = var.public_subnets
}

# === SECURITY GROUP OUTPUTS ===
output "rds_security_group_id" {
  description = "RDS security group ID"
  value       = aws_security_group.rds_sg.id
}

output "redis_security_group_id" {
  description = "Redis security group ID"
  value       = aws_security_group.redis_sg.id
}

output "rabbitmq_security_group_id" {
  description = "RabbitMQ security group ID"
  value       = aws_security_group.rabbitmq_sg.id
}

# === PROJECT INFO ===
output "project_name" {
  description = "Project name"
  value       = var.project_name
}

output "environment" {
  description = "Current environment"
  value       = terraform.workspace
}

output "region" {
  description = "AWS region"
  value       = data.aws_region.current.name
}