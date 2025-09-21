# Outputs for the modular Terraform setup

# === DATABASE OUTPUTS ===
output "database_endpoint" {
  description = "RDS PostgreSQL endpoint"
  value       = aws_db_instance.healink_db.endpoint
}

output "database_name" {
  description = "Database name"
  value       = aws_db_instance.healink_db.db_name
}

# === REDIS OUTPUTS ===
output "redis_endpoint" {
  description = "Redis endpoint"
  value       = aws_elasticache_replication_group.healink_redis.configuration_endpoint_address
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

# === ECR OUTPUTS ===
output "auth_service_ecr_url" {
  description = "Auth Service ECR repository URL"
  value       = aws_ecr_repository.auth_service.repository_url
}

output "product_service_ecr_url" {
  description = "Product Service ECR repository URL"
  value       = aws_ecr_repository.product_service.repository_url
}

output "gateway_ecr_url" {
  description = "Gateway ECR repository URL"
  value       = aws_ecr_repository.gateway.repository_url
}

# === MICROSERVICE OUTPUTS ===
output "gateway_alb_dns" {
  description = "Gateway ALB DNS name"
  value       = module.gateway.alb_dns_name
}

output "gateway_url" {
  description = "Gateway public URL"
  value       = "http://${module.gateway.alb_dns_name}"
}

output "auth_service_alb_dns" {
  description = "Auth Service ALB DNS name"
  value       = module.auth_service.alb_dns_name
}

output "auth_service_url" {
  description = "Auth Service public URL"
  value       = "http://${module.auth_service.alb_dns_name}"
}

output "product_service_alb_dns" {
  description = "Product Service ALB DNS name"
  value       = module.product_service.alb_dns_name
}

output "product_service_url" {
  description = "Product Service public URL"
  value       = "http://${module.product_service.alb_dns_name}"
}

# === ENVIRONMENT INFO ===
output "environment" {
  description = "Current Terraform workspace/environment"
  value       = terraform.workspace
}

output "cluster_name" {
  description = "ECS cluster name"
  value       = aws_ecs_cluster.healink_cluster.name
}

# === QUICK ACCESS COMMANDS ===
output "docker_commands" {
  description = "Docker commands for building and pushing images"
  value = {
    auth_service = {
      build = "docker build -t ${aws_ecr_repository.auth_service.repository_url}:latest -f src/AuthService/AuthService.API/Dockerfile ."
      push  = "docker push ${aws_ecr_repository.auth_service.repository_url}:latest"
    }
    product_service = {
      build = "docker build -t ${aws_ecr_repository.product_service.repository_url}:latest -f src/ProductService/ProductService.API/Dockerfile ."
      push  = "docker push ${aws_ecr_repository.product_service.repository_url}:latest"
    }
    gateway = {
      build = "docker build -t ${aws_ecr_repository.gateway.repository_url}:latest -f src/Gateway/Gateway.API/Dockerfile ."
      push  = "docker push ${aws_ecr_repository.gateway.repository_url}:latest"
    }
  }
}